using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class PlayCharacterTests
    {
        [Test]
        public void Play_PaysPa_PlacesReadyRecent_OnEmptySlot()
        {
            var catalog = MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var state = CombatTestHelpers.PlayToMain(41UL, catalog);
            var actor = state.ActivePlayerId;
            CombatTestHelpers.SetPa(state, actor, 1);
            var cardId = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);

            var beforeHand = state.GetPlayer(actor).Hand.Count;
            state = CombatTestHelpers.PlayCharacter(engine, state, cardId, BoardRow.Vanguard, 0, "play1");

            var card = state.Cards[cardId];
            Assert.AreEqual(CardZone.Vanguard, card.Zone);
            Assert.AreEqual(ReadyState.Ready, card.ReadyState);
            Assert.IsTrue(CombatPolicies.HasRecent(card));
            Assert.AreEqual(0, state.GetPlayer(actor).PaCurrent);
            Assert.AreEqual(beforeHand - 1, state.GetPlayer(actor).Hand.Count);
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.PaSpent));
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.CharacterPlayed));
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.RecentApplied));
        }

        [Test]
        public void Play_Rejected_WhenInsufficientPa_DoesNotMutate()
        {
            var catalog = MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var state = CombatTestHelpers.PlayToMain(42UL, catalog);
            var actor = state.ActivePlayerId;
            CombatTestHelpers.SetPa(state, actor, 0);
            var cardId = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);
            var hash = GameStateHasher.Hash(state);

            var rejected = engine.Submit(
                state,
                Command.Play("play", actor, state.EventIndex, cardId, BoardRow.Vanguard, 0));
            Assert.IsFalse(rejected.Accepted);
            Assert.AreEqual(hash, GameStateHasher.Hash(rejected.State));
            Assert.AreEqual(CardZone.Hand, rejected.State.Cards[cardId].Zone);
        }

        [Test]
        public void Play_Replace_WhenBoardFull_DiscardsReplacedAndOccupiesSameSlot()
        {
            var catalog = MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var state = CombatTestHelpers.PlayToMain(43UL, catalog);
            var actor = state.ActivePlayerId;
            CombatTestHelpers.SetPa(state, actor, 8);

            var placed = new CardInstanceId[4];
            var slot = 0;
            for (var row = 0; row < 2; row++)
            {
                for (var index = 0; index < 2; index++)
                {
                    var id = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);
                    var boardRow = row == 0 ? BoardRow.Vanguard : BoardRow.Rearguard;
                    state = CombatTestHelpers.PlayCharacter(
                        engine,
                        state,
                        id,
                        boardRow,
                        index,
                        "fill-" + slot);
                    placed[slot] = id;
                    slot++;
                    CombatTestHelpers.SetPa(state, actor, 8);
                }
            }

            Assert.AreEqual(4, CombatPolicies.CountBoardOccupants(state.GetPlayer(actor)));
            var replaceTarget = placed[0];
            var incoming = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);
            CombatTestHelpers.SetPa(state, actor, 1);

            var result = engine.Submit(
                state,
                Command.PlayReplace("repl", actor, state.EventIndex, incoming, replaceTarget));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            state = result.State;

            Assert.AreEqual(CardZone.Discard, state.Cards[replaceTarget].Zone);
            Assert.AreEqual(CardZone.Vanguard, state.Cards[incoming].Zone);
            Assert.AreEqual(0, state.Cards[incoming].Slot.Index);
            Assert.AreEqual(BoardRow.Vanguard, state.Cards[incoming].Slot.Row);
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.CharacterReplaced));
            Assert.AreEqual(4, CombatPolicies.CountBoardOccupants(state.GetPlayer(actor)));
            Assert.IsNull(GameStateInvariants.FindViolation(state));
        }

        [Test]
        public void Play_Rejected_ToOccupiedSlot_WhenBoardNotFull()
        {
            var catalog = MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var state = CombatTestHelpers.PlayToMain(44UL, catalog);
            var actor = state.ActivePlayerId;
            CombatTestHelpers.SetPa(state, actor, 2);
            var first = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);
            state = CombatTestHelpers.PlayCharacter(engine, state, first, BoardRow.Vanguard, 0, "p1");
            CombatTestHelpers.SetPa(state, actor, 1);
            var second = CombatTestHelpers.EnsureCharacterInHand(state, actor, catalog);
            var hash = GameStateHasher.Hash(state);

            var rejected = engine.Submit(
                state,
                Command.Play("p2", actor, state.EventIndex, second, BoardRow.Vanguard, 0));
            Assert.IsFalse(rejected.Accepted);
            Assert.AreEqual(hash, GameStateHasher.Hash(rejected.State));
        }
    }
}
