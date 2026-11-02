using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class TutorialMatchFixtureTests
    {
        [Test]
        public void TutorialDecks_AreLegal_AndCreateMatchOpensMulligan()
        {
            var fixture = TutorialMatchFixture.Create(Wz1MatchFixtures.Catalog);
            Assert.AreEqual(40, fixture.Learner.MainDeckCardIds.Count);
            Assert.AreEqual(40, fixture.Tutor.MainDeckCardIds.Count);

            var engine = new RulesEngine(Wz1MatchFixtures.Catalog);
            var result = engine.Submit(null, Command.CreateMatch("tut-1", fixture.ToMatchSetup()));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            Assert.AreEqual(MatchPhase.MulliganP0, result.State.Phase);
            Assert.AreEqual(PlayerId.Player0, result.State.FirstPlayerId);
        }

        [Test]
        public void TutorialDecks_PreserveOrder_OpeningHandIsScripted()
        {
            var fixture = TutorialMatchFixture.Create(Wz1MatchFixtures.Catalog);
            var engine = new RulesEngine(Wz1MatchFixtures.Catalog);
            var result = engine.Submit(null, Command.CreateMatch("tut-2", fixture.ToMatchSetup()));
            Assert.IsTrue(result.Accepted, result.RejectionReason);

            var hand = result.State.GetPlayer(PlayerId.Player0).Hand;
            Assert.AreEqual(5, hand.Count);
            for (var i = 0; i < TutorialMatchFixture.LearnerOpeningHand.Length; i++)
            {
                Assert.IsTrue(result.State.TryGetCard(hand[i], out var card));
                Assert.AreEqual(
                    TutorialMatchFixture.LearnerOpeningHand[i],
                    card.DefinitionId.Value,
                    "Hand index " + i);
            }
        }

        [Test]
        public void TutorialDecks_UseDistinctHeroes()
        {
            var fixture = TutorialMatchFixture.Create(Wz1MatchFixtures.Catalog);
            Assert.AreNotEqual(fixture.Learner.HeroCardId, fixture.Tutor.HeroCardId);
            Assert.AreEqual(new CardId("WZ1-001"), fixture.Learner.HeroCardId);
            Assert.AreEqual(new CardId("WZ1-002"), fixture.Tutor.HeroCardId);
        }

        [Test]
        public void TutorialRules_UseShortResolutionForVictoryPath()
        {
            var fixture = TutorialMatchFixture.Create(Wz1MatchFixtures.Catalog);
            Assert.AreEqual(1, fixture.Rules.InitialResolution);
        }

        [Test]
        public void TutorialMainDecks_NeverExceedThreeCopies()
        {
            AssertMaxCopies(TutorialMatchFixture.LearnerMainDeck());
            AssertMaxCopies(TutorialMatchFixture.TutorMainDeck());
        }

        private static void AssertMaxCopies(System.Collections.Generic.IReadOnlyList<CardId> deck)
        {
            var counts = new System.Collections.Generic.Dictionary<string, int>();
            for (var i = 0; i < deck.Count; i++)
            {
                var key = deck[i].Value;
                counts.TryGetValue(key, out var n);
                counts[key] = n + 1;
            }

            foreach (var pair in counts)
            {
                Assert.LessOrEqual(pair.Value, 3, pair.Key);
            }
        }
    }
}
