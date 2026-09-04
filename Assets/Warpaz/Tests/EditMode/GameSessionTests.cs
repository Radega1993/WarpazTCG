using System.Reflection;
using NUnit.Framework;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class GameSessionTests
    {
        [Test]
        public void State_HasNoPublicSetter()
        {
            var property = typeof(GameSession).GetProperty("State");
            Assert.IsNotNull(property);
            Assert.IsNotNull(property.GetGetMethod());
            Assert.IsNull(property.GetSetMethod());
            Assert.IsTrue(property.GetSetMethod(true).IsPrivate);
        }

        [Test]
        public void CurrentHash_IsEmpty_BeforeStart()
        {
            var session = new GameSession(MatchFixtures.Catalog);
            Assert.IsNull(session.State);
            Assert.AreEqual(string.Empty, session.CurrentHash);
        }

        [Test]
        public void Submit_StaleExpectedEventIndex_DoesNotMutate()
        {
            var session = MatchFixtures.Started(15UL);
            var indexBefore = session.State.EventIndex;
            var hashBefore = session.CurrentHash;
            var stateBefore = session.State;
            var acceptedBefore = session.AcceptedCommands.Count;

            var stale = Command.ConfirmMulligan("stale", 0, 0, true);
            var result = new SubmitCommandUseCase().Execute(session, stale);

            Assert.IsFalse(result.Accepted);
            Assert.AreSame(stateBefore, session.State);
            Assert.AreEqual(indexBefore, session.State.EventIndex);
            Assert.AreEqual(hashBefore, session.CurrentHash);
            Assert.AreEqual(acceptedBefore, session.AcceptedCommands.Count);
            Assert.AreEqual(hashBefore, GameStateHasher.Hash(session.State));
        }

        [Test]
        public void AcceptedCommand_UpdatesSession_RejectedDoesNot()
        {
            var session = MatchFixtures.Started(17UL);
            var hashBefore = session.CurrentHash;
            var accepted = session.ConfirmMulligan(0, true);
            Assert.IsTrue(accepted.Accepted, accepted.RejectionReason);
            Assert.AreSame(accepted.State, session.State);
            Assert.AreEqual(accepted.StateHash, session.CurrentHash);
            Assert.AreNotEqual(hashBefore, session.CurrentHash);
            Assert.AreEqual(MatchPhase.MulliganP1, session.State.Phase);
            Assert.IsTrue(session.LastResult.Accepted);

            var wrongActor = session.ConfirmMulligan(0, true);
            Assert.IsFalse(wrongActor.Accepted);
            Assert.AreEqual(CommandRejectionCode.WrongActor, wrongActor.Code);
            Assert.AreEqual(MatchPhase.MulliganP1, session.State.Phase);
            Assert.AreEqual(accepted.StateHash, session.CurrentHash);
        }

        [Test]
        public void CreateMatchUseCase_StartsSession()
        {
            var session = new GameSession(MatchFixtures.Catalog);
            var result = new CreateMatchUseCase().Execute(session, MatchFixtures.Standard(4UL));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            Assert.IsNotNull(session.State);
            Assert.AreEqual(MatchPhase.MulliganP0, session.State.Phase);
            Assert.AreEqual(session.CurrentHash, GameStateHasher.Hash(session.State));
        }

        [Test]
        public void Session_KeepMulligansAndFiveEndTurns_Advances()
        {
            var session = MatchFixtures.Started(16UL);
            Assert.IsTrue(session.ConfirmMulligan(0, true).Accepted);
            Assert.IsTrue(session.ConfirmMulligan(1, true).Accepted);

            for (var i = 0; i < 5; i++)
            {
                var result = session.EndTurn(session.State.ActivePlayerId);
                Assert.IsTrue(result.Accepted, result.RejectionReason);
            }

            Assert.AreEqual(6, session.State.Turn);
            Assert.AreEqual(8, session.AcceptedCommands.Count);
        }
    }
}
