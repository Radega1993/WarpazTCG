using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class ExpandedEffectsTests
    {
        private CardCatalog _catalog;
        private GameState _state;
        private RulesEngine _engine;
        private DeckList _deck;
        private int _sequence;
        [SetUp] public void Setup()
        {
            _catalog=CardCatalogLoader.LoadFromDirectory(Path.Combine(Directory.GetCurrentDirectory(),"Assets/StreamingAssets/Data")).Catalog;
            Assert.That(_catalog,Is.Not.Null);
            var ids=_catalog.All.Where(d=>EffectEngine.Supports(d)||AdvancedEffects.Supports(d)||ExpandedEffects.Supports(d)).Select(d=>d.CardId).ToList();
            ids.Add(new CardId("WZ1-018")); ids.Add(new CardId("WZ1-074"));
            _deck=new DeckList("expanded","Expanded",new CardId("WZ1-001"),new CardId("WZ1-012"),ids);
            _engine=new RulesEngine(_catalog); _sequence=0;
            var created=_engine.Submit(null,Command.CreateMatch("new",new MatchSetup(8,RuleConfig.ExpandedDefaults(),_catalog.ContentVersion,new PlayerSetup(PlayerId.Player0,_deck),new PlayerSetup(PlayerId.Player1,_deck))));
            Assert.That(created.Accepted,Is.True,created.RejectionReason); _state=created.State;
            Accept(Command.ConfirmMulligan(Id(),PlayerId.Player0,_state.EventIndex,true));
            Accept(Command.ConfirmMulligan(Id(),PlayerId.Player1,_state.EventIndex,true));
            foreach(var player in _state.AllPlayers) CombatTestHelpers.SetPa(_state,player.PlayerId,8);
        }
        private CommandId Id()=>new CommandId("expanded-" + ++_sequence);
        private void Accept(Command c) { var r=_engine.Submit(_state,c); Assert.That(r.Accepted,Is.True,r.RejectionReason); GameStateInvariants.AssertValid(_state); }
        private void Reject(Command c) { var hash=GameStateHasher.Hash(_state); Assert.That(_engine.Submit(_state,c).Accepted,Is.False); Assert.That(GameStateHasher.Hash(_state),Is.EqualTo(hash)); }
        private CardInstance Hand(string id,PlayerId actor)
        {
            var c=_state.Cards.Values.First(x=>x.OwnerId==actor&&x.DefinitionId.Value==id&&!x.HasBoardSlot);
            var p=_state.GetPlayer(actor); p.Deck.Remove(c.InstanceId); p.Discard.Remove(c.InstanceId);
            if(!p.Hand.Contains(c.InstanceId))p.Hand.Add(c.InstanceId); c.SetZone(CardZone.Hand); return c;
        }
        private CardInstance Board(string id,PlayerId actor,BoardRow row=BoardRow.Vanguard,int slot=0)
        {
            var c=Hand(id,actor); EngineActions.PlaceOnBoard(_state,_state.GetPlayer(actor),c.InstanceId,row,slot); return c;
        }
        private int Printed(CardInstance c) { _catalog.TryGet(c.DefinitionId,out var d); return d.Power??0; }
        private int Power(CardInstance c,bool attack=false,bool hero=false)=>AdvancedEffects.Power(_state,_catalog,c,attack,hero);
        private void Attach(string id,CardInstance bearer)
        { var e=Hand(id,bearer.ControllerId); Accept(Command.Equip(Id(),bearer.ControllerId,_state.EventIndex,e.InstanceId,bearer.InstanceId)); }
        private void Pass()=>Accept(Command.DeclineInterception(Id(),_state.PriorityPlayerId,_state.EventIndex));
        private void Combat(CardInstance a,CardInstance d)=>Accept(Command.Attack(Id(),a.ControllerId,_state.EventIndex,a.InstanceId,d.InstanceId));

        [Test] public void ExactBindingsAndVersionGate()
        {
            Assert.That(_catalog.All.Count(ExpandedEffects.Supports),Is.EqualTo(17));
            Assert.That(_catalog.All.Count(AdvancedEffects.Supports),Is.EqualTo(17));
            foreach(var d in _catalog.All.Where(ExpandedEffects.Supports))Assert.That(Wz1EffectBindings.ForCard(d.CardId,d.EffectText+"!"),Is.Empty);
            Assert.That(DeckValidator.Validate(_deck,_catalog,RuleConfig.AdvancedDefaults()).IsValid,Is.False);
        }
        [TestCase("WZ1-019",false)] [TestCase("WZ1-040",true)]
        public void EquipmentConditionalBonusTracksAttachment(string id,bool attack)
        {
            var c=Board(id,_state.ActivePlayerId); Assert.That(Power(c,attack,true),Is.EqualTo(Printed(c)));
            Attach("WZ1-108",c); Assert.That(Power(c,attack,true),Is.EqualTo(Printed(c)+1));
            AdvancedEffects.Detach(_state,c); Assert.That(Power(c,attack,true),Is.EqualTo(Printed(c)));
        }
        [Test] public void GnollConditionAndAuraFollowPresenceAndOpponentTurn()
        {
            var actor=_state.ActivePlayerId; var pup=Board("WZ1-034",actor); var other=Board("WZ1-035",actor,BoardRow.Vanguard,1);
            var alpha=Board("WZ1-037",actor,BoardRow.Rearguard);
            Assert.That(Power(pup),Is.EqualTo(Printed(pup)+1)); Assert.That(Power(alpha),Is.EqualTo(Printed(alpha)));
            _state.ActivePlayerId=actor.Opponent; Assert.That(Power(pup),Is.EqualTo(Printed(pup)+2)); Assert.That(Power(other),Is.EqualTo(Printed(other)+1));
            EngineActions.KnockOutToDiscard(_state,_state.GetPlayer(actor),other.InstanceId); Assert.That(Power(pup),Is.EqualTo(Printed(pup)+1));
            EngineActions.KnockOutToDiscard(_state,_state.GetPlayer(actor),alpha.InstanceId); Assert.That(Power(pup),Is.EqualTo(Printed(pup)));
        }
        [Test] public void CaptainAuraDoesNotBuffSelfRearOrAttackAndDisappearsOnKo()
        {
            var actor=_state.ActivePlayerId; var captain=Board("WZ1-045",actor); var other=Board("WZ1-018",actor,BoardRow.Vanguard,1);
            var rear=Board("WZ1-019",actor,BoardRow.Rearguard);
            Assert.That(Power(captain),Is.EqualTo(Printed(captain))); Assert.That(Power(other),Is.EqualTo(Printed(other)+1));
            Assert.That(Power(other,true),Is.EqualTo(Printed(other))); Assert.That(Power(rear),Is.EqualTo(Printed(rear)));
            EngineActions.KnockOutToDiscard(_state,_state.GetPlayer(actor),captain.InstanceId); Assert.That(Power(other),Is.EqualTo(Printed(other)));
        }
        [Test] public void LoneVanguardAndHighCostAllyAreRecomputedAfterReorganization()
        {
            var actor=_state.ActivePlayerId; var lone=Board("WZ1-054",actor); var high=Board("WZ1-037",actor,BoardRow.Rearguard);
            var peon=Board("WZ1-058",actor,BoardRow.Rearguard,1);
            Assert.That(Power(lone),Is.EqualTo(Printed(lone)+1)); Assert.That(Power(peon),Is.EqualTo(Printed(peon)+1));
            EngineActions.MoveBoardToEmptySlot(_state,_state.GetPlayer(actor),high.InstanceId,BoardRow.Vanguard,1);
            Assert.That(Power(lone),Is.EqualTo(Printed(lone)));
            EngineActions.KnockOutToDiscard(_state,_state.GetPlayer(actor),high.InstanceId); Assert.That(Power(peon),Is.EqualTo(Printed(peon)));
        }
        [TestCase("WZ1-056","WZ1-060",1)] [TestCase("WZ1-056","WZ1-018",0)]
        [TestCase("WZ1-073","WZ1-037",2)] [TestCase("WZ1-073","WZ1-060",0)]
        [TestCase("WZ1-059","WZ1-018",1)] [TestCase("WZ1-059","WZ1-037",0)]
        public void AttackConditionsUsePrintedCostAndExcludeHero(string attackerId,string defenderId,int expected)
        {
            var actor=_state.ActivePlayerId; var a=Board(attackerId,actor); var d=Board(defenderId,actor.Opponent);
            Combat(a,d); Assert.That(Power(a,true),Is.EqualTo(Printed(a)+expected));
            Assert.That(Power(a,true,true),Is.EqualTo(Printed(a)));
        }
        [Test] public void PrintedPowerComparisonIgnoresTemporaryBuffs()
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Board("WZ1-035",actor.Opponent);
            a.TurnPowerModifier=20; Combat(a,d); Assert.That(Power(d),Is.EqualTo(Printed(d)));
        }
        [TestCase("WZ1-027",BoardRow.Vanguard,false)] [TestCase("WZ1-027",BoardRow.Rearguard,true)]
        [TestCase("WZ1-060",BoardRow.Vanguard,false)] [TestCase("WZ1-060",BoardRow.Rearguard,false)]
        public void MovementProtectionIsDirectionalAndDoesNotBlockOwnMovement(string id,BoardRow row,bool enemyAllowed)
        {
            var c=Board(id,_state.ActivePlayerId,row); Assert.That(ExpandedEffects.CanMoveByEffect(_state,_catalog,c,c.ControllerId.Opponent),Is.EqualTo(enemyAllowed));
            Assert.That(ExpandedEffects.CanMoveByEffect(_state,_catalog,c,c.ControllerId),Is.True);
            EngineActions.KnockOutToDiscard(_state,_state.GetPlayer(c.ControllerId),c.InstanceId); Assert.That(c.Zone,Is.EqualTo(CardZone.Discard));
        }
        [Test] public void ArmorAddsDefenseAndEnemyMovementImmunityWithoutProtectingFromKo()
        {
            var c=Board("WZ1-018",_state.ActivePlayerId); Attach("WZ1-114",c);
            Assert.That(Power(c),Is.EqualTo(Printed(c)+2)); Assert.That(Power(c,true),Is.EqualTo(Printed(c)));
            Assert.That(ExpandedEffects.CanMoveByEffect(_state,_catalog,c,c.ControllerId.Opponent),Is.False);
            var eq=c.AttachedEquipmentId; EngineActions.KnockOutToDiscard(_state,_state.GetPlayer(c.ControllerId),c.InstanceId);
            Assert.That(_state.Cards[eq].Zone,Is.EqualTo(CardZone.Discard));
        }
        [TestCase(true)] [TestCase(false)] public void EvadeOffersPrivateOptionalPostCombatMove(bool accept)
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Board("WZ1-060",actor.Opponent); var r=Hand("WZ1-104",d.ControllerId);
            Combat(a,d); Accept(Command.React(Id(),d.ControllerId,_state.EventIndex,r.InstanceId)); Pass(); Pass(); Pass(); Pass();
            Assert.That(_state.Combat,Is.Null); Assert.That(_state.PendingEffectKind,Is.EqualTo(EffectStepKind.EvadeToRear));
            Reject(Command.ChooseInterceptor(Id(),actor,_state.EventIndex,d.InstanceId));
            Accept(Command.ChooseInterceptor(Id(),d.ControllerId,_state.EventIndex,accept?d.InstanceId:CardInstanceId.None));
            Assert.That(d.Zone,Is.EqualTo(accept?CardZone.Rearguard:CardZone.Vanguard)); Assert.That(_state.Phase,Is.EqualTo(MatchPhase.Main));
            Assert.That(_state.GetPlayer(d.ControllerId).FreeReorganizationUsed,Is.False);
        }
        [Test] public void EvadeDoesNotBlockWhenRearIsFull()
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Board("WZ1-060",actor.Opponent); var r=Hand("WZ1-104",d.ControllerId);
            Board("WZ1-019",d.ControllerId,BoardRow.Rearguard,0); Board("WZ1-027",d.ControllerId,BoardRow.Rearguard,1);
            Combat(a,d); Accept(Command.React(Id(),d.ControllerId,_state.EventIndex,r.InstanceId)); Pass(); Pass(); Pass(); Pass();
            Assert.That(_state.Phase,Is.EqualTo(MatchPhase.Main)); Assert.That(d.Zone,Is.EqualTo(CardZone.Vanguard));
        }
        [Test] public void CounterDoesNotLeaveTurnModifierOnKnockedOutAttacker()
        {
            var actor=_state.ActivePlayerId; var a=Board("WZ1-018",actor); var d=Board("WZ1-060",actor.Opponent); var r=Hand("WZ1-099",d.ControllerId);
            Combat(a,d); Accept(Command.React(Id(),d.ControllerId,_state.EventIndex,r.InstanceId)); Pass(); Pass();
            Assert.That(_state.Combat.CounterPenalty,Is.EqualTo(1)); Assert.That(_state.Combat.DefenseBonus,Is.EqualTo(2)); Pass(); Pass();
            Assert.That(a.Zone,Is.EqualTo(CardZone.Discard)); Assert.That(a.TurnPowerModifier,Is.Zero);
        }
    }
}
