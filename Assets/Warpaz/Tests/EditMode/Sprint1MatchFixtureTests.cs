using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class Sprint1MatchFixtureTests
    {
        [Test]
        public void Factory_BuildsLegalWz1Seats_AndMatchFactoryAccepts()
        {
            var catalog = Wz1MatchFixtures.Catalog;
            var options = new Sprint1MatchFixtureOptions(
                11UL,
                "Ada",
                "Ben",
                MulliganDecision.ReplaceWholeHand,
                false);
            var fixture = Sprint1MatchFixtureFactory.Create(catalog, options);

            Assert.AreEqual(11UL, fixture.Seed);
            Assert.AreEqual("Ada", fixture.Player1Name);
            Assert.AreEqual("Ben", fixture.Player2Name);
            Assert.AreEqual(MulliganDecision.ReplaceWholeHand, fixture.MulliganDecision);
            Assert.IsFalse(fixture.DebugVisibility);
            Assert.AreEqual(PlayerId.Player0, fixture.Player0.PlayerId);
            Assert.AreEqual(PlayerId.Player1, fixture.Player1.PlayerId);
            Assert.AreNotEqual(fixture.Player0.PlayerId, fixture.Player1.PlayerId);

            AssertLegalSeat(catalog, fixture.Player0, "Ada");
            AssertLegalSeat(catalog, fixture.Player1, "Ben");
            AssertAllReferencesExist(catalog, fixture);

            var created = MatchFactory.TryCreate(catalog, fixture.ToMatchSetup());
            Assert.IsTrue(created.Accepted, created.RejectionReason);
            Assert.AreEqual(MatchPhase.MulliganP0, created.State.Phase);
        }

        [Test]
        public void Factory_DebugVisibility_DefaultsOff_AndCanBeEnabled()
        {
            var catalog = Wz1MatchFixtures.Catalog;
            var defaults = Sprint1MatchFixtureFactory.Create(catalog);
            Assert.IsFalse(defaults.DebugVisibility);
            Assert.AreEqual(MulliganDecision.KeepHand, defaults.MulliganDecision);
            Assert.AreEqual(Sprint1MatchFixtureOptions.DefaultPlayer1Name, defaults.Player1Name);

            var debug = Sprint1MatchFixtureFactory.Create(
                catalog,
                new Sprint1MatchFixtureOptions(debugVisibility: true));
            Assert.IsTrue(debug.DebugVisibility);
        }

        [Test]
        public void Factory_RenamingCatalogCards_DoesNotChangeIdsOrAcceptance()
        {
            var catalog = Wz1MatchFixtures.Catalog;
            var options = new Sprint1MatchFixtureOptions(3UL, "P1", "P2");
            var original = Sprint1MatchFixtureFactory.Create(catalog, options);
            var renamedCatalog = CatalogWithRenamedCards(catalog);
            var renamed = Sprint1MatchFixtureFactory.Create(renamedCatalog, options);

            CollectionAssert.AreEqual(original.ReferencedCardIds(), renamed.ReferencedCardIds());
            Assert.AreEqual(original.Player0.HeroCardId, renamed.Player0.HeroCardId);
            Assert.AreEqual(original.Player1.ClassCardId, renamed.Player1.ClassCardId);

            Assert.IsTrue(catalog.TryGet(original.Player0.HeroCardId, out var before));
            Assert.IsTrue(renamedCatalog.TryGet(original.Player0.HeroCardId, out var after));
            Assert.AreNotEqual(before.Name, after.Name);

            var created = MatchFactory.TryCreate(renamedCatalog, renamed.ToMatchSetup());
            Assert.IsTrue(created.Accepted, created.RejectionReason);
            Assert.AreEqual(original.Player0.HeroCardId, created.State.Players[0].HeroId);
        }

        private static void AssertAllReferencesExist(ICardCatalog catalog, Sprint1MatchFixture fixture)
        {
            var ids = fixture.ReferencedCardIds();
            Assert.Greater(ids.Count, 0);
            for (var i = 0; i < ids.Count; i++)
            {
                Assert.IsTrue(catalog.TryGet(ids[i], out _), "Missing " + ids[i]);
            }
        }

        private static void AssertLegalSeat(ICardCatalog catalog, PlayerSetup setup, string displayName)
        {
            Assert.AreEqual(displayName, setup.DisplayName);
            Assert.AreEqual(40, setup.MainDeckCardIds.Count);

            Assert.IsTrue(catalog.TryGet(setup.HeroCardId, out var hero));
            Assert.AreEqual(CardType.Hero, hero.Type);
            Assert.IsTrue(catalog.TryGet(setup.ClassCardId, out var classPath));
            Assert.AreEqual(CardType.ClassPath, classPath.Type);

            var copies = new Dictionary<CardId, int>();
            for (var i = 0; i < setup.MainDeckCardIds.Count; i++)
            {
                var id = setup.MainDeckCardIds[i];
                Assert.IsTrue(catalog.TryGet(id, out var card), "Missing " + id);
                Assert.IsFalse(card.IsIdentity, id + " is an identity in the main deck.");
                Assert.AreNotEqual(setup.HeroCardId, id);
                Assert.AreNotEqual(setup.ClassCardId, id);
                copies.TryGetValue(id, out var count);
                count++;
                Assert.LessOrEqual(count, 3, id + " exceeds 3 copies.");
                copies[id] = count;
            }

            var rules = RuleConfig.AlphaDefaults();
            var validation = DeckValidator.Validate(setup, catalog, rules);
            Assert.IsTrue(validation.IsValid, validation.Reason);
        }

        private static CardCatalog CatalogWithRenamedCards(ICardCatalog source)
        {
            var cards = new List<CardDefinition>(source.Count);
            for (var i = 0; i < source.All.Count; i++)
            {
                var card = source.All[i];
                cards.Add(WithName(card, "Renamed " + card.CardId.Value));
            }

            return CardCatalog.Create(cards, source.ContentVersion);
        }

        private static CardDefinition WithName(CardDefinition source, string name)
        {
            return new CardDefinition(
                source.CardId,
                source.SetId,
                source.Number,
                name,
                source.Type,
                source.Subtype,
                source.Race,
                source.ClassPath,
                source.CostPa,
                source.Power,
                source.Response,
                source.Rarity,
                source.Unique,
                source.Position,
                source.Keywords,
                source.EffectText,
                source.Flavor,
                source.Archetype,
                source.Canon,
                source.Complexity,
                source.BalanceNotes,
                source.Effects);
        }
    }
}
