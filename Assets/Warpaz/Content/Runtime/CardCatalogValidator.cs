using System;
using System.Collections.Generic;
using System.Globalization;
using Warpaz.Domain;

namespace Warpaz.Content
{
    public static class CardCatalogValidator
    {
        public static readonly IReadOnlyDictionary<CardType, int> ExpectedTypeCounts =
            new Dictionary<CardType, int>
            {
                { CardType.Character, 60 },
                { CardType.Action, 20 },
                { CardType.Equipment, 13 },
                { CardType.Hero, 11 },
                { CardType.Reaction, 10 },
                { CardType.ClassPath, 6 }
            };

        public static ContentValidationReport Validate(
            ContentManifest manifest,
            string actualCatalogSha256,
            IReadOnlyList<CardDefinition> cards,
            IReadOnlyList<ContentValidationIssue> priorErrors = null)
        {
            var errors = new List<ContentValidationIssue>();
            var warnings = new List<ContentValidationIssue>();
            if (priorErrors != null)
            {
                errors.AddRange(priorErrors);
            }

            var contentVersion = manifest != null ? manifest.ContentVersion : null;
            var hash = actualCatalogSha256 ?? string.Empty;
            if (cards == null)
            {
                errors.Add(new ContentValidationIssue(null, "cards", "Catalog is missing."));
                return new ContentValidationReport(
                    contentVersion,
                    hash,
                    0,
                    new Dictionary<string, int>(),
                    0,
                    0,
                    errors,
                    warnings);
            }

            if (manifest == null)
            {
                errors.Add(new ContentValidationIssue(null, "manifest", "Content manifest is missing."));
            }
            else
            {
                if (!string.Equals(manifest.ContentVersion, ContentManifest.ExpectedContentVersion, StringComparison.Ordinal))
                {
                    errors.Add(new ContentValidationIssue(
                        null,
                        "contentVersion",
                        "Expected " + ContentManifest.ExpectedContentVersion + ", found " + manifest.ContentVersion + "."));
                }

                if (!string.Equals(manifest.SetId, ContentManifest.ExpectedSetId, StringComparison.Ordinal))
                {
                    errors.Add(new ContentValidationIssue(
                        null,
                        "setId",
                        "Expected " + ContentManifest.ExpectedSetId + ", found " + manifest.SetId + "."));
                }

                if (!string.Equals(manifest.SchemaVersion, ContentManifest.ExpectedSchemaVersion, StringComparison.Ordinal))
                {
                    errors.Add(new ContentValidationIssue(
                        null,
                        "schemaVersion",
                        "Expected " + ContentManifest.ExpectedSchemaVersion + ", found " + manifest.SchemaVersion + "."));
                }

                if (!string.Equals(manifest.CatalogSha256, actualCatalogSha256, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new ContentValidationIssue(
                        null,
                        "catalogSha256",
                        "Catalog hash mismatch. Expected " + manifest.CatalogSha256 + ", found " + actualCatalogSha256 + "."));
                }
            }

            if (cards.Count != ContentManifest.ExpectedCardCount)
            {
                errors.Add(new ContentValidationIssue(
                    null,
                    "cardCount",
                    "Expected " + ContentManifest.ExpectedCardCount + " cards, found " + cards.Count + "."));
            }

            var seen = new HashSet<CardId>();
            var counts = new Dictionary<CardType, int>();
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                {
                    errors.Add(new ContentValidationIssue(null, "cards", "Entry at index " + i + " is missing."));
                    continue;
                }

                var idText = card.CardId.Value;
                if (!seen.Add(card.CardId))
                {
                    errors.Add(new ContentValidationIssue(idText, "cardId", "Duplicate cardId."));
                }

                var expectedId = "WZ1-" + card.Number.ToString("000", CultureInfo.InvariantCulture);
                if (card.CardId.Value != expectedId)
                {
                    errors.Add(new ContentValidationIssue(
                        idText,
                        "cardId",
                        "ID does not follow number (expected " + expectedId + ")."));
                }

                if (card.Name == card.CardId.Value)
                {
                    errors.Add(new ContentValidationIssue(idText, "name", "cardId must not be the display name."));
                }

                if (card.SetId != ContentManifest.ExpectedSetId)
                {
                    errors.Add(new ContentValidationIssue(idText, "setId", "setId is not WZ1."));
                }

                if (card.Effects != null && card.Effects.Count != 0
                    && !EffectEngine.Supports(card) && !AdvancedEffects.Supports(card)
                    && !ExpandedEffects.Supports(card) && !TacticalEffects.Supports(card))
                {
                    errors.Add(new ContentValidationIssue(
                        idText,
                        "effects",
                        "Unsupported compiled effect program."));
                }

                if (!card.IsIdentity)
                {
                    if (!card.CostPa.HasValue)
                    {
                        errors.Add(new ContentValidationIssue(idText, "costPA", "Non-identity card is missing Coste PA."));
                    }

                    if (!card.Response.HasValue)
                    {
                        errors.Add(new ContentValidationIssue(idText, "response", "Non-identity card is missing Respuesta."));
                    }
                }

                if (card.Type == CardType.Character && !card.Power.HasValue)
                {
                    errors.Add(new ContentValidationIssue(idText, "power", "Personaje is missing Poder."));
                }

                counts.TryGetValue(card.Type, out var n);
                counts[card.Type] = n + 1;
            }

            var identities = Count(counts, CardType.Hero) + Count(counts, CardType.ClassPath);
            var deck = cards.Count - identities;

            if (cards.Count > 0)
            {
                for (var n = 1; n <= ContentManifest.ExpectedCardCount; n++)
                {
                    var id = new CardId("WZ1-" + n.ToString("000", CultureInfo.InvariantCulture));
                    if (!seen.Contains(id))
                    {
                        errors.Add(new ContentValidationIssue(id.Value, "cardId", "Missing correlative ID."));
                    }
                }

                foreach (var pair in ExpectedTypeCounts)
                {
                    counts.TryGetValue(pair.Key, out var found);
                    if (found != pair.Value)
                    {
                        var label = CardTypeCatalog.ToLabel(pair.Key);
                        errors.Add(new ContentValidationIssue(
                            null,
                            "type",
                            "Type " + label + ": expected " + pair.Value + ", found " + found + "."));
                    }
                }

                if (identities != ContentManifest.ExpectedIdentityCount)
                {
                    errors.Add(new ContentValidationIssue(
                        null,
                        "identityCount",
                        "Identities: expected " + ContentManifest.ExpectedIdentityCount + ", found " + identities + "."));
                }

                if (deck != ContentManifest.ExpectedMainDeckCount)
                {
                    errors.Add(new ContentValidationIssue(
                        null,
                        "mainDeckCardCount",
                        "Main-deck cards: expected " + ContentManifest.ExpectedMainDeckCount + ", found " + deck + "."));
                }
            }

            var labels = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in counts)
            {
                labels[CardTypeCatalog.ToLabel(pair.Key)] = pair.Value;
            }

            return new ContentValidationReport(
                contentVersion,
                hash,
                cards.Count,
                labels,
                identities,
                deck,
                errors,
                warnings);
        }

        private static int Count(Dictionary<CardType, int> counts, CardType type)
        {
            counts.TryGetValue(type, out var n);
            return n;
        }
    }
}
