using System.IO;
using System.Text;
using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class RuleConfigLoaderTests
    {
        [Test]
        public void LoadFromDirectory_MatchesAlphaDefaults()
        {
            var result = RuleConfigLoader.LoadFromDirectory(DataDirectory());
            Assert.IsTrue(result.Success, FormatErrors(result));
            Assert.AreEqual(RuleConfig.SchemaVersion, result.SchemaVersion);
            Assert.AreEqual(RuleConfig.AlphaRuleVersion, result.RuleVersion);

            var expected = RuleConfig.AlphaDefaults();
            var config = result.Config;
            Assert.AreEqual(expected.RuleVersion, config.RuleVersion);
            Assert.AreEqual(expected.InitialHandSize, config.InitialHandSize);
            Assert.AreEqual(expected.InitialResolution, config.InitialResolution);
            Assert.AreEqual(expected.DeckSize, config.DeckSize);
            Assert.AreEqual(expected.MaxCopiesPerCard, config.MaxCopiesPerCard);
            Assert.AreEqual(expected.InitialPaMax, config.InitialPaMax);
            Assert.AreEqual(expected.PaIncreasePerTurn, config.PaIncreasePerTurn);
            Assert.AreEqual(expected.PaLimit, config.PaLimit);
            Assert.AreEqual(expected.AscensionRequiredPaMax, config.AscensionRequiredPaMax);
            Assert.AreEqual(expected.AscensionCost, config.AscensionCost);
            Assert.AreEqual(expected.MaximumEquipmentPerCharacter, config.MaximumEquipmentPerCharacter);
            Assert.AreEqual(expected.FreeReorganizationsPerTurn, config.FreeReorganizationsPerTurn);
            Assert.AreEqual(expected.HandLimit, config.HandLimit);
            Assert.AreEqual(expected.FailedDrawResolutionLoss, config.FailedDrawResolutionLoss);
            Assert.AreEqual(expected.MaximumEventsPerTurn, config.MaximumEventsPerTurn);
            Assert.AreEqual(expected.MaximumDecisionsPerTurn, config.MaximumDecisionsPerTurn);
        }

        [Test]
        public void LoadFromDirectory_AlteredRuleVersion_Fails()
        {
            var temp = CopyOfficialToTemp();
            try
            {
                var path = Path.Combine(temp, RuleConfigLoader.FileName);
                var json = File.ReadAllText(path, Encoding.UTF8);
                json = json.Replace("\"ruleVersion\": \"0.1.0\"", "\"ruleVersion\": \"9.9.9\"");
                File.WriteAllText(path, json, new UTF8Encoding(false));

                var result = RuleConfigLoader.LoadFromDirectory(temp);
                Assert.IsFalse(result.Success);
                Assert.IsNull(result.Config);
                Assert.IsTrue(HasError(result, "ruleVersion"));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public void LoadFromDirectory_AlteredKnob_Fails()
        {
            var temp = CopyOfficialToTemp();
            try
            {
                var path = Path.Combine(temp, RuleConfigLoader.FileName);
                var json = File.ReadAllText(path, Encoding.UTF8);
                json = json.Replace("\"deckSize\": 40", "\"deckSize\": 39");
                File.WriteAllText(path, json, new UTF8Encoding(false));

                var result = RuleConfigLoader.LoadFromDirectory(temp);
                Assert.IsFalse(result.Success);
                Assert.IsNull(result.Config);
                Assert.IsTrue(HasError(result, "deckSize"));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        private static string DataDirectory()
        {
            return Path.Combine(UnityEngine.Application.dataPath, "StreamingAssets", "Data");
        }

        private static string CopyOfficialToTemp()
        {
            var temp = Path.Combine(Path.GetTempPath(), "warpaz-rules-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            File.Copy(
                Path.Combine(DataDirectory(), RuleConfigLoader.FileName),
                Path.Combine(temp, RuleConfigLoader.FileName));
            return temp;
        }

        private static bool HasError(RuleConfigLoadResult result, string field)
        {
            for (var i = 0; i < result.Errors.Count; i++)
            {
                if (result.Errors[i].Field == field)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatErrors(RuleConfigLoadResult result)
        {
            if (result == null || result.Errors == null || result.Errors.Count == 0)
            {
                return "Load failed without errors.";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < result.Errors.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(result.Errors[i]);
            }

            return builder.ToString();
        }
    }
}
