using System;
using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    /// <summary>
    /// Integration tests that run a full Sprint-1 match (WZ1 catalog) and verify
    /// invariants, determinism, and seed sensitivity without duplicating unit tests
    /// that already exist in GameSessionTests / Wz1CreateMatchTests / etc.
    /// </summary>
    public sealed class Sprint1IntegrationTests
    {
        private static GameSession BuildSession(ulong seed)
        {
            var catalog = Wz1MatchFixtures.Catalog;
            var session = new GameSession(catalog);
            var setup = Wz1MatchFixtures.Standard(seed);
            var start = session.Start(setup);
            Assert.IsTrue(start.Accepted, "Start rejected: " + start.RejectionReason);
            return session;
        }

        private static (GameSession session, List<int> eventIndexSnapshots)
            RunFullFlow(ulong seed)
        {
            var session = BuildSession(seed);
            var snapshots = new List<int>();

            snapshots.Add(session.State.EventIndex);

            var m0 = session.ConfirmMulligan(PlayerId.Player0, MulliganDecision.KeepHand);
            Assert.IsTrue(m0.Accepted, "ConfirmMulligan P0 rejected: " + m0.RejectionReason);
            snapshots.Add(session.State.EventIndex);

            var m1 = session.ConfirmMulligan(PlayerId.Player1, MulliganDecision.KeepHand);
            Assert.IsTrue(m1.Accepted, "ConfirmMulligan P1 rejected: " + m1.RejectionReason);
            snapshots.Add(session.State.EventIndex);

            for (var i = 0; i < 5; i++)
            {
                var et = session.EndTurn(session.State.ActivePlayerId);
                Assert.IsTrue(et.Accepted, "EndTurn " + (i + 1) + " rejected: " + et.RejectionReason);
                snapshots.Add(session.State.EventIndex);
            }

            return (session, snapshots);
        }

        [Test]
        public void Sprint1_FullFlow_InvariantsHoldAfterEveryCommand()
        {
            var session = BuildSession(7UL);
            var prevIndex = 0;

            Sprint1StateAssertions.AssertValid(session.State, prevIndex);
            prevIndex = session.State.EventIndex;

            var m0 = session.ConfirmMulligan(PlayerId.Player0, MulliganDecision.KeepHand);
            Assert.IsTrue(m0.Accepted, m0.RejectionReason);
            Sprint1StateAssertions.AssertValid(session.State, prevIndex);
            prevIndex = session.State.EventIndex;

            var m1 = session.ConfirmMulligan(PlayerId.Player1, MulliganDecision.KeepHand);
            Assert.IsTrue(m1.Accepted, m1.RejectionReason);
            Sprint1StateAssertions.AssertValid(session.State, prevIndex);
            prevIndex = session.State.EventIndex;

            Assert.AreEqual(MatchPhase.Main, session.State.Phase);

            for (var i = 0; i < 5; i++)
            {
                var et = session.EndTurn(session.State.ActivePlayerId);
                Assert.IsTrue(et.Accepted, "EndTurn " + (i + 1) + ": " + et.RejectionReason);
                Sprint1StateAssertions.AssertValid(session.State, prevIndex);
                prevIndex = session.State.EventIndex;
            }

            Assert.AreEqual(6, session.State.Turn);
            Assert.AreEqual(MatchPhase.Main, session.State.Phase);
        }

        [Test]
        public void Sprint1_SameSeed_IsFullyDeterministic()
        {
            const ulong seed = 99UL;

            var (s1, _) = RunFullFlow(seed);
            var (s2, _) = RunFullFlow(seed);

            Assert.AreEqual(s1.State.FirstPlayerId, s2.State.FirstPlayerId,
                "FirstPlayerId must be identical for same seed");

            Assert.AreEqual(GameStateHasher.Hash(s1.State), GameStateHasher.Hash(s2.State),
                "Final hash must be identical for same seed + commands");

            foreach (var seat in new[] { PlayerId.Player0, PlayerId.Player1 })
            {
                var view1 = s1.Project(seat);
                var view2 = s2.Project(seat);
                var hand1 = view1.Viewer.VisibleHand;
                var hand2 = view2.Viewer.VisibleHand;
                Assert.AreEqual(hand1.Count, hand2.Count,
                    "Hand count differs for " + seat);
                for (var i = 0; i < hand1.Count; i++)
                {
                    Assert.AreEqual(hand1[i].CardId, hand2[i].CardId,
                        "Hand card at index " + i + " differs for player " + seat);
                }
            }

            var log1 = s1.State.EventLog;
            var log2 = s2.State.EventLog;
            Assert.AreEqual(log1.Count, log2.Count, "Event log count differs");
            for (var i = 0; i < log1.Count; i++)
            {
                Assert.AreEqual(log1[i].Type, log2[i].Type,
                    "Event type at index " + i + " differs");
                Assert.AreEqual(log1[i].Visibility, log2[i].Visibility,
                    "Event visibility at index " + i + " differs");
            }
        }

        [Test]
        public void Sprint1_DifferentSeed_ProducesReasonableDifference()
        {
            var sessionA = BuildSession(1UL);
            Assert.IsTrue(sessionA.ConfirmMulligan(PlayerId.Player0, MulliganDecision.KeepHand).Accepted);
            Assert.IsTrue(sessionA.ConfirmMulligan(PlayerId.Player1, MulliganDecision.KeepHand).Accepted);

            var sessionB = BuildSession(2UL);
            Assert.IsTrue(sessionB.ConfirmMulligan(PlayerId.Player0, MulliganDecision.KeepHand).Accepted);
            Assert.IsTrue(sessionB.ConfirmMulligan(PlayerId.Player1, MulliganDecision.KeepHand).Accepted);

            Assert.AreNotEqual(
                GameStateHasher.Hash(sessionA.State),
                GameStateHasher.Hash(sessionB.State),
                "Two different seeds produced identical final hash — likely a determinism bug");

            var foundDifference = false;
            foreach (var seat in new[] { PlayerId.Player0, PlayerId.Player1 })
            {
                var handA = sessionA.Project(seat).Viewer.VisibleHand;
                var handB = sessionB.Project(seat).Viewer.VisibleHand;
                for (var i = 0; i < Math.Min(handA.Count, handB.Count); i++)
                {
                    if (handA[i].CardId.Value != handB[i].CardId.Value)
                    {
                        foundDifference = true;
                        break;
                    }
                }

                if (foundDifference)
                {
                    break;
                }
            }

            Assert.IsTrue(foundDifference,
                "Seeds 1 and 2 produced identical hands — RNG may not be seeded correctly");
        }
    }
}
