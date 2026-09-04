using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Warpaz.Domain;

namespace Warpaz.Content
{
    public sealed class RuleConfigLoadResult
    {
        public RuleConfigLoadResult(
            RuleConfig config,
            string schemaVersion,
            string ruleVersion,
            IReadOnlyList<ContentValidationIssue> errors)
        {
            Config = config;
            SchemaVersion = schemaVersion;
            RuleVersion = ruleVersion;
            Errors = errors ?? Array.Empty<ContentValidationIssue>();
        }

        public RuleConfig Config { get; }
        public string SchemaVersion { get; }
        public string RuleVersion { get; }
        public IReadOnlyList<ContentValidationIssue> Errors { get; }
        public bool Success => Config != null && Errors.Count == 0;
    }

    public static class RuleConfigLoader
    {
        public const string FileName = "rule-config.v0.1.json";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore
        };

        public static RuleConfigLoadResult LoadFromDirectory(string dataDirectory)
        {
            var errors = new List<ContentValidationIssue>();
            if (string.IsNullOrEmpty(dataDirectory))
            {
                errors.Add(new ContentValidationIssue(null, "dataDirectory", "Content data directory is missing."));
                return new RuleConfigLoadResult(null, null, null, errors);
            }

            var path = Path.Combine(dataDirectory, FileName);
            if (!File.Exists(path))
            {
                errors.Add(new ContentValidationIssue(null, "ruleConfig", "Missing RuleConfig file: " + path + "."));
                return new RuleConfigLoadResult(null, null, null, errors);
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
            if (json.Length > 0 && json[0] == '\ufeff')
            {
                json = json.Substring(1);
            }

            RuleConfigDto dto;
            try
            {
                dto = JsonConvert.DeserializeObject<RuleConfigDto>(json, SerializerSettings);
            }
            catch (JsonException ex)
            {
                errors.Add(new ContentValidationIssue(null, "ruleConfig", "RuleConfig JSON is invalid: " + ex.Message));
                return new RuleConfigLoadResult(null, null, null, errors);
            }

            if (dto == null)
            {
                errors.Add(new ContentValidationIssue(null, "ruleConfig", "RuleConfig JSON is empty."));
                return new RuleConfigLoadResult(null, null, null, errors);
            }

            if (!string.Equals(dto.SchemaVersion, RuleConfig.SchemaVersion, StringComparison.Ordinal))
            {
                errors.Add(new ContentValidationIssue(
                    null,
                    "schemaVersion",
                    "Expected " + RuleConfig.SchemaVersion + ", found " + dto.SchemaVersion + "."));
            }

            if (!string.Equals(dto.RuleVersion, RuleConfig.AlphaRuleVersion, StringComparison.Ordinal))
            {
                errors.Add(new ContentValidationIssue(
                    null,
                    "ruleVersion",
                    "Expected " + RuleConfig.AlphaRuleVersion + ", found " + dto.RuleVersion + "."));
            }

            var expected = RuleConfig.AlphaDefaults();
            Compare(errors, "initialHandSize", dto.InitialHandSize, expected.InitialHandSize);
            Compare(errors, "initialResolution", dto.InitialResolution, expected.InitialResolution);
            Compare(errors, "deckSize", dto.DeckSize, expected.DeckSize);
            Compare(errors, "maxCopiesPerCard", dto.MaxCopiesPerCard, expected.MaxCopiesPerCard);
            Compare(errors, "initialPaMax", dto.InitialPaMax, expected.InitialPaMax);
            Compare(errors, "paIncreasePerTurn", dto.PaIncreasePerTurn, expected.PaIncreasePerTurn);
            Compare(errors, "paLimit", dto.PaLimit, expected.PaLimit);
            Compare(errors, "ascensionRequiredPaMax", dto.AscensionRequiredPaMax, expected.AscensionRequiredPaMax);
            Compare(errors, "ascensionCost", dto.AscensionCost, expected.AscensionCost);
            Compare(errors, "maximumEquipmentPerCharacter", dto.MaximumEquipmentPerCharacter, expected.MaximumEquipmentPerCharacter);
            Compare(errors, "freeReorganizationsPerTurn", dto.FreeReorganizationsPerTurn, expected.FreeReorganizationsPerTurn);
            Compare(errors, "failedDrawResolutionLoss", dto.FailedDrawResolutionLoss, expected.FailedDrawResolutionLoss);
            Compare(errors, "maximumEventsPerTurn", dto.MaximumEventsPerTurn, expected.MaximumEventsPerTurn);
            Compare(errors, "maximumDecisionsPerTurn", dto.MaximumDecisionsPerTurn, expected.MaximumDecisionsPerTurn);
            if (dto.HandLimit != expected.HandLimit)
            {
                errors.Add(new ContentValidationIssue(
                    null,
                    "handLimit",
                    "Expected " + FormatNullable(expected.HandLimit) + ", found " + FormatNullable(dto.HandLimit) + "."));
            }

            if (errors.Count > 0)
            {
                return new RuleConfigLoadResult(null, dto.SchemaVersion, dto.RuleVersion, errors);
            }

            var config = new RuleConfig(
                dto.RuleVersion,
                dto.InitialHandSize,
                dto.InitialResolution,
                dto.DeckSize,
                dto.MaxCopiesPerCard,
                dto.InitialPaMax,
                dto.PaIncreasePerTurn,
                dto.PaLimit,
                dto.AscensionRequiredPaMax,
                dto.AscensionCost,
                dto.MaximumEquipmentPerCharacter,
                dto.FreeReorganizationsPerTurn,
                dto.HandLimit,
                dto.FailedDrawResolutionLoss,
                dto.MaximumEventsPerTurn,
                dto.MaximumDecisionsPerTurn);
            return new RuleConfigLoadResult(config, dto.SchemaVersion, dto.RuleVersion, errors);
        }

        private static void Compare(List<ContentValidationIssue> errors, string field, int actual, int expected)
        {
            if (actual != expected)
            {
                errors.Add(new ContentValidationIssue(
                    null,
                    field,
                    "Expected " + expected + ", found " + actual + "."));
            }
        }

        private static string FormatNullable(int? value)
        {
            return value.HasValue ? value.Value.ToString() : "null";
        }

        private sealed class RuleConfigDto
        {
            [JsonProperty("schemaVersion")]
            public string SchemaVersion { get; set; }

            [JsonProperty("ruleVersion")]
            public string RuleVersion { get; set; }

            [JsonProperty("initialHandSize")]
            public int InitialHandSize { get; set; }

            [JsonProperty("initialResolution")]
            public int InitialResolution { get; set; }

            [JsonProperty("deckSize")]
            public int DeckSize { get; set; }

            [JsonProperty("maxCopiesPerCard")]
            public int MaxCopiesPerCard { get; set; }

            [JsonProperty("initialPaMax")]
            public int InitialPaMax { get; set; }

            [JsonProperty("paIncreasePerTurn")]
            public int PaIncreasePerTurn { get; set; }

            [JsonProperty("paLimit")]
            public int PaLimit { get; set; }

            [JsonProperty("ascensionRequiredPaMax")]
            public int AscensionRequiredPaMax { get; set; }

            [JsonProperty("ascensionCost")]
            public int AscensionCost { get; set; }

            [JsonProperty("maximumEquipmentPerCharacter")]
            public int MaximumEquipmentPerCharacter { get; set; }

            [JsonProperty("freeReorganizationsPerTurn")]
            public int FreeReorganizationsPerTurn { get; set; }

            [JsonProperty("handLimit")]
            public int? HandLimit { get; set; }

            [JsonProperty("failedDrawResolutionLoss")]
            public int FailedDrawResolutionLoss { get; set; }

            [JsonProperty("maximumEventsPerTurn")]
            public int MaximumEventsPerTurn { get; set; }

            [JsonProperty("maximumDecisionsPerTurn")]
            public int MaximumDecisionsPerTurn { get; set; }
        }
    }
}
