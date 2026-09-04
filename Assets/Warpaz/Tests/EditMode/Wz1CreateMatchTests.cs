using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class Wz1CreateMatchTests
    {
        [Test]
        public void CreateMatch_ValidWz1Decks_OpensMulliganP0AndDrawsFive()
        {
            var result = Create(1UL);
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            var state = result.State;
            Assert.AreEqual(MatchPhase.MulliganP0, state.Phase);
            Assert.AreEqual(PlayerId.Player0, state.PriorityPlayerId);
            Assert.AreEqual(state.FirstPlayerId, state.ActivePlayerId);
            Assert.AreEqual(5, state.Players[0].Hand.Count);
            Assert.AreEqual(5, state.Players[1].Hand.Count);
            Assert.AreEqual(35, state.Players[0].Deck.Count);
            Assert.AreEqual(35, state.Players[1].Deck.Count);
            Assert.AreEqual(0, state.Players[0].PaMax);
            Assert.AreEqual(0, state.Players[0].PaCurrent);
            Assert.AreEqual(0, state.Players[1].PaMax);
            Assert.AreEqual(5, state.Players[0].Resolution);
            Assert.AreEqual(5, state.Players[1].Resolution);
            Assert.AreEqual(0, state.Players[0].Discard.Count);
            Assert.IsFalse(state.Players[0].Ascended);
            Assert.AreEqual(ReadyState.Ready, state.Cards[state.Players[0].HeroInstanceId].ReadyState);
            Assert.AreEqual(CardZone.Hero, state.Cards[state.Players[0].HeroInstanceId].Zone);
            Assert.AreEqual(CardZone.ClassPath, state.Cards[state.Players[0].ClassInstanceId].Zone);
            Assert.IsNull(GameStateInvariants.FindViolation(state));
            Assert.Greater(state.EventIndex, 0);
            Assert.AreEqual(state.EventIndex, state.Events.Count);
        }

        [Test]
        public void CreateMatch_SameSetupAndSeed_SameStateAndHash()
        {
            var first = Create(7UL).State;
            var second = Create(7UL).State;
            Assert.AreEqual(GameStateHasher.Hash(first), GameStateHasher.Hash(second));
            Assert.AreEqual(
                CanonicalGameStateSerializer.ToString(first),
                CanonicalGameStateSerializer.ToString(second));
        }

        [Test]
        public void CreateMatch_DifferentSeed_CanChangeOrderOrFirstPlayer()
        {
            var a = Create(1UL).State;
            var b = Create(2UL).State;
            var sameFirst = a.FirstPlayerId == b.FirstPlayerId;
            var sameP0 = SameDefinitionOrder(a, 0, b);
            var sameP1 = SameDefinitionOrder(a, 1, b);
            Assert.IsFalse(sameFirst && sameP0 && sameP1, "A different seed should change first player or a deck order.");
        }

        [Test]
        public void CreateMatch_EachPlayerHasFortyMainDeckInstances()
        {
            var state = Create(3UL).State;
            for (var p = 0; p < 2; p++)
            {
                var player = state.Players[p];
                Assert.AreEqual(
                    40,
                    player.Deck.Count + player.Hand.Count + player.Discard.Count);
                Assert.AreEqual(84, state.Cards.Count);
            }
        }

        [Test]
        public void CreateMatch_CardInstanceIds_AreUnique()
        {
            var state = Create(4UL).State;
            var seen = new HashSet<CardInstanceId>();
            foreach (var card in state.Cards.Values)
            {
                Assert.IsTrue(seen.Add(card.InstanceId), "Duplicate instance id " + card.InstanceId);
                Assert.Greater(card.InstanceId.Value, 0);
            }

            Assert.AreEqual(state.Cards.Count, seen.Count);
        }

        [Test]
        public void CreateMatch_HeroAndClass_AreOutsideTheMainDeck()
        {
            var state = Create(5UL).State;
            for (var p = 0; p < 2; p++)
            {
                var player = state.Players[p];
                Assert.IsFalse(Contains(player.Deck, player.HeroInstanceId));
                Assert.IsFalse(Contains(player.Hand, player.HeroInstanceId));
                Assert.IsFalse(Contains(player.Discard, player.HeroInstanceId));
                Assert.IsFalse(Contains(player.Deck, player.ClassInstanceId));
                Assert.IsFalse(Contains(player.Hand, player.ClassInstanceId));
                Assert.IsFalse(Contains(player.Discard, player.ClassInstanceId));
                Assert.AreEqual(CardZone.Hero, state.Cards[player.HeroInstanceId].Zone);
                Assert.AreEqual(CardZone.ClassPath, state.Cards[player.ClassInstanceId].Zone);
            }
        }

        [Test]
        public void CreateMatch_OpponentView_HidesPrivateHand()
        {
            var state = Create(6UL).State;
            var view = GameStateView.For(state, PlayerId.Player0);
            Assert.AreEqual(5, view.Player1.HandCount);
            Assert.AreEqual(0, view.Player1.VisibleHand.Count);
            Assert.AreEqual(5, view.Player0.VisibleHand.Count);
        }

        [Test]
        public void CreateMatch_CardDrawnEvents_DoNotIncludeDefinitionIds()
        {
            var result = Create(8UL);
            foreach (var evt in result.Events)
            {
                if (evt.Type != GameEventType.CardDrawn)
                {
                    continue;
                }

                Assert.IsTrue(result.State.TryGetCard(evt.SourceInstanceId, out var card));
                StringAssert.DoesNotContain(card.DefinitionId.Value, evt.Payload);
            }
        }

        [Test]
        public void CreateMatch_Rejects_39Cards()
        {
            AssertRejected(
                Wz1MatchFixtures.Player(
                    PlayerId.Player0,
                    Wz1MatchFixtures.HeroId(0),
                    Wz1MatchFixtures.ClassId(0),
                    Wz1MatchFixtures.MainDeckOfSize(39)),
                "40");
        }

        [Test]
        public void CreateMatch_Rejects_41Cards()
        {
            AssertRejected(
                Wz1MatchFixtures.Player(
                    PlayerId.Player0,
                    Wz1MatchFixtures.HeroId(0),
                    Wz1MatchFixtures.ClassId(0),
                    Wz1MatchFixtures.MainDeckOfSize(41)),
                "40");
        }

        [Test]
        public void CreateMatch_Rejects_FourthCopy()
        {
            AssertRejected(
                Wz1MatchFixtures.Player(
                    PlayerId.Player0,
                    Wz1MatchFixtures.HeroId(0),
                    Wz1MatchFixtures.ClassId(0),
                    Wz1MatchFixtures.DeckWithFourthCopy()),
                "3-copy");
        }

        [Test]
        public void CreateMatch_Rejects_UnknownId()
        {
            AssertRejected(
                Wz1MatchFixtures.Player(
                    PlayerId.Player0,
                    Wz1MatchFixtures.HeroId(0),
                    Wz1MatchFixtures.ClassId(0),
                    Wz1MatchFixtures.DeckWithUnknownId()),
                "Unknown");
        }

        [Test]
        public void CreateMatch_Rejects_HeroUsedAsClass()
        {
            AssertRejected(
                Wz1MatchFixtures.Player(
                    PlayerId.Player0,
                    Wz1MatchFixtures.HeroId(0),
                    Wz1MatchFixtures.HeroId(1),
                    Wz1MatchFixtures.MainDeck40()),
                "ClassPath");
        }

        [Test]
        public void CreateMatch_Rejects_ClassUsedAsHero()
        {
            AssertRejected(
                Wz1MatchFixtures.Player(
                    PlayerId.Player0,
                    Wz1MatchFixtures.ClassId(0),
                    Wz1MatchFixtures.ClassId(1),
                    Wz1MatchFixtures.MainDeck40()),
                "Hero");
        }

        [Test]
        public void CreateMatch_Rejects_IdentityInMainDeck()
        {
            AssertRejected(
                Wz1MatchFixtures.Player(
                    PlayerId.Player0,
                    Wz1MatchFixtures.HeroId(0),
                    Wz1MatchFixtures.ClassId(0),
                    Wz1MatchFixtures.DeckWithIdentity()),
                "Identity");
        }

        [Test]
        public void CreateMatch_Rejects_DuplicatePlayerId()
        {
            var setup = new MatchSetup(
                1UL,
                RuleConfig.AlphaDefaults(),
                Wz1MatchFixtures.Catalog.ContentVersion,
                Wz1MatchFixtures.Seat(PlayerId.Player0),
                Wz1MatchFixtures.Player(
                    PlayerId.Player0,
                    Wz1MatchFixtures.HeroId(1),
                    Wz1MatchFixtures.ClassId(1),
                    Wz1MatchFixtures.MainDeck40()));
            var result = new RulesEngine(Wz1MatchFixtures.Catalog).Submit(null, Command.CreateMatch("c1", setup));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            StringAssert.Contains("Duplicate PlayerId", result.RejectionReason);
        }

        [Test]
        public void CreateMatch_Rejection_LeavesNoPartialState()
        {
            var engine = new RulesEngine(Wz1MatchFixtures.Catalog);
            var result = engine.Submit(
                null,
                Command.CreateMatch(
                    "c1",
                    new MatchSetup(
                        1UL,
                        RuleConfig.AlphaDefaults(),
                        Wz1MatchFixtures.Catalog.ContentVersion,
                        Wz1MatchFixtures.Player(
                            PlayerId.Player0,
                            Wz1MatchFixtures.HeroId(0),
                            Wz1MatchFixtures.ClassId(0),
                            Wz1MatchFixtures.MainDeckOfSize(39)),
                        Wz1MatchFixtures.Seat(PlayerId.Player1))));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void GameSession_StartRequest_SeatsByPlayerId()
        {
            var session = new GameSession(Wz1MatchFixtures.Catalog);
            var request = new MatchSetupRequest(
                Wz1MatchFixtures.Seat(PlayerId.Player1),
                Wz1MatchFixtures.Seat(PlayerId.Player0),
                11UL,
                RuleConfig.AlphaRuleVersion,
                Wz1MatchFixtures.Catalog.ContentVersion);
            var result = session.Start(request, RuleConfig.AlphaDefaults());
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            Assert.AreEqual(Wz1MatchFixtures.HeroId(0), session.State.Players[0].HeroId);
            Assert.AreEqual(Wz1MatchFixtures.HeroId(1), session.State.Players[1].HeroId);
        }

        [Test]
        public void GameSession_StartRequest_RejectsDuplicatePlayerIdWithoutState()
        {
            var session = new GameSession(Wz1MatchFixtures.Catalog);
            var request = new MatchSetupRequest(
                Wz1MatchFixtures.Seat(PlayerId.Player0),
                Wz1MatchFixtures.Player(
                    PlayerId.Player0,
                    Wz1MatchFixtures.HeroId(1),
                    Wz1MatchFixtures.ClassId(1),
                    Wz1MatchFixtures.MainDeck40()),
                11UL,
                RuleConfig.AlphaRuleVersion,
                Wz1MatchFixtures.Catalog.ContentVersion);
            var result = session.Start(request, RuleConfig.AlphaDefaults());
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            Assert.IsNull(session.State);
            StringAssert.Contains("Duplicate PlayerId", result.RejectionReason);
        }

        [Test]
        public void GameSession_StartRequest_RejectsIncompatibleVersions()
        {
            var session = new GameSession(Wz1MatchFixtures.Catalog);
            var rules = RuleConfig.AlphaDefaults();
            var badContent = new MatchSetupRequest(
                Wz1MatchFixtures.Seat(PlayerId.Player0),
                Wz1MatchFixtures.Seat(PlayerId.Player1),
                1UL,
                rules.RuleVersion,
                "not-wz1");
            var contentResult = session.Start(badContent, rules);
            Assert.IsFalse(contentResult.Accepted);
            Assert.IsNull(session.State);
            StringAssert.Contains("content version", contentResult.RejectionReason);

            var badRules = new MatchSetupRequest(
                Wz1MatchFixtures.Seat(PlayerId.Player0),
                Wz1MatchFixtures.Seat(PlayerId.Player1),
                1UL,
                "9.9.9",
                Wz1MatchFixtures.Catalog.ContentVersion);
            var rulesResult = session.Start(badRules, rules);
            Assert.IsFalse(rulesResult.Accepted);
            Assert.IsNull(session.State);
            StringAssert.Contains("rule version", rulesResult.RejectionReason);
        }

        private static CommandResult Create(ulong seed)
        {
            var result = new RulesEngine(Wz1MatchFixtures.Catalog)
                .Submit(null, Command.CreateMatch("c1", Wz1MatchFixtures.Standard(seed)));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            return result;
        }

        private static void AssertRejected(PlayerSetup invalidPlayer0, string reasonFragment)
        {
            var setup = new MatchSetup(
                1UL,
                RuleConfig.AlphaDefaults(),
                Wz1MatchFixtures.Catalog.ContentVersion,
                invalidPlayer0,
                Wz1MatchFixtures.Seat(PlayerId.Player1));
            var result = new RulesEngine(Wz1MatchFixtures.Catalog).Submit(null, Command.CreateMatch("c1", setup));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            StringAssert.Contains(reasonFragment, result.RejectionReason);
        }

        private static bool SameDefinitionOrder(GameState a, int playerIndex, GameState b)
        {
            var left = OpeningDefinitions(a, playerIndex);
            var right = OpeningDefinitions(b, playerIndex);
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static List<CardId> OpeningDefinitions(GameState state, int playerIndex)
        {
            var player = state.Players[playerIndex];
            var order = new List<CardId>(player.Hand.Count + player.Deck.Count);
            AppendDefinitions(state, player.Hand, order);
            AppendDefinitions(state, player.Deck, order);
            return order;
        }

        private static void AppendDefinitions(
            GameState state,
            IReadOnlyList<CardInstanceId> ids,
            List<CardId> target)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                target.Add(state.Cards[ids[i]].DefinitionId);
            }
        }

        private static bool Contains(IReadOnlyList<CardInstanceId> ids, CardInstanceId target)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
