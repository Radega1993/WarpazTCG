using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class ReorganizeTests
    {
        [Test]
        public void Reorganize_MoveToEmptyOtherRow_OncePerTurn()
        {
            var catalog = MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var state = CombatTestHelpers.PlayToMain(51UL, catalog);
            var actor = state.ActivePlayerId;
            CombatTestHelpers.SetPa(state, actor, 1);
            var cardId = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);
            state = CombatTestHelpers.PlayCharacter(engine, state, cardId, BoardRow.Vanguard, 0, "p1");

            var moved = engine.Submit(
                state,
                Command.ReorganizeMove("r1", actor, state.EventIndex, cardId, BoardRow.Rearguard, 0));
            Assert.IsTrue(moved.Accepted, moved.RejectionReason);
            state = moved.State;
            Assert.AreEqual(CardZone.Rearguard, state.Cards[cardId].Zone);
            Assert.IsTrue(state.GetPlayer(actor).FreeReorganizationUsed);

            var second = engine.Submit(
                state,
                Command.ReorganizeMove("r2", actor, state.EventIndex, cardId, BoardRow.Vanguard, 0));
            Assert.IsFalse(second.Accepted);
            Assert.AreEqual(CommandRejectionCode.InvalidPayload, second.Code);
        }

        [Test]
        public void Reorganize_SwapRows_ExchangesOccupants()
        {
            var catalog = MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var state = CombatTestHelpers.PlayToMain(52UL, catalog);
            var actor = state.ActivePlayerId;
            CombatTestHelpers.SetPa(state, actor, 2);
            var front = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);
            state = CombatTestHelpers.PlayCharacter(engine, state, front, BoardRow.Vanguard, 0, "p1");
            CombatTestHelpers.SetPa(state, actor, 1);
            var back = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);
            state = CombatTestHelpers.PlayCharacter(engine, state, back, BoardRow.Rearguard, 0, "p2");

            var swapped = engine.Submit(
                state,
                Command.ReorganizeSwap("swap", actor, state.EventIndex, front, back));
            Assert.IsTrue(swapped.Accepted, swapped.RejectionReason);
            state = swapped.State;
            Assert.AreEqual(CardZone.Rearguard, state.Cards[front].Zone);
            Assert.AreEqual(CardZone.Vanguard, state.Cards[back].Zone);
            Assert.IsTrue(state.GetPlayer(actor).FreeReorganizationUsed);
        }

        [Test]
        public void Reorganize_Resets_OnNextOwnTurn()
        {
            var catalog = MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var state = CombatTestHelpers.PlayToMain(53UL, catalog);
            var actor = state.ActivePlayerId;
            CombatTestHelpers.SetPa(state, actor, 1);
            var cardId = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);
            state = CombatTestHelpers.PlayCharacter(engine, state, cardId, BoardRow.Vanguard, 0, "p1");
            state = engine.Submit(
                state,
                Command.ReorganizeMove("r1", actor, state.EventIndex, cardId, BoardRow.Rearguard, 0)).State;
            Assert.IsTrue(state.GetPlayer(actor).FreeReorganizationUsed);

            state = CombatTestHelpers.ClearRecentByPassingTurns(engine, state, actor);
            Assert.IsFalse(state.GetPlayer(actor).FreeReorganizationUsed);
        }
    }
}
