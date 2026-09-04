using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class LegalActionQueryTests
    {
        [Test]
        public void MulliganP0_Player0CanKeepOrReplace()
        {
            var session = MatchFixtures.Started(3UL);
            Assert.AreEqual(MatchPhase.MulliganP0, session.State.Phase);

            var actions = LegalActionQuery.For(session);
            Assert.AreEqual(2, actions.Count);
            AssertMulliganPair(actions, PlayerId.Player0);
            Assert.AreEqual(0, CountFor(actions, PlayerId.Player1));
        }

        [Test]
        public void MulliganP1_Player1CanKeepOrReplace()
        {
            var session = MatchFixtures.Started(3UL);
            Assert.IsTrue(session.ConfirmMulligan(0, true).Accepted);

            var actions = LegalActionQuery.For(session.State);
            Assert.AreEqual(MatchPhase.MulliganP1, session.State.Phase);
            Assert.AreEqual(2, actions.Count);
            AssertMulliganPair(actions, PlayerId.Player1);
            Assert.AreEqual(0, CountFor(actions, PlayerId.Player0));
        }

        [Test]
        public void Main_OnlyActivePlayerCanEndTurn()
        {
            var session = PlayToMain();
            var active = session.State.ActivePlayerId;
            var actions = session.LegalActions();

            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual(CommandType.EndTurn, actions[0].Type);
            Assert.AreEqual(active, actions[0].ActorId);
            Assert.IsFalse(actions[0].HasMulliganDecision);
            Assert.AreEqual(0, CountEndTurn(actions, active.Opponent));
        }

        [Test]
        public void GameOver_HasNoLegalActions()
        {
            var session = PlayToMain();
            var miller = session.State.ActivePlayerId;
            RulesEngine.MoveDeckToDiscard(session.State, miller);
            session.State.Players[miller.Index].Resolution = 1;
            Assert.IsTrue(session.EndTurn(miller).Accepted);
            Assert.IsTrue(session.EndTurn(session.State.ActivePlayerId).Accepted);

            Assert.AreEqual(MatchPhase.GameOver, session.State.Phase);
            Assert.AreEqual(0, LegalActionQuery.For(session).Count);
        }

        [Test]
        public void MissingState_HasNoLegalActions()
        {
            Assert.AreEqual(0, LegalActionQuery.For((GameState)null).Count);
            Assert.AreEqual(0, LegalActionQuery.For((GameSession)null).Count);
            Assert.AreEqual(0, LegalActionQuery.For(new GameSession(MatchFixtures.Catalog)).Count);
        }

        private static GameSession PlayToMain()
        {
            var session = MatchFixtures.Started(8UL);
            Assert.IsTrue(session.ConfirmMulligan(0, true).Accepted);
            Assert.IsTrue(session.ConfirmMulligan(1, true).Accepted);
            Assert.AreEqual(MatchPhase.Main, session.State.Phase);
            return session;
        }

        private static void AssertMulliganPair(IReadOnlyList<LegalAction> actions, PlayerId actor)
        {
            Assert.AreEqual(CommandType.ConfirmMulligan, actions[0].Type);
            Assert.AreEqual(actor, actions[0].ActorId);
            Assert.AreEqual(MulliganDecision.KeepHand, actions[0].MulliganDecision);
            Assert.AreEqual(CommandType.ConfirmMulligan, actions[1].Type);
            Assert.AreEqual(actor, actions[1].ActorId);
            Assert.AreEqual(MulliganDecision.ReplaceWholeHand, actions[1].MulliganDecision);
        }

        private static int CountFor(IReadOnlyList<LegalAction> actions, PlayerId actor)
        {
            var count = 0;
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].ActorId == actor)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEndTurn(IReadOnlyList<LegalAction> actions, PlayerId actor)
        {
            var count = 0;
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Type == CommandType.EndTurn && actions[i].ActorId == actor)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
