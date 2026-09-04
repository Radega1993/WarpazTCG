using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class CreateMatchTests
    {
        [Test]
        public void CreateMatch_ValidDecks_OpensMulliganAndDrawsFive()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var result = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(1UL)));

            Assert.IsTrue(result.Accepted, result.RejectionReason);
            Assert.AreEqual(MatchPhase.MulliganP0, result.State.Phase);
            Assert.AreEqual(PlayerId.Player0, result.State.PriorityPlayerId);
            Assert.AreEqual(5, result.State.Players[0].Hand.Count);
            Assert.AreEqual(5, result.State.Players[1].Hand.Count);
            Assert.AreEqual(35, result.State.Players[0].Deck.Count);
            Assert.AreEqual(0, result.State.Players[0].PaMax);
            Assert.AreEqual(5, result.State.Players[0].Resolution);
            Assert.IsNull(GameStateInvariants.FindViolation(result.State));
        }

        [Test]
        public void CreateMatch_Rejects_39Cards()
        {
            var setup = new MatchSetup(
                1UL,
                RuleConfig.AlphaDefaults(),
                MatchFixtures.Catalog.ContentVersion,
                MatchFixtures.Player(
                    PlayerId.Player0,
                    MinimalCatalog.HeroA,
                    MinimalCatalog.ClassA,
                    MatchFixtures.DeckOfSize(39)),
                MatchFixtures.Player(
                    PlayerId.Player1,
                    MinimalCatalog.HeroB,
                    MinimalCatalog.ClassB,
                    MinimalCatalog.StandardDeck40()));
            var result = new RulesEngine(MatchFixtures.Catalog).Submit(null, Command.CreateMatch("c1", setup));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            StringAssert.Contains("40", result.RejectionReason);
        }

        [Test]
        public void CreateMatch_Rejects_FourthCopy()
        {
            var deck = new List<CardId>(MinimalCatalog.StandardDeck40());
            deck[39] = MinimalCatalog.VanillaId(1);
            var setup = new MatchSetup(
                1UL,
                RuleConfig.AlphaDefaults(),
                MatchFixtures.Catalog.ContentVersion,
                MatchFixtures.Player(PlayerId.Player0, MinimalCatalog.HeroA, MinimalCatalog.ClassA, deck),
                MatchFixtures.Player(
                    PlayerId.Player1,
                    MinimalCatalog.HeroB,
                    MinimalCatalog.ClassB,
                    MinimalCatalog.StandardDeck40()));
            var result = new RulesEngine(MatchFixtures.Catalog).Submit(null, Command.CreateMatch("c1", setup));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            StringAssert.Contains("3-copy", result.RejectionReason);
        }

        [Test]
        public void CreateMatch_Rejects_MissingIdentity()
        {
            var setup = new MatchSetup(
                1UL,
                RuleConfig.AlphaDefaults(),
                MatchFixtures.Catalog.ContentVersion,
                MatchFixtures.Player(
                    PlayerId.Player0,
                    new CardId("WZ9-999"),
                    MinimalCatalog.ClassA,
                    MinimalCatalog.StandardDeck40()),
                MatchFixtures.Player(
                    PlayerId.Player1,
                    MinimalCatalog.HeroB,
                    MinimalCatalog.ClassB,
                    MinimalCatalog.StandardDeck40()));
            var result = new RulesEngine(MatchFixtures.Catalog).Submit(null, Command.CreateMatch("c1", setup));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            StringAssert.Contains("Identity", result.RejectionReason);
        }

        [Test]
        public void CreateMatch_Rejects_UnknownId()
        {
            var deck = new List<CardId>(MinimalCatalog.StandardDeck40());
            deck[0] = new CardId("WZ1-999");
            var setup = new MatchSetup(
                1UL,
                RuleConfig.AlphaDefaults(),
                MatchFixtures.Catalog.ContentVersion,
                MatchFixtures.Player(PlayerId.Player0, MinimalCatalog.HeroA, MinimalCatalog.ClassA, deck),
                MatchFixtures.Player(
                    PlayerId.Player1,
                    MinimalCatalog.HeroB,
                    MinimalCatalog.ClassB,
                    MinimalCatalog.StandardDeck40()));
            var result = new RulesEngine(MatchFixtures.Catalog).Submit(null, Command.CreateMatch("c1", setup));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            StringAssert.Contains("Unknown", result.RejectionReason);
        }

        [Test]
        public void CreateMatch_Rejects_DuplicatePlayerId()
        {
            var setup = new MatchSetup(
                1UL,
                RuleConfig.AlphaDefaults(),
                MatchFixtures.Catalog.ContentVersion,
                MatchFixtures.Player(
                    PlayerId.Player0,
                    MinimalCatalog.HeroA,
                    MinimalCatalog.ClassA,
                    MinimalCatalog.StandardDeck40()),
                MatchFixtures.Player(
                    PlayerId.Player0,
                    MinimalCatalog.HeroB,
                    MinimalCatalog.ClassB,
                    MinimalCatalog.StandardDeck40()));
            var result = new RulesEngine(MatchFixtures.Catalog).Submit(null, Command.CreateMatch("c1", setup));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            StringAssert.Contains("Duplicate PlayerId", result.RejectionReason);
        }

        [Test]
        public void CreateMatch_Rejects_IncompatibleContentVersion()
        {
            var setup = new MatchSetup(
                1UL,
                RuleConfig.AlphaDefaults(),
                "not-a-content-version",
                MatchFixtures.Player(
                    PlayerId.Player0,
                    MinimalCatalog.HeroA,
                    MinimalCatalog.ClassA,
                    MinimalCatalog.StandardDeck40()),
                MatchFixtures.Player(
                    PlayerId.Player1,
                    MinimalCatalog.HeroB,
                    MinimalCatalog.ClassB,
                    MinimalCatalog.StandardDeck40()));
            var result = new RulesEngine(MatchFixtures.Catalog).Submit(null, Command.CreateMatch("c1", setup));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.State);
            StringAssert.Contains("content version", result.RejectionReason);
        }
    }
}
