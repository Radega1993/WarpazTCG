using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class AdvancedEffectsTests
    {
        private CardCatalog _catalog;
        private RulesEngine _engine;
        private GameState _state;
        private int _sequence;
        [SetUp] public void Setup()
        {
            _catalog = CardCatalogLoader.LoadFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Data")).Catalog;
            var ids = _catalog.All.Where(AdvancedEffects.Supports).Select(c => c.CardId).ToList();
            ids.AddRange(_catalog.All.Where(EffectEngine.Supports).Select(c => c.CardId));
            foreach (var c in _catalog.All.Where(c => c.Type == CardType.Character))
            {
                if (ids.Count == 40) break;
                ids.Add(c.CardId);
            }
            var deck = new DeckList("advanced", "Advanced test", new CardId("WZ1-001"), new CardId("WZ1-012"), ids);
            _engine = new RulesEngine(_catalog); _sequence = 0;
            var r = _engine.Submit(null, Command.CreateMatch("create", new MatchSetup(5, RuleConfig.AdvancedDefaults(), _catalog.ContentVersion,
                new PlayerSetup(PlayerId.Player0, deck), new PlayerSetup(PlayerId.Player1, deck))));
            Assert.That(r.Accepted, Is.True, r.RejectionReason); _state = r.State;
            Accept(Command.ConfirmMulligan(Id(), PlayerId.Player0, _state.EventIndex, true));
            Accept(Command.ConfirmMulligan(Id(), PlayerId.Player1, _state.EventIndex, true));
            foreach (var p in _state.AllPlayers) CombatTestHelpers.SetPa(_state, p.PlayerId, 8);
        }
        private CommandId Id() => new CommandId("advanced-" + ++_sequence);
        private void Accept(Command c) { var r = _engine.Submit(_state, c); Assert.That(r.Accepted, Is.True, r.RejectionReason); GameStateInvariants.AssertValid(_state); }
        private void Reject(Command c) { var hash = GameStateHasher.Hash(_state); var rng = _state.SaveRng(); Assert.That(_engine.Submit(_state,c).Accepted, Is.False); Assert.That(GameStateHasher.Hash(_state), Is.EqualTo(hash)); Assert.That(_state.SaveRng(), Is.EqualTo(rng)); }
        private CardInstance Hand(string id, PlayerId actor)
        {
            var card = _state.Cards.Values.First(c => c.OwnerId == actor && c.DefinitionId.Value == id && !c.HasBoardSlot);
            var p = _state.GetPlayer(actor); p.Deck.Remove(card.InstanceId); p.Discard.Remove(card.InstanceId);
            if (!p.Hand.Contains(card.InstanceId)) p.Hand.Add(card.InstanceId); card.SetZone(CardZone.Hand); return card;
        }
        private CardInstance Board(string id, PlayerId actor, int slot = 0)
        {
            var c = Hand(id, actor); CombatTestHelpers.PlaceReadyVanguardWithoutRecent(_state, actor, c.InstanceId, slot); return c;
        }
        private void Equip(CardInstance e, CardInstance target) => Accept(Command.Equip(Id(), e.OwnerId, _state.EventIndex, e.InstanceId, target.InstanceId));
        private void Choose(CardInstanceId id) => Accept(Command.ChooseInterceptor(Id(), _state.PriorityPlayerId, _state.EventIndex, id));
        private void Pass() => Accept(Command.DeclineInterception(Id(), _state.PriorityPlayerId, _state.EventIndex));
        private void React(CardInstance card) => Accept(Command.React(Id(), card.OwnerId, _state.EventIndex, card.InstanceId));
        private void StartCombat(CardInstance attacker, CardInstance defender) => Accept(Command.Attack(Id(), attacker.ControllerId, _state.EventIndex, attacker.InstanceId, defender.InstanceId));

        [Test] public void CompiledProgramsAreExplicitAndOldRulesRejectNewNonCharacters()
        {
            Assert.That(_catalog.All.Count(AdvancedEffects.Supports), Is.EqualTo(17));
            Assert.That(_catalog.All.Count(EffectEngine.Supports), Is.EqualTo(4));
            foreach (var d in _catalog.All.Where(AdvancedEffects.Supports)) Assert.That(Wz1EffectBindings.ForCard(d.CardId, d.EffectText + "changed"), Is.Empty);
            var ids = _state.GetPlayer(PlayerId.Player0).DeckInstanceIds.Concat(_state.GetPlayer(PlayerId.Player0).HandInstanceIds).Select(id => _state.Cards[id].DefinitionId).ToArray();
            var deck = new DeckList("x","x",new CardId("WZ1-001"),new CardId("WZ1-012"),ids);
            Assert.That(DeckValidator.Validate(deck,_catalog,RuleConfig.EffectsDefaults()).IsValid,Is.False);
        }
        [Test] public void EquipmentPaysAttachesReplacesAndLeavesWithBearer()
        {
            var actor = _state.ActivePlayerId; var bearer = Board("WZ1-018",actor);
            var sword = Hand("WZ1-108",actor); Equip(sword,bearer);
            Assert.That(sword.Zone,Is.EqualTo(CardZone.Equipment)); Assert.That(_state.GetPlayer(actor).PaCurrent,Is.EqualTo(7));
            _catalog.TryGet(bearer.DefinitionId,out var d);
            Assert.That(AdvancedEffects.Power(_state,_catalog,bearer,true,false),Is.EqualTo(d.Power+1));
            Assert.That(AdvancedEffects.Power(_state,_catalog,bearer,true,true),Is.EqualTo(d.Power));
            var shield=Hand("WZ1-109",actor); Equip(shield,bearer); Assert.That(sword.Zone,Is.EqualTo(CardZone.Discard));
            Assert.That(AdvancedEffects.Power(_state,_catalog,bearer,false,false),Is.EqualTo(d.Power+1));
            var view=GameStateViewProjector.Project(_state,actor.Opponent); Assert.That(view.Opponent.BoardSlots[0].Occupant.EquipmentId,Is.EqualTo(shield.DefinitionId));
            EngineActions.KnockOutToDiscard(_state,_state.GetPlayer(actor),bearer.InstanceId);
            Assert.That(shield.Zone,Is.EqualTo(CardZone.Discard)); Assert.That(bearer.AttachedEquipmentId.IsNone,Is.True); GameStateInvariants.AssertValid(_state);
        }
        [Test] public void IllegalEquipmentDoesNotPayAndOrphansAreDetected()
        {
            var actor=_state.ActivePlayerId; var enemy=Board("WZ1-018",actor.Opponent); var e=Hand("WZ1-108",actor);
            Reject(Command.Equip(Id(),actor,_state.EventIndex,e.InstanceId,enemy.InstanceId));
            var own=Board("WZ1-018",actor); _state.GetPlayer(actor).PaCurrent=0;
            Reject(Command.Equip(Id(),actor,_state.EventIndex,e.InstanceId,own.InstanceId));
            _state.GetPlayer(actor).Hand.Remove(e.InstanceId); e.SetZone(CardZone.Equipment);
            Assert.That(GameStateInvariants.FindViolation(_state),Is.EqualTo("Orphan equipment."));
        }
        [Test] public void ReactionStackAlternatesAndOnlyResolvesAfterTwoPasses()
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Board("WZ1-018",actor.Opponent); var r=Hand("WZ1-098",actor.Opponent);
            StartCombat(a,d); Assert.That(_state.PriorityPlayerId,Is.EqualTo(d.ControllerId));
            React(r); Assert.That(_state.Combat.DefenseBonus,Is.Zero); Assert.That(_state.Combat.Stack.Count,Is.EqualTo(1));
            Reject(Command.EndTurn(Id(),actor,_state.EventIndex)); Pass(); Assert.That(_state.Combat.DefenseBonus,Is.Zero);
            Pass(); Assert.That(_state.Combat.DefenseBonus,Is.EqualTo(2)); Assert.That(a.HasBoardSlot,Is.True);
            Pass(); Pass(); Assert.That(a.Zone,Is.EqualTo(CardZone.Discard)); Assert.That(d.HasBoardSlot,Is.True); Assert.That(_state.Combat,Is.Null);
        }
        [Test] public void EscapeIsLifoDiscardsEquipmentAndCancelsRemainingCombat()
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Board("WZ1-018",actor.Opponent);
            var shield=Hand("WZ1-109",d.ControllerId); var p=_state.GetPlayer(d.ControllerId);
            p.Hand.Remove(shield.InstanceId); shield.SetZone(CardZone.Equipment); d.AttachedEquipmentId=shield.InstanceId;
            var buff=Hand("WZ1-098",d.ControllerId); var escape=Hand("WZ1-100",d.ControllerId);
            StartCombat(a,d); React(buff); Pass(); React(escape); Pass(); Pass();
            Assert.That(_state.Combat,Is.Null); Assert.That(_state.Phase,Is.EqualTo(MatchPhase.Main));
            Assert.That(d.Zone,Is.EqualTo(CardZone.Hand)); Assert.That(shield.Zone,Is.EqualTo(CardZone.Discard));
            Assert.That(a.ReadyState,Is.EqualTo(ReadyState.Exhausted)); Assert.That(_state.EventLog.Any(e=>e.Type==GameEventType.CombatResolved),Is.False);
        }
        [Test] public void ResponseAndReactionAreMutuallyExclusiveAndResponseOncePerSide()
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Board("WZ1-018",actor.Opponent);
            var response=Hand("WZ1-107",d.ControllerId); var reaction=Hand("WZ1-098",d.ControllerId); var other=Hand("WZ1-086",actor);
            StartCombat(a,d); Accept(Command.Respond(Id(),d.ControllerId,_state.EventIndex,response.InstanceId));
            Accept(Command.Respond(Id(),actor,_state.EventIndex,other.InstanceId));
            Reject(Command.React(Id(),d.ControllerId,_state.EventIndex,reaction.InstanceId));
            Reject(Command.Respond(Id(),d.ControllerId,_state.EventIndex,reaction.InstanceId));
            Assert.That(_state.Combat.AttackBonus,Is.EqualTo(2)); Assert.That(_state.Combat.DefenseBonus,Is.EqualTo(1));
            Pass(); Pass(); Assert.That(_state.Combat,Is.Null);
        }
        [Test] public void PlanOrdersFourThenDrawsThenChoosesHandBottomWithPrivacy()
        {
            var actor=_state.ActivePlayerId; var action=Hand("WZ1-096",actor); var p=_state.GetPlayer(actor);
            var top=p.Deck.Take(4).Reverse().ToArray(); var handBefore=p.Hand.Count;
            Accept(Command.PlayAction(Id(),actor,_state.EventIndex,action.InstanceId,CardInstanceId.None));
            Assert.That(EffectDecisionView.Project(_state,_catalog,actor.Opponent),Is.Null);
            var options=EffectDecisionView.Project(_state,_catalog,actor); Assert.That(options.Options.Count,Is.EqualTo(4));
            var before=GameStateHasher.Hash(_state); Reject(Command.ChooseInterceptor(Id(),actor,_state.EventIndex,action.InstanceId));
            Reject(Command.ChooseInterceptor(Id(),actor.Opponent,_state.EventIndex,top[0]));
            foreach(var id in top) Choose(id);
            Assert.That(p.Hand.Contains(top[0]),Is.True); Assert.That(_state.PendingEffectKind,Is.EqualTo(EffectStepKind.BottomHand));
            var choice=Command.ChooseInterceptor(Id(),actor,_state.EventIndex,top[0]); Accept(choice); Reject(choice);
            Assert.That(p.Deck.Last(),Is.EqualTo(top[0])); Assert.That(p.Deck.Take(3),Is.EqualTo(top.Skip(1)));
            Assert.That(p.Hand.Count,Is.EqualTo(handBefore-1)); Assert.That(_state.EffectQueue,Is.Empty); Assert.That(_state.Phase,Is.EqualTo(MatchPhase.Main));
        }
        [Test] public void SacrificeDrawDiscardThenEquipmentTriggerDrawDiscardIsOneContinuationChain()
        {
            var actor=_state.ActivePlayerId; var mechanic=Board("WZ1-039",actor); var e=Hand("WZ1-108",actor); Equip(e,mechanic);
            var action=Hand("WZ1-084",actor); var p=_state.GetPlayer(actor); var before=p.Deck.Count;
            Accept(Command.PlayAction(Id(),actor,_state.EventIndex,action.InstanceId,mechanic.InstanceId));
            Assert.That(mechanic.Zone,Is.EqualTo(CardZone.Discard)); Assert.That(p.Deck.Count,Is.EqualTo(before-2));
            Assert.That(_state.EventLog.Count(x=>x.Type==GameEventType.CharacterKnockedOut),Is.Zero);
            Choose(p.Hand[0]); Assert.That(p.Deck.Count,Is.EqualTo(before-3)); Assert.That(_state.PendingEffectKind,Is.EqualTo(EffectStepKind.Discard));
            Choose(p.Hand[0]); Assert.That(_state.Phase,Is.EqualTo(MatchPhase.Main)); Assert.That(mechanic.LastTriggerTurn,Is.EqualTo(_state.Turn));
        }
        [TestCase("WZ1-018")] [TestCase("WZ1-028")] [TestCase("WZ1-069")]
        public void DeploymentOpensAppropriateChoice(string id)
        {
            var actor=_state.ActivePlayerId; var source=Hand(id,actor);
            if(id!="WZ1-018") { var target=Hand(id=="WZ1-028"?"WZ1-108":"WZ1-018",actor); EngineActions.Move(_state,_state.GetPlayer(actor),target.InstanceId,CardZone.Discard); }
            var legal=LegalActions.For(_state,_catalog).First(a=>a.Type==CommandType.Play && a.PrimaryInstanceId==source.InstanceId && a.HasBoardSlotTarget);
            Accept(Command.Play(Id(),actor,_state.EventIndex,source.InstanceId,legal.TargetRow,legal.TargetSlotIndex));
            Assert.That(_state.PendingDecision.Kind,Is.EqualTo(PendingDecisionKind.EffectChoice));
            Choose(id=="WZ1-018"?CardInstanceId.None:_state.PendingDecision.ChoiceIds[0]); Assert.That(_state.Phase,Is.EqualTo(MatchPhase.Main));
        }
        [Test] public void EmptyDeckEffectEndsMatchAndDropsRemainingChoices()
        {
            var actor=_state.ActivePlayerId; var action=Hand("WZ1-096",actor); var p=_state.GetPlayer(actor);
            foreach(var id in p.Deck.ToArray()) EngineActions.Move(_state,p,id,CardZone.Discard); p.Resolution=1;
            Accept(Command.PlayAction(Id(),actor,_state.EventIndex,action.InstanceId,CardInstanceId.None));
            Assert.That(_state.Phase,Is.EqualTo(MatchPhase.GameOver)); Assert.That(_state.PendingDecision.IsPending,Is.False); Assert.That(_state.EffectQueue,Is.Empty);
        }
        [Test] public void CombatAndPartialChoiceProgressAreHashed()
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Board("WZ1-018",actor.Opponent);
            StartCombat(a,d); var before=GameStateHasher.Hash(_state); _state.Combat.AttackBonus++; Assert.That(GameStateHasher.Hash(_state),Is.Not.EqualTo(before));
            before=GameStateHasher.Hash(_state); _state.Combat.ResponseUsed[0]=true; Assert.That(GameStateHasher.Hash(_state),Is.Not.EqualTo(before));
        }
        [TestCase("WZ1-106",false,1)] [TestCase("WZ1-106",true,3)] [TestCase("WZ1-107",false,1)]
        public void ConditionalReactionBonusesAreEvaluatedOnResolution(string id,bool equipped,int expected)
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Board("WZ1-018",actor.Opponent); var r=Hand(id,d.ControllerId);
            if(equipped) { var e=Hand("WZ1-109",d.ControllerId); _state.GetPlayer(d.ControllerId).Hand.Remove(e.InstanceId); e.SetZone(CardZone.Equipment); d.AttachedEquipmentId=e.InstanceId; }
            StartCombat(a,d); React(r); Pass(); Pass(); Assert.That(_state.Combat.DefenseBonus,Is.EqualTo(expected));
            var response=Hand("WZ1-086",d.ControllerId); Reject(Command.Respond(Id(),d.ControllerId,_state.EventIndex,response.InstanceId));
        }
        [Test] public void UnderdogAndKoDrawUsePrintedPowerAndActualKo()
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-024",actor); var d=Board("WZ1-018",actor.Opponent); var r=Hand("WZ1-107",d.ControllerId);
            StartCombat(a,d); React(r); Pass(); Pass(); Assert.That(_state.Combat.DefenseBonus,Is.EqualTo(2));
            var ko=Hand("WZ1-102",d.ControllerId); var before=_state.GetPlayer(d.ControllerId).Deck.Count;
            React(ko); Pass(); Pass(); a.TurnPowerModifier=20; Pass(); Pass();
            Assert.That(d.Zone,Is.EqualTo(CardZone.Discard)); Assert.That(_state.GetPlayer(d.ControllerId).Deck.Count,Is.EqualTo(before-1));
        }
        [TestCase("WZ1-024")] [TestCase("WZ1-036")] [TestCase("WZ1-055")]
        public void InterceptionTriggerIsCombatOnlyAndStillOpensReactionWindow(string id)
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Hand(id,actor.Opponent);
            EngineActions.PlaceOnBoard(_state,_state.GetPlayer(d.ControllerId),d.InstanceId,BoardRow.Rearguard,0);
            Assert.That(CombatPolicies.CanIntercept(d,_catalog,_state.Rules),Is.True);
            Accept(Command.AttackHero(Id(),actor,_state.EventIndex,a.InstanceId));
            Assert.That(_state.PendingDecision.Kind,Is.EqualTo(PendingDecisionKind.ChooseInterceptor)); Choose(d.InstanceId);
            Assert.That(_state.PendingDecision.Kind,Is.EqualTo(PendingDecisionKind.Reaction)); Assert.That(_state.Combat.DefenseBonus,Is.EqualTo(1));
            Pass(); Pass(); Assert.That(d.TurnPowerModifier,Is.Zero);
        }
        [Test] public void HighestCostEquipmentRecalculatesWithBoardAndAllowsTies()
        {
            var actor=_state.ActivePlayerId; var bearer=Board("WZ1-018",actor); var hammer=Hand("WZ1-119",actor); Equip(hammer,bearer);
            _catalog.TryGet(bearer.DefinitionId,out var d); Assert.That(AdvancedEffects.Power(_state,_catalog,bearer,true,false),Is.EqualTo(d.Power+2));
            var other=Board("WZ1-024",actor,1); Assert.That(AdvancedEffects.Power(_state,_catalog,bearer,true,false),Is.EqualTo(d.Power));
            EngineActions.KnockOutToDiscard(_state,_state.GetPlayer(actor),other.InstanceId);
            Assert.That(AdvancedEffects.Power(_state,_catalog,bearer,true,true),Is.EqualTo(d.Power+2));
        }
        [Test] public void EquipmentLootOnlyTriggersOncePerTurnEvenAcrossTwoReplacements()
        {
            var actor=_state.ActivePlayerId; var mechanic=Board("WZ1-039",actor); var sword=Hand("WZ1-108",actor); Equip(sword,mechanic);
            var shield=Hand("WZ1-109",actor); Equip(shield,mechanic); Choose(_state.GetPlayer(actor).Hand[0]);
            CombatTestHelpers.SetPa(_state,actor,8); var hammer=Hand("WZ1-119",actor); Equip(hammer,mechanic);
            Assert.That(_state.PendingDecision.IsPending,Is.False); Assert.That(_state.EventLog.Count(e=>e.Type==GameEventType.TriggerQueued),Is.EqualTo(1));
        }
        [Test] public void PartialOrderSnapshotAndPrivateEventsDoNotLeakToOpponent()
        {
            var actor=_state.ActivePlayerId; var action=Hand("WZ1-096",actor);
            Accept(Command.PlayAction(Id(),actor,_state.EventIndex,action.InstanceId,CardInstanceId.None));
            var id=_state.PendingDecision.ChoiceIds[0]; Choose(id); var before=GameStateHasher.Hash(_state);
            _state.EffectQueue[0].Ordered.Reverse(); // one item: equal; then alter progress explicitly
            _state.EffectQueue[0].Ordered.Clear(); Assert.That(GameStateHasher.Hash(_state),Is.Not.EqualTo(before));
            var owner=GameStateViewProjector.Project(_state,actor); var rival=GameStateViewProjector.Project(_state,actor.Opponent);
            Assert.That(owner.Events.Count,Is.GreaterThan(rival.Events.Count)); Assert.That(rival.Opponent.VisibleHand,Is.Empty);
            Assert.That(EffectDecisionView.Project(_state,_catalog,actor.Opponent),Is.Null);
        }
    }
}
