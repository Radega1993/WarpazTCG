using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Warpaz.Domain;

namespace Warpaz.Content
{
    public sealed class CardCatalogLoadResult
    {
        public CardCatalogLoadResult(CardCatalog catalog, ContentManifest manifest, ContentValidationReport report)
        {
            Catalog = catalog;
            Manifest = manifest;
            Report = report;
        }

        public CardCatalog Catalog { get; }
        public ContentManifest Manifest { get; }
        public ContentValidationReport Report { get; }

        public bool Success => Catalog != null && Report != null && Report.IsValid;
    }

    /// <summary>
    /// Loads WZ1 from StreamingAssets using Unity's official Newtonsoft package.
    /// Does not interpret <c>effects</c> or <c>effectText</c>, and does not create match state.
    /// </summary>
    public static class CardCatalogLoader
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore
        };

        public static CardCatalogLoadResult LoadFromDirectory(string dataDirectory)
        {
            var prior = new List<ContentValidationIssue>();
            if (string.IsNullOrEmpty(dataDirectory))
            {
                prior.Add(new ContentValidationIssue(null, "dataDirectory", "Content data directory is missing."));
                return Fail(null, null, prior);
            }

            var catalogPath = Path.Combine(dataDirectory, ContentManifest.CatalogFileName);
            var manifestPath = Path.Combine(dataDirectory, ContentManifest.ManifestFileName);
            if (!File.Exists(catalogPath))
            {
                prior.Add(new ContentValidationIssue(null, "catalogFile", "Missing catalog file: " + catalogPath + "."));
            }

            if (!File.Exists(manifestPath))
            {
                prior.Add(new ContentValidationIssue(null, "manifest", "Missing manifest file: " + manifestPath + "."));
            }

            if (prior.Count > 0)
            {
                return Fail(null, null, prior);
            }

            var catalogBytes = File.ReadAllBytes(catalogPath);
            var actualHash = Sha256Hex(catalogBytes);
            var catalogJson = Encoding.UTF8.GetString(catalogBytes);
            if (catalogJson.Length > 0 && catalogJson[0] == '\ufeff')
            {
                catalogJson = catalogJson.Substring(1);
            }
            var manifestJson = File.ReadAllText(manifestPath, Encoding.UTF8);

            CatalogFileDto catalogDto;
            ManifestDto manifestDto;
            try
            {
                catalogDto = JsonConvert.DeserializeObject<CatalogFileDto>(catalogJson, SerializerSettings);
            }
            catch (JsonException ex)
            {
                prior.Add(new ContentValidationIssue(null, "catalogFile", "Catalog JSON is invalid: " + ex.Message));
                catalogDto = null;
            }

            try
            {
                manifestDto = JsonConvert.DeserializeObject<ManifestDto>(manifestJson, SerializerSettings);
            }
            catch (JsonException ex)
            {
                prior.Add(new ContentValidationIssue(null, "manifest", "Manifest JSON is invalid: " + ex.Message));
                manifestDto = null;
            }

            var manifest = manifestDto == null ? null : ToManifest(manifestDto);
            if (catalogDto == null)
            {
                return Fail(manifest, actualHash, prior);
            }

            if (!string.Equals(catalogDto.ContentVersion, manifest != null ? manifest.ContentVersion : null, StringComparison.Ordinal))
            {
                prior.Add(new ContentValidationIssue(
                    null,
                    "contentVersion",
                    "Catalog contentVersion does not match the manifest."));
            }

            var mapped = new List<CardDefinition>();
            var cardsNode = catalogDto.Cards;
            if (cardsNode == null)
            {
                prior.Add(new ContentValidationIssue(null, "cards", "Catalog is missing the cards array."));
            }
            else
            {
                if (catalogDto.CardCount != cardsNode.Count)
                {
                    prior.Add(new ContentValidationIssue(
                        null,
                        "cardCount",
                        "Declared cardCount " + catalogDto.CardCount + " does not match cards array length " + cardsNode.Count + "."));
                }

                for (var i = 0; i < cardsNode.Count; i++)
                {
                    if (TryMapCard(cardsNode[i], i, prior, out var definition))
                    {
                        mapped.Add(definition);
                    }
                }
            }

            var report = CardCatalogValidator.Validate(manifest, actualHash, mapped, prior);
            CardCatalog catalog = null;
            if (report.IsValid)
            {
                catalog = CardCatalog.Create(mapped, manifest.ContentVersion);
            }

            return new CardCatalogLoadResult(catalog, manifest, report);
        }

        private static CardCatalogLoadResult Fail(
            ContentManifest manifest,
            string actualHash,
            List<ContentValidationIssue> prior)
        {
            var report = CardCatalogValidator.Validate(
                manifest,
                actualHash,
                Array.Empty<CardDefinition>(),
                prior);
            return new CardCatalogLoadResult(null, manifest, report);
        }

        private static bool TryMapCard(
            CardDto dto,
            int index,
            List<ContentValidationIssue> errors,
            out CardDefinition definition)
        {
            definition = null;
            if (dto == null)
            {
                errors.Add(new ContentValidationIssue(null, "cards", "Card at index " + index + " is missing."));
                return false;
            }

            var idText = dto.CardId;
            if (dto.Extra != null)
            {
                foreach (var key in dto.Extra.Keys)
                {
                    if (string.Equals(key, "Control ID", StringComparison.Ordinal) ||
                        string.Equals(key, "Control stats", StringComparison.Ordinal))
                    {
                        errors.Add(new ContentValidationIssue(
                            idText,
                            key,
                            "Runtime catalog must not include " + key + "."));
                    }
                }
            }

            if (!CardId.TryParse(dto.CardId, out var cardId))
            {
                errors.Add(new ContentValidationIssue(idText, "cardId", "Invalid cardId."));
                return false;
            }

            if (!CardTypeCatalog.TryFromLabel(dto.Type, out var type))
            {
                errors.Add(new ContentValidationIssue(cardId.Value, "type", "Unknown catalog type '" + dto.Type + "'."));
                return false;
            }

            if (dto.Effects != null && dto.Effects.Count != 0)
            {
                errors.Add(new ContentValidationIssue(
                    cardId.Value,
                    "effects",
                    "effects must be empty in this phase; effectText is stored but not interpreted."));
            }

            IReadOnlyList<string> keywords = dto.Keywords;
            definition = new CardDefinition(
                cardId,
                dto.SetId,
                dto.Number,
                dto.Name,
                type,
                dto.Subtype,
                dto.Race,
                dto.ClassPath,
                dto.CostPa,
                dto.Power,
                dto.Response,
                dto.Rarity,
                dto.Unique,
                dto.Position,
                keywords,
                dto.EffectText,
                dto.Flavor,
                dto.Archetype,
                dto.Canon,
                dto.Complexity,
                dto.BalanceNotes,
                Array.Empty<EffectDefinition>());
            return true;
        }

        private static ContentManifest ToManifest(ManifestDto dto)
        {
            IReadOnlyDictionary<string, int> typeCounts = dto.TypeCounts ?? new Dictionary<string, int>();
            return new ContentManifest(
                dto.SchemaVersion,
                dto.ContentVersion,
                dto.SetId,
                dto.ExpectedCardCount,
                dto.FirstCardId,
                dto.LastCardId,
                dto.IdentityCount,
                dto.HeroCount,
                dto.ClassCount,
                dto.MainDeckCardCount,
                dto.CatalogFile,
                dto.SourceWorkbook,
                dto.Sheet,
                dto.CatalogSha256,
                typeCounts);
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

        private sealed class CatalogFileDto
        {
            [JsonProperty("schemaVersion")]
            public string SchemaVersion { get; set; }

            [JsonProperty("contentVersion")]
            public string ContentVersion { get; set; }

            [JsonProperty("setId")]
            public string SetId { get; set; }

            [JsonProperty("cardCount")]
            public int CardCount { get; set; }

            [JsonProperty("cards")]
            public List<CardDto> Cards { get; set; }
        }

        private sealed class CardDto
        {
            [JsonProperty("cardId")]
            public string CardId { get; set; }

            [JsonProperty("setId")]
            public string SetId { get; set; }

            [JsonProperty("number")]
            public int Number { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("subtype")]
            public string Subtype { get; set; }

            [JsonProperty("race")]
            public string Race { get; set; }

            [JsonProperty("class")]
            public string ClassPath { get; set; }

            [JsonProperty("costPA")]
            public int? CostPa { get; set; }

            [JsonProperty("power")]
            public int? Power { get; set; }

            [JsonProperty("response")]
            public int? Response { get; set; }

            [JsonProperty("rarity")]
            public string Rarity { get; set; }

            [JsonProperty("unique")]
            public bool Unique { get; set; }

            [JsonProperty("position")]
            public string Position { get; set; }

            [JsonProperty("keywords")]
            public List<string> Keywords { get; set; }

            [JsonProperty("effectText")]
            public string EffectText { get; set; }

            [JsonProperty("flavor")]
            public string Flavor { get; set; }

            [JsonProperty("archetype")]
            public string Archetype { get; set; }

            [JsonProperty("canon")]
            public string Canon { get; set; }

            [JsonProperty("complexity")]
            public int? Complexity { get; set; }

            [JsonProperty("balanceNotes")]
            public string BalanceNotes { get; set; }

            [JsonProperty("effects")]
            public List<JToken> Effects { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JToken> Extra { get; set; }
        }

        private sealed class ManifestDto
        {
            [JsonProperty("schemaVersion")]
            public string SchemaVersion { get; set; }

            [JsonProperty("contentVersion")]
            public string ContentVersion { get; set; }

            [JsonProperty("setId")]
            public string SetId { get; set; }

            [JsonProperty("expectedCardCount")]
            public int ExpectedCardCount { get; set; }

            [JsonProperty("firstCardId")]
            public string FirstCardId { get; set; }

            [JsonProperty("lastCardId")]
            public string LastCardId { get; set; }

            [JsonProperty("identityCount")]
            public int IdentityCount { get; set; }

            [JsonProperty("heroCount")]
            public int HeroCount { get; set; }

            [JsonProperty("classCount")]
            public int ClassCount { get; set; }

            [JsonProperty("mainDeckCardCount")]
            public int MainDeckCardCount { get; set; }

            [JsonProperty("catalogFile")]
            public string CatalogFile { get; set; }

            [JsonProperty("sourceWorkbook")]
            public string SourceWorkbook { get; set; }

            [JsonProperty("sheet")]
            public string Sheet { get; set; }

            [JsonProperty("catalogSha256")]
            public string CatalogSha256 { get; set; }

            [JsonProperty("typeCounts")]
            public Dictionary<string, int> TypeCounts { get; set; }
        }
    }
}
