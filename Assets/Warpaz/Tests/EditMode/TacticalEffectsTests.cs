using System.IO;
using System.Linq;
using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class TacticalEffectsTests
    {
        private CardCatalog _catalog;
        private GameState _state;
        private RulesEngine _engine;
        private DeckList _deck;
        private int _sequence;

        [SetUp]
        public void Setup()
        {
            _catalog = CardCatalogLoader.LoadFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Data")).Catalog;
            Assert.That(_catalog, Is.Not.Null);
            var required = new[]
            {
                "WZ1-018", "WZ1-074", "WZ1-040", "WZ1-083", "WZ1-067", "WZ1-075", "WZ1-110",
                "WZ1-064", "WZ1-065", "WZ1-093", "WZ1-046", "WZ1-111", "WZ1-051",
                "WZ1-019", "WZ1-027", "WZ1-034", "WZ1-035", "WZ1-108", "WZ1-114"
            };
            var ids = required.Select(id => new CardId(id)).ToList();
            foreach (var d in _catalog.All.Where(c => !c.IsIdentity
                && (EffectEngine.Supports(c) || AdvancedEffects.Supports(c) || ExpandedEffects.Supports(c) || TacticalEffects.Supports(c)
                    || c.Type == CardType.Character)))
            {
                if (ids.Count >= 40) break;
                if (!ids.Contains(d.CardId)) ids.Add(d.CardId);
            }
            Assert.That(ids.Count, Is.EqualTo(40));
            _deck = new DeckList("tactical", "Tactical", new CardId("WZ1-001"), new CardId("WZ1-012"), ids);
            _engine = new RulesEngine(_catalog);
            _sequence = 0;
            var created = _engine.Submit(null, Command.CreateMatch("new", new MatchSetup(
                9, RuleConfig.TacticalDefaults(), _catalog.ContentVersion,
                new PlayerSetup(PlayerId.Player0, _deck), new PlayerSetup(PlayerId.Player1, _deck))));
            Assert.That(created.Accepted, Is.True, created.RejectionReason);
            _state = created.State;
            Accept(Command.ConfirmMulligan(Id(), PlayerId.Player0, _state.EventIndex, true));
            Accept(Command.ConfirmMulligan(Id(), PlayerId.Player1, _state.EventIndex, true));
            foreach (var player in _state.AllPlayers) CombatTestHelpers.SetPa(_state, player.PlayerId, 8);
        }

        private CommandId Id() => new CommandId("tactical-" + ++_sequence);
        private void Accept(Command c)
        {
            var r = _engine.Submit(_state, c);
            Assert.That(r.Accepted, Is.True, r.RejectionReason);
            GameStateInvariants.AssertValid(_state);
        }
        private void Reject(Command c)
        {
            var hash = GameStateHasher.Hash(_state);
            Assert.That(_engine.Submit(_state, c).Accepted, Is.False);
            Assert.That(GameStateHasher.Hash(_state), Is.EqualTo(hash));
        }
        private CardInstance Hand(string id, PlayerId actor)
        {
            var c = _state.Cards.Values.First(x => x.OwnerId == actor && x.DefinitionId.Value == id && !x.HasBoardSlot);
            var p = _state.GetPlayer(actor);
            p.Deck.Remove(c.InstanceId); p.Discard.Remove(c.InstanceId);
            if (!p.Hand.Contains(c.InstanceId)) p.Hand.Add(c.InstanceId);
            c.SetZone(CardZone.Hand);
            return c;
        }
        private CardInstance Board(string id, PlayerId actor, BoardRow row = BoardRow.Vanguard, int slot = 0)
        {
            var c = Hand(id, actor);
            EngineActions.PlaceOnBoard(_state, _state.GetPlayer(actor), c.InstanceId, row, slot);
            return c;
        }

        [Test]
        public void ExactBindingsAndVersionGate()
        {
            Assert.That(_catalog.All.Count(TacticalEffects.Supports), Is.EqualTo(6));
            foreach (var d in _catalog.All.Where(TacticalEffects.Supports))
                Assert.That(Wz1EffectBindings.ForCard(d.CardId, d.EffectText + "!"), Is.Empty);
            Assert.That(DeckValidator.Validate(_deck, _catalog, RuleConfig.ExpandedDefaults()).IsValid, Is.False);
            Assert.That(DeckValidator.Validate(_deck, _catalog, RuleConfig.TacticalDefaults()).IsValid, Is.True);
        }

        [Test]
        public void EscarchaExhaustsThenFreezesAlreadyExhausted()
        {
            var actor = _state.ActivePlayerId;
            var target = Board("WZ1-018", actor.Opponent);
            var frost = Hand("WZ1-083", actor);
            Accept(Command.PlayReplace(Id(), actor, _state.EventIndex, frost.InstanceId, target.InstanceId));
            Assert.That(target.ReadyState, Is.EqualTo(ReadyState.Exhausted));
            Assert.That(TacticalEffects.HasStatus(target, StatusType.Frozen), Is.False);

            var frost2 = Hand("WZ1-083", actor);
            Accept(Command.PlayReplace(Id(), actor, _state.EventIndex, frost2.InstanceId, target.InstanceId));
            Assert.That(TacticalEffects.HasStatus(target, StatusType.Frozen), Is.True);
            Assert.That(TacticalEffects.CanDeclareAttack(_state, _catalog, target), Is.False);
        }

        [Test]
        public void FrozenBlocksPreparationReady()
        {
            var actor = _state.ActivePlayerId;
            var target = Board("WZ1-018", actor);
            target.AddStatus(StatusType.Frozen);
            EngineActions.Exhaust(_state, target);
            Accept(Command.EndTurn(Id(), actor, _state.EventIndex));
            Accept(Command.EndTurn(Id(), actor.Opponent, _state.EventIndex));
            Assert.That(target.ReadyState, Is.EqualTo(ReadyState.Exhausted));
            Assert.That(TacticalEffects.HasStatus(target, StatusType.Frozen), Is.True);
        }

        [Test]
        public void RangedAllowsRearAttackAndRearTarget()
        {
            var actor = _state.ActivePlayerId;
            var attacker = Board("WZ1-040", actor, BoardRow.Rearguard, 0);
            var front = Board("WZ1-018", actor.Opponent);
            var rear = Board("WZ1-074", actor.Opponent, BoardRow.Rearguard);
            Assert.That(TacticalEffects.CanDeclareAttack(_state, _catalog, attacker), Is.True);
            Assert.That(TacticalEffects.CanAttackCharacter(_state, _catalog, attacker, front), Is.True);
            Assert.That(TacticalEffects.CanAttackCharacter(_state, _catalog, attacker, rear), Is.True);
            Accept(Command.Attack(Id(), actor, _state.EventIndex, attacker.InstanceId, rear.InstanceId));
        }

        [Test]
        public void MeleeCannotAttackRearOrFromRear()
        {
            var actor = _state.ActivePlayerId;
            var meleeRear = Board("WZ1-018", actor, BoardRow.Rearguard);
            var enemyRear = Board("WZ1-074", actor.Opponent, BoardRow.Rearguard);
            var meleeFront = Board("WZ1-074", actor);
            Assert.That(TacticalEffects.CanDeclareAttack(_state, _catalog, meleeRear), Is.False);
            Assert.That(TacticalEffects.CanAttackCharacter(_state, _catalog, meleeFront, enemyRear), Is.False);
            Reject(Command.Attack(Id(), actor, _state.EventIndex, meleeFront.InstanceId, enemyRear.InstanceId));
        }

        [Test]
        public void ArcGrantsRangedAndRearBonus()
        {
            var actor = _state.ActivePlayerId;
            var bearer = Board("WZ1-018", actor, BoardRow.Rearguard);
            var enemyRear = Board("WZ1-074", actor.Opponent, BoardRow.Rearguard);
            Assert.That(TacticalEffects.CanDeclareAttack(_state, _catalog, bearer), Is.False);
            var bow = Hand("WZ1-110", actor);
            Accept(Command.Equip(Id(), actor, _state.EventIndex, bow.InstanceId, bearer.InstanceId));
            Assert.That(TacticalEffects.HasRanged(_state, _catalog, bearer), Is.True);
            Assert.That(TacticalEffects.CanDeclareAttack(_state, _catalog, bearer), Is.True);
            Accept(Command.Attack(Id(), actor, _state.EventIndex, bearer.InstanceId, enemyRear.InstanceId));
            PassPriorityUntilMain();
            Assert.That(enemyRear.Zone, Is.EqualTo(CardZone.Discard));
        }

        [Test]
        public void CuranderaClearsFrozen()
        {
            var actor = _state.ActivePlayerId;
            var ally = Board("WZ1-018", actor);
            ally.AddStatus(StatusType.Frozen);
            EngineActions.Exhaust(_state, ally);
            var healer = Hand("WZ1-075", actor);
            Accept(Command.Play(Id(), actor, _state.EventIndex, healer.InstanceId, BoardRow.Rearguard, 0));
            Assert.That(_state.PendingDecision.Kind, Is.EqualTo(PendingDecisionKind.EffectChoice));
            Accept(Command.ChooseInterceptor(Id(), actor, _state.EventIndex, ally.InstanceId));
            Assert.That(TacticalEffects.HasStatus(ally, StatusType.Frozen), Is.False);
        }

        [Test]
        public void InvocadorFreezesLowPrintedEnemy()
        {
            var actor = _state.ActivePlayerId;
            var enemy = Board("WZ1-018", actor.Opponent);
            var summoner = Hand("WZ1-067", actor);
            Accept(Command.Play(Id(), actor, _state.EventIndex, summoner.InstanceId, BoardRow.Rearguard, 0));
            Assert.That(_state.PendingDecision.Kind, Is.EqualTo(PendingDecisionKind.EffectChoice));
            Accept(Command.ChooseInterceptor(Id(), actor, _state.EventIndex, enemy.InstanceId));
            Assert.That(TacticalEffects.HasStatus(enemy, StatusType.Frozen), Is.True);
            Assert.That(enemy.ReadyState, Is.EqualTo(ReadyState.Exhausted));
        }

        [Test]
        public void HiddenBlocksEnemyAttackUntilDetected()
        {
            var actor = _state.ActivePlayerId;
            var hidden = Board("WZ1-064", actor, BoardRow.Rearguard);
            hidden.AddStatus(StatusType.Hidden);
            var attacker = Board("WZ1-040", actor.Opponent, BoardRow.Rearguard);
            Assert.That(TacticalEffects.CanAttackCharacter(_state, _catalog, attacker, hidden), Is.False);
            TacticalEffects.ApplyDetect(_state, hidden);
            Assert.That(TacticalEffects.HasStatus(hidden, StatusType.Hidden), Is.False);
            Assert.That(TacticalEffects.HasStatus(hidden, StatusType.Detected), Is.True);
            Assert.That(TacticalEffects.CanAttackCharacter(_state, _catalog, attacker, hidden), Is.True);
        }

        [Test]
        public void AmbushFromRearSkipsHeroInterception()
        {
            var actor = _state.ActivePlayerId;
            var ambusher = Board("WZ1-065", actor, BoardRow.Rearguard);
            Assert.That(TacticalEffects.HasAmbush(_state, _catalog, ambusher), Is.True);
            Assert.That(TacticalEffects.IsAmbushAttack(_state, _catalog, ambusher), Is.True);
            Board("WZ1-027", actor.Opponent, BoardRow.Rearguard);
            Accept(Command.AttackHero(Id(), actor, _state.EventIndex, ambusher.InstanceId));
            Assert.That(_state.PendingDecision.Kind, Is.Not.EqualTo(PendingDecisionKind.ChooseInterceptor));
        }

        [Test]
        public void DaggersGrantAmbushToBearer()
        {
            var actor = _state.ActivePlayerId;
            var bearer = Board("WZ1-018", actor, BoardRow.Rearguard);
            Assert.That(TacticalEffects.HasAmbush(_state, _catalog, bearer), Is.False);
            var daggers = Hand("WZ1-111", actor);
            Accept(Command.Equip(Id(), actor, _state.EventIndex, daggers.InstanceId, bearer.InstanceId));
            Assert.That(TacticalEffects.AmbushValue(_state, _catalog, bearer), Is.EqualTo(1));
            Assert.That(TacticalEffects.CanDeclareAttack(_state, _catalog, bearer), Is.True);
        }

        private void PassPriorityUntilMain()
        {
            var guard = 0;
            while (_state.Phase == MatchPhase.PendingChoice && guard++ < 16)
                Accept(Command.DeclineInterception(Id(), _state.PriorityPlayerId, _state.EventIndex));
        }
    }
}
