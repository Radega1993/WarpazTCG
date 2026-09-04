using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class RuleConfigTests
    {
        [Test]
        public void AlphaDefaults_MatchSpecificationV01()
        {
            var config = RuleConfig.AlphaDefaults();

            Assert.AreEqual(RuleConfig.AlphaRuleVersion, config.RuleVersion);
            Assert.AreEqual(5, config.InitialHandSize);
            Assert.AreEqual(5, config.InitialResolution);
            Assert.AreEqual(40, config.DeckSize);
            Assert.AreEqual(3, config.MaxCopiesPerCard);
            Assert.AreEqual(0, config.InitialPaMax);
            Assert.AreEqual(1, config.PaIncreasePerTurn);
            Assert.AreEqual(8, config.PaLimit);
            Assert.AreEqual(5, config.AscensionRequiredPaMax);
            Assert.AreEqual(2, config.AscensionCost);
            Assert.AreEqual(1, config.MaximumEquipmentPerCharacter);
            Assert.AreEqual(1, config.FreeReorganizationsPerTurn);
            Assert.IsNull(config.HandLimit);
            Assert.AreEqual(1, config.FailedDrawResolutionLoss);
            Assert.AreEqual(RuleConfig.DefaultMaximumEventsPerTurn, config.MaximumEventsPerTurn);
            Assert.AreEqual(RuleConfig.DefaultMaximumDecisionsPerTurn, config.MaximumDecisionsPerTurn);
            Assert.Greater(config.MaximumEventsPerTurn, 0);
            Assert.Greater(config.MaximumDecisionsPerTurn, 0);
        }

        [Test]
        public void ContentManifest_CoversStableWz1Range()
        {
            Assert.AreEqual("WZ1-v0.1", ContentManifest.ExpectedContentVersion);
            Assert.AreEqual("WZ1", ContentManifest.ExpectedSetId);
            Assert.AreEqual(120, ContentManifest.ExpectedCardCount);
            Assert.AreEqual("WZ1-001", ContentManifest.FirstCardId);
            Assert.AreEqual("WZ1-120", ContentManifest.LastCardId);
            Assert.IsTrue(CardId.IsValid(ContentManifest.FirstCardId));
            Assert.IsTrue(CardId.IsValid(ContentManifest.LastCardId));
        }
    }
}
