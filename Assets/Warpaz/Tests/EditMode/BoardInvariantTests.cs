using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class BoardInvariantTests
    {
        [Test]
        public void CharacterPosition_ParsesSpanishAndEnglish()
        {
            Assert.IsTrue(CharacterPositions.TryParse("Vanguardia", out var v) && v == CharacterPosition.Vanguard);
            Assert.IsTrue(CharacterPositions.TryParse("Vanguard", out v) && v == CharacterPosition.Vanguard);
            Assert.IsTrue(CharacterPositions.TryParse("Retaguardia", out var r) && r == CharacterPosition.Rearguard);
            Assert.IsTrue(CharacterPositions.TryParse("Rearguard", out r) && r == CharacterPosition.Rearguard);
            Assert.IsTrue(CharacterPositions.TryParse("Cualquiera", out var a) && a == CharacterPosition.Any);
            Assert.IsTrue(CharacterPositions.TryParse("Any", out a) && a == CharacterPosition.Any);
            Assert.IsFalse(CharacterPositions.TryParse("Hero", out _));
            Assert.IsTrue(CharacterPositions.AllowsRow(CharacterPosition.Any, BoardRow.Rearguard));
            Assert.IsFalse(CharacterPositions.AllowsRow(CharacterPosition.Vanguard, BoardRow.Rearguard));
        }

        [Test]
        public void AfterPlay_DeckHandDiscardBoard_SumToDeckSize()
        {
            var catalog = MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var state = CombatTestHelpers.PlayToMain(31UL, catalog);
            var actor = state.ActivePlayerId;
            CombatTestHelpers.SetPa(state, actor, 1);
            var card = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);

            state = CombatTestHelpers.PlayCharacter(engine, state, card, BoardRow.Vanguard, 0, "p1");

            var player = state.GetPlayer(actor);
            var board = CombatPolicies.CountBoardOccupants(player);
            Assert.AreEqual(1, board);
            Assert.AreEqual(
                state.Rules.DeckSize,
                player.Deck.Count + player.Hand.Count + player.Discard.Count + board);
            Assert.IsNull(GameStateInvariants.FindViolation(state));
        }

        [Test]
        public void BoardSlots_RejectNonCharacterPlay()
        {
            var catalog = MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var state = CombatTestHelpers.PlayToMain(32UL, catalog);
            var actor = state.ActivePlayerId;
            var hero = state.GetPlayer(actor).HeroInstanceId;
            CombatTestHelpers.SetPa(state, actor, 8);

            var rejected = engine.Submit(
                state,
                Command.Play("bad", actor, state.EventIndex, hero, BoardRow.Vanguard, 0));
            Assert.IsFalse(rejected.Accepted);
            Assert.AreEqual(CommandRejectionCode.InvalidPayload, rejected.Code);
            Assert.AreEqual(state.EventIndex, rejected.State.EventIndex);
        }
    }
}
