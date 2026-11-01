using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class CommandPipelineTests
    {
        [Test]
        public void ConfirmMulligan_CurrentEventIndex_IsAcceptedWithMatchingHash()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var created = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(3UL)));
            Assert.IsTrue(created.Accepted, created.RejectionReason);

            var from = created.State.EventIndex;
            var result = engine.Submit(
                created.State,
                Command.ConfirmMulligan("m1", 0, created.State.EventIndex, keep: true));

            Assert.IsTrue(result.Accepted, result.RejectionReason);
            Assert.AreEqual(CommandRejectionCode.None, result.Code);
            Assert.AreEqual(GameStateHasher.Hash(result.State), result.StateHash);
            Assert.Greater(result.Events.Count, 0);
            Assert.AreEqual(from, result.Events[0].EventIndex);
            for (var i = 1; i < result.Events.Count; i++)
            {
                Assert.AreEqual(result.Events[i - 1].EventIndex + 1, result.Events[i].EventIndex);
            }

            Assert.AreEqual(result.State.EventIndex, result.State.Events.Count);
        }

        [Test]
        public void StaleExpectedEventIndex_RejectsWithoutMutatingHashRngOrEvents()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(4UL))).State;
            var indexBefore = state.EventIndex;
            var hashBefore = GameStateHasher.Hash(state);
            var rngBefore = state.Rng.Save();
            var eventCountBefore = state.Events.Count;

            var result = engine.Submit(state, Command.ConfirmMulligan("stale", 0, 0, true));

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(CommandRejectionCode.StaleState, result.Code);
            StringAssert.Contains("expectedEventIndex", result.RejectionReason);
            Assert.AreEqual(0, result.Events.Count);
            Assert.AreEqual(indexBefore, state.EventIndex);
            Assert.AreEqual(eventCountBefore, state.Events.Count);
            Assert.AreEqual(hashBefore, GameStateHasher.Hash(state));
            Assert.AreEqual(hashBefore, result.StateHash);
            Assert.AreEqual(rngBefore, state.Rng.Save());
        }

        [Test]
        public void HostActor_OnMulligan_IsUnknownPlayer()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(5UL))).State;
            var hashBefore = GameStateHasher.Hash(state);

            var result = engine.Submit(
                state,
                Command.ConfirmMulligan(CommandId.Parse("host"), Command.HostActorId, state.EventIndex, true));

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(CommandRejectionCode.UnknownPlayer, result.Code);
            Assert.AreEqual(hashBefore, GameStateHasher.Hash(state));
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void UnsupportedCommandType_IsCommandNotSupported()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(6UL))).State;
            var hashBefore = GameStateHasher.Hash(state);
            var rngBefore = state.Rng.Save();

            var result = engine.Submit(
                state,
                Command.Unsupported("atk", 0, CommandType.Equip, state.EventIndex));

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(CommandRejectionCode.CommandNotSupported, result.Code);
            Assert.AreEqual(hashBefore, GameStateHasher.Hash(state));
            Assert.AreEqual(rngBefore, state.Rng.Save());
            Assert.AreEqual(state.EventIndex, result.State.EventIndex);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void DuplicateCommandId_IsRejectedAndDoesNotMutate()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var created = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(7UL)));
            var keep = Command.ConfirmMulligan("m-dup", 0, created.State.EventIndex, true);
            var first = engine.Submit(created.State, keep);
            Assert.IsTrue(first.Accepted, first.RejectionReason);

            var hashBefore = GameStateHasher.Hash(first.State);
            var indexBefore = first.State.EventIndex;
            var rngBefore = first.State.Rng.Save();

            var second = engine.Submit(
                first.State,
                Command.ConfirmMulligan("m-dup", 0, first.State.EventIndex, true));

            Assert.IsFalse(second.Accepted);
            Assert.AreEqual(CommandRejectionCode.DuplicateCommand, second.Code);
            Assert.AreEqual(hashBefore, GameStateHasher.Hash(first.State));
            Assert.AreEqual(indexBefore, first.State.EventIndex);
            Assert.AreEqual(rngBefore, first.State.Rng.Save());
            Assert.AreEqual(0, second.Events.Count);
        }

        [Test]
        public void CommandAfterGameOver_IsMatchAlreadyFinished()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(8UL))).State;
            state.Phase = MatchPhase.GameOver;
            state.Result = MatchResult.Ended(PlayerId.Player0, MatchEndCause.Concede);
            var hashBefore = GameStateHasher.Hash(state);

            var result = engine.Submit(
                state,
                Command.ConfirmMulligan("late", 0, state.EventIndex, true));

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(CommandRejectionCode.MatchAlreadyFinished, result.Code);
            Assert.AreEqual(hashBefore, GameStateHasher.Hash(state));
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void MissingCommand_IsInvalidPayload()
        {
            var result = new RulesEngine(MatchFixtures.Catalog).Submit(null, null);
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(CommandRejectionCode.InvalidPayload, result.Code);
            Assert.IsNull(result.State);
        }
    }
}
