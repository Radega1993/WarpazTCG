using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class EffectEngineTests
    {
        private CardCatalog _catalog;
        private RulesEngine _engine;
        private GameState _state;
        [SetUp] public void Setup()
        {
            _catalog = CardCatalogLoader.LoadFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Data")).Catalog;
            Assert.NotNull(_catalog);
            var cards = _catalog.All.Where(c => c.Type == CardType.Character).Take(12).SelectMany(c => Enumerable.Repeat(c.CardId, 3)).ToList();
            cards.AddRange(new[] { "WZ1-078", "WZ1-079", "WZ1-082", "WZ1-086" }.Select(id => new CardId(id)));
            var d = new DeckList("effects", "Effects test", _catalog.All.First(c => c.Type == CardType.Hero).CardId,
                _catalog.All.First(c => c.Type == CardType.ClassPath).CardId, cards);
            _engine = new RulesEngine(_catalog);
            var result = _engine.Submit(null, Command.CreateMatch("c", new MatchSetup(1, RuleConfig.EffectsDefaults(), _catalog.ContentVersion,
                new PlayerSetup(PlayerId.Player0, d), new PlayerSetup(PlayerId.Player1, d))));
            Assert.IsTrue(result.Accepted, result.RejectionReason); _state = result.State;
            _state = _engine.Submit(_state, Command.ConfirmMulligan("m0", 0, _state.EventIndex, true)).State;
            _state = _engine.Submit(_state, Command.ConfirmMulligan("m1", 1, _state.EventIndex, true)).State;
        }
        private CardInstance Action(string id)
        {
            var card = _state.Cards.Values.First(c => c.OwnerId == _state.ActivePlayerId && c.DefinitionId.Value == id);
            var p = _state.GetPlayer(card.OwnerId); p.Deck.Remove(card.InstanceId); if (!p.Hand.Contains(card.InstanceId)) p.Hand.Add(card.InstanceId);
            card.SetZone(CardZone.Hand); CombatTestHelpers.SetPa(_state, card.OwnerId, 8); return card;
        }
        private CardInstance Target(bool enemy)
        {
            var actor = enemy ? _state.ActivePlayerId.Opponent : _state.ActivePlayerId;
            var target = _state.Cards.Values.First(c => c.OwnerId == actor && _catalog.TryGet(c.DefinitionId, out var d) && d.Type == CardType.Character);
            CombatTestHelpers.PlaceReadyVanguardWithoutRecent(_state, actor, target.InstanceId, 0); return target;
        }
        private Command Play(CardInstance action, CardInstance target, string id = "effect") => Command.PlayAction(new CommandId(id), _state.ActivePlayerId, _state.EventIndex, action.InstanceId, target.InstanceId);

        [TestCase("WZ1-078", false, 2)] [TestCase("WZ1-079", true, -2)]
        [TestCase("WZ1-082", true, -2)] [TestCase("WZ1-086", true, 2)]
        public void CompleteProgramsPayDiscardApplyAndExpire(string id, bool enemy, int modifier)
        {
            var action = Action(id); var target = Target(enemy); var actor = _state.ActivePlayerId;
            _catalog.TryGet(action.DefinitionId, out var definition);
            Assert.IsTrue(LegalActions.For(_state, _catalog).Any(a => a.PrimaryInstanceId == action.InstanceId && a.SecondaryInstanceId == target.InstanceId));
            var result = _engine.Submit(_state, Play(action, target)); Assert.IsTrue(result.Accepted, result.RejectionReason);
            Assert.AreEqual(8 - definition.CostPa, _state.GetPlayer(actor).PaCurrent);
            Assert.AreEqual(CardZone.Discard, action.Zone); Assert.AreEqual(modifier, target.TurnPowerModifier);
            Assert.AreEqual(id == "WZ1-079", target.CannotInterceptThisTurn);
            Assert.AreEqual(id == "WZ1-082", target.CannotReadyThisTurn);
            GameStateInvariants.AssertValid(_state);
            result = _engine.Submit(_state, Command.EndTurn("end", actor, _state.EventIndex)); Assert.IsTrue(result.Accepted);
            Assert.AreEqual(0, target.TurnPowerModifier); Assert.IsFalse(target.CannotReadyThisTurn); Assert.IsFalse(target.CannotInterceptThisTurn);
        }
        [Test] public void InvalidTargetAndDuplicateLeaveHashUnchanged()
        {
            var action = Action("WZ1-078"); var enemy = Target(true); var own = Target(false);
            var before = GameStateHasher.Hash(_state);
            Assert.IsFalse(_engine.Submit(_state, Play(action, enemy)).Accepted); Assert.AreEqual(before, GameStateHasher.Hash(_state));
            var valid = Play(action, own, "ok"); Assert.IsTrue(_engine.Submit(_state, valid).Accepted);
            before = GameStateHasher.Hash(_state); Assert.IsFalse(_engine.Submit(_state, valid).Accepted); Assert.AreEqual(before, GameStateHasher.Hash(_state));
        }
        [Test] public void ModifiersStackAffectPowerAndAreClearedOnLeavingBoard()
        {
            var a = Action("WZ1-078"); var b = Action("WZ1-086"); var target = Target(false);
            Assert.IsTrue(_engine.Submit(_state, Play(a, target, "a")).Accepted);
            CombatTestHelpers.SetPa(_state, _state.ActivePlayerId, 8);
            Assert.IsTrue(_engine.Submit(_state, Play(b, target, "b")).Accepted);
            _catalog.TryGet(target.DefinitionId, out var d); Assert.AreEqual(d.Power + 4, EffectEngine.EffectivePower(d, target));
            var hash = GameStateHasher.Hash(_state); target.TurnPowerModifier++; Assert.AreNotEqual(hash, GameStateHasher.Hash(_state));
            EngineActions.KnockOutToDiscard(_state, _state.GetPlayer(target.ControllerId), target.InstanceId);
            Assert.AreEqual(0, target.TurnPowerModifier); GameStateInvariants.AssertValid(_state);
        }
        [Test] public void UnimplementedCardsAndChangedPrintedBindingsFailClosed()
        {
            Assert.AreEqual(4, _catalog.All.Count(EffectEngine.Supports));
            Assert.IsEmpty(Wz1EffectBindings.ForCard(new CardId("WZ1-078"), "changed"));
            Assert.IsTrue(_catalog.All.Where(c => c.Type == CardType.Reaction || c.Type == CardType.Equipment).All(c => !EffectEngine.Supports(c)));
        }
        [Test] public void PreventionBlocksReadyingAndNegativePowerIsClamped()
        {
            var action = Action("WZ1-082"); var target = Target(true);
            Assert.IsTrue(_engine.Submit(_state, Play(action, target)).Accepted);
            EngineActions.Exhaust(_state, target); EngineActions.Ready(_state, target);
            Assert.AreEqual(ReadyState.Exhausted, target.ReadyState);
            _catalog.TryGet(target.DefinitionId, out var d); target.TurnPowerModifier = -100;
            Assert.AreEqual(0, EffectEngine.EffectivePower(d, target));
        }
        [Test] public void InsufficientPaDoesNotPayOrDiscard()
        {
            var a = Action("WZ1-078"); var t = Target(false); _state.GetPlayer(_state.ActivePlayerId).PaCurrent = 0;
            var hash = GameStateHasher.Hash(_state); Assert.IsFalse(_engine.Submit(_state, Play(a,t)).Accepted);
            Assert.AreEqual(hash, GameStateHasher.Hash(_state)); Assert.AreEqual(CardZone.Hand,a.Zone);
        }
        [Test] public void PowerModifierChangesRealCombatOutcome()
        {
            var action = Action("WZ1-078"); var attacker = Target(false); var defender = Target(true);
            Assert.AreEqual(attacker.DefinitionId, defender.DefinitionId);
            Assert.IsTrue(_engine.Submit(_state, Play(action, attacker)).Accepted);
            var result = _engine.Submit(_state, Command.Attack("combat", _state.ActivePlayerId, _state.EventIndex, attacker.InstanceId, defender.InstanceId));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            Assert.AreEqual(CardZone.Vanguard, attacker.Zone); Assert.AreEqual(CardZone.Discard, defender.Zone);
        }
        [Test] public void InvalidProgramsAreRejectedAndInstructionsAreCopied()
        {
            _catalog.TryGet(new CardId("WZ1-078"), out var d);
            Func<EffectDefinition[], CardDefinition> with = effects => new CardDefinition(d.CardId, d.SetId, d.Number, d.Name, d.Type,
                d.Subtype, d.Race, d.ClassPath, d.CostPa, d.Power, d.Response, d.Rarity, d.Unique, d.Position, d.Keywords,
                d.EffectText, d.Flavor, d.Archetype, d.Canon, d.Complexity, d.BalanceNotes, effects);
            Assert.IsFalse(EffectEngine.Supports(with(new[] { new EffectDefinition((EffectOpcode)999, EffectTarget.AnyCharacter) })));
            Assert.IsFalse(EffectEngine.Supports(with(Enumerable.Repeat(d.Effects[0], 17).ToArray())));
            Assert.IsFalse(EffectEngine.Supports(with(new[] { new EffectDefinition(EffectOpcode.PowerUntilEndTurn, EffectTarget.AnyCharacter, 999) })));
            var program = new[] { d.Effects[0] }; var copy = with(program); program[0] = null;
            Assert.IsTrue(EffectEngine.Supports(copy));
        }
    }
}
