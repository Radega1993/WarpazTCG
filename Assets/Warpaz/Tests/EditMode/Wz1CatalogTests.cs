using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class Wz1CatalogTests
    {
        [Test]
        public void Catalog_LoadsAll120Cards()
        {
            var result = LoadOfficial();
            Assert.IsTrue(result.Success, FormatErrors(result));
            Assert.AreEqual(ContentManifest.ExpectedCardCount, result.Catalog.Count);
            Assert.AreEqual(ContentManifest.ExpectedCardCount, result.Catalog.All.Count);
            Assert.AreEqual(ContentManifest.ExpectedContentVersion, result.Report.ContentVersion);
            Assert.AreEqual(0, result.Report.Errors.Count);
        }

        [Test]
        public void Catalog_FindsWz1_001()
        {
            var catalog = LoadOfficial().Catalog;
            Assert.IsTrue(catalog.TryGet(new CardId("WZ1-001"), out var definition));
            Assert.AreEqual("Aedran, Vigía Akkedis", definition.Name);
            Assert.AreEqual(CardType.Hero, definition.Type);
        }

        [Test]
        public void Catalog_Wz1_001_ReturnsExactlyOneDefinition()
        {
            var catalog = LoadOfficial().Catalog;
            var matches = 0;
            for (var i = 0; i < catalog.All.Count; i++)
            {
                if (catalog.All[i].CardId == new CardId("WZ1-001"))
                {
                    matches++;
                }
            }

            Assert.AreEqual(1, matches);
            Assert.IsTrue(catalog.TryGet(new CardId("WZ1-001"), out _));
        }

        [Test]
        public void Catalog_Ids_AreCorrelativeAndUnique()
        {
            var catalog = LoadOfficial().Catalog;
            var seen = new HashSet<CardId>();
            for (var n = 1; n <= ContentManifest.ExpectedCardCount; n++)
            {
                var id = new CardId("WZ1-" + n.ToString("000", CultureInfo.InvariantCulture));
                Assert.IsTrue(catalog.TryGet(id, out var card), "Missing " + id);
                Assert.AreEqual(n, card.Number);
                Assert.AreNotEqual(card.Name, card.CardId.Value);
                Assert.IsTrue(seen.Add(id));
            }

            Assert.AreEqual(ContentManifest.ExpectedCardCount, seen.Count);
            Assert.AreEqual(ContentManifest.ExpectedCardCount, catalog.Count);
        }

        [Test]
        public void Catalog_TypeCounts_MatchWz1()
        {
            var catalog = LoadOfficial().Catalog;
            Assert.AreEqual(60, catalog.Filter(type: CardType.Character).Count);
            Assert.AreEqual(20, catalog.Filter(type: CardType.Action).Count);
            Assert.AreEqual(13, catalog.Filter(type: CardType.Equipment).Count);
            Assert.AreEqual(11, catalog.Filter(type: CardType.Hero).Count);
            Assert.AreEqual(10, catalog.Filter(type: CardType.Reaction).Count);
            Assert.AreEqual(6, catalog.Filter(type: CardType.ClassPath).Count);
            Assert.Greater(catalog.Filter(race: "Akkedis").Count, 0);
            Assert.Greater(catalog.Filter(classPath: "Guerrero").Count, 0);
            Assert.Greater(catalog.Filter(costPa: 1).Count, 0);
            Assert.Greater(catalog.Filter(rarity: "Identidad").Count, 0);
            Assert.Greater(catalog.Filter(keyword: "Ascenso").Count, 0);
        }

        [Test]
        public void Catalog_NullableFields_LoadAsNull()
        {
            var catalog = LoadOfficial().Catalog;
            Assert.IsTrue(catalog.TryGet(new CardId("WZ1-001"), out var hero));
            Assert.IsNull(hero.CostPa);
            Assert.IsNull(hero.Response);
            Assert.IsNull(hero.ClassPath);
            Assert.AreEqual(5, hero.Power);

            Assert.IsTrue(catalog.TryGet(new CardId("WZ1-012"), out var classPath));
            Assert.IsNull(classPath.Race);
            Assert.IsNull(classPath.CostPa);
            Assert.IsNull(classPath.Power);
            Assert.IsNull(classPath.Response);
            Assert.AreEqual("Guerrero", classPath.ClassPath);

            Assert.IsTrue(catalog.TryGet(new CardId("WZ1-120"), out var equipment));
            Assert.IsNotNull(equipment.CostPa);
            Assert.IsNotNull(equipment.Response);
        }

        [Test]
        public void Catalog_DuplicateCardId_Fails()
        {
            var temp = CopyOfficialToTemp();
            try
            {
                var catalogPath = Path.Combine(temp, ContentManifest.CatalogFileName);
                var json = File.ReadAllText(catalogPath, Encoding.UTF8);
                json = json.Replace("\"cardId\": \"WZ1-002\"", "\"cardId\": \"WZ1-001\"");
                var bytes = Encoding.UTF8.GetBytes(json);
                File.WriteAllBytes(catalogPath, bytes);

                var manifestPath = Path.Combine(temp, ContentManifest.ManifestFileName);
                var manifest = File.ReadAllText(manifestPath, Encoding.UTF8);
                File.WriteAllText(manifestPath, ReplaceHash(manifest, Sha256Hex(bytes)), new UTF8Encoding(false));

                var result = CardCatalogLoader.LoadFromDirectory(temp);
                Assert.IsFalse(result.Success);
                Assert.IsNull(result.Catalog);
                Assert.IsTrue(HasError(result, "WZ1-001", "cardId"));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public void Catalog_AlteredHash_Fails()
        {
            var temp = CopyOfficialToTemp();
            try
            {
                var manifestPath = Path.Combine(temp, ContentManifest.ManifestFileName);
                var manifest = File.ReadAllText(manifestPath, Encoding.UTF8);
                File.WriteAllText(
                    manifestPath,
                    ReplaceHash(manifest, "0000000000000000000000000000000000000000000000000000000000000000"),
                    new UTF8Encoding(false));

                var result = CardCatalogLoader.LoadFromDirectory(temp);
                Assert.IsFalse(result.Success);
                Assert.IsNull(result.Catalog);
                Assert.IsTrue(HasError(result, null, "catalogSha256"));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public void Catalog_RejectsExternalMutation()
        {
            var catalog = LoadOfficial().Catalog;
            var all = catalog.All as IList<CardDefinition>;
            Assert.IsNotNull(all);
            Assert.IsTrue(all.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => all.Clear());
            Assert.AreEqual(120, catalog.Count);
            Assert.IsTrue(catalog.TryGet(new CardId("WZ1-001"), out var definition));

            var keywords = definition.Keywords as IList<string>;
            Assert.IsNotNull(keywords);
            Assert.IsTrue(keywords.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => keywords.Clear());
            Assert.Greater(definition.Keywords.Count, 0);
        }

        [Test]
        public void Catalog_RenamingDefinition_DoesNotChangeCardId()
        {
            var catalog = LoadOfficial().Catalog;
            Assert.IsTrue(catalog.TryGet(new CardId("WZ1-001"), out var original));
            var renamed = WithName(original, "Renamed test identity");
            Assert.AreEqual(original.CardId, renamed.CardId);
            Assert.AreNotEqual(original.Name, renamed.Name);
            Assert.IsTrue(catalog.TryGet(renamed.CardId, out var indexed));
            Assert.AreEqual(original.Name, indexed.Name);
            Assert.AreEqual(1, CountById(catalog, original.CardId));
        }

        [Test]
        public void CatalogJson_OmitsControlColumns()
        {
            var json = File.ReadAllText(CatalogPath(), Encoding.UTF8);
            StringAssert.DoesNotContain("Control ID", json);
            StringAssert.DoesNotContain("Control stats", json);
        }

        private static CardCatalogLoadResult LoadOfficial()
        {
            var result = CardCatalogLoader.LoadFromDirectory(DataDirectory());
            Assert.IsTrue(result.Success, FormatErrors(result));
            Assert.IsNotNull(result.Catalog);
            return result;
        }

        private static string DataDirectory()
        {
            return Path.Combine(UnityEngine.Application.dataPath, "StreamingAssets", "Data");
        }

        private static string CatalogPath()
        {
            return Path.Combine(DataDirectory(), ContentManifest.CatalogFileName);
        }

        private static string CopyOfficialToTemp()
        {
            var temp = Path.Combine(Path.GetTempPath(), "warpaz-wz1-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            File.Copy(CatalogPath(), Path.Combine(temp, ContentManifest.CatalogFileName));
            File.Copy(
                Path.Combine(DataDirectory(), ContentManifest.ManifestFileName),
                Path.Combine(temp, ContentManifest.ManifestFileName));
            return temp;
        }

        private static string ReplaceHash(string manifest, string hash)
        {
            const string key = "\"catalogSha256\": \"";
            var start = manifest.IndexOf(key, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0);
            start += key.Length;
            var end = manifest.IndexOf('"', start);
            return manifest.Substring(0, start) + hash + manifest.Substring(end);
        }

        private static string Sha256Hex(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private static bool HasError(CardCatalogLoadResult result, string cardId, string field)
        {
            var errors = result.Report.Errors;
            for (var i = 0; i < errors.Count; i++)
            {
                if (errors[i].Field == field && errors[i].CardId == cardId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountById(ICardCatalog catalog, CardId cardId)
        {
            var count = 0;
            for (var i = 0; i < catalog.All.Count; i++)
            {
                if (catalog.All[i].CardId == cardId)
                {
                    count++;
                }
            }

            return count;
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

        private static string FormatErrors(CardCatalogLoadResult result)
        {
            if (result == null || result.Report == null)
            {
                return "Load failed without a report.";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < result.Report.Errors.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(result.Report.Errors[i]);
            }

            return builder.ToString();
        }
    }
}
