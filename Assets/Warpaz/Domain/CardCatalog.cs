using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Immutable in-memory catalog. Indexed only by <see cref="CardId"/>.
    /// Does not create match state or interpret effects.
    /// </summary>
    public sealed class CardCatalog : ICardCatalog
    {
        private readonly Dictionary<CardId, CardDefinition> _byId;
        private readonly IReadOnlyList<CardDefinition> _all;

        private CardCatalog(
            Dictionary<CardId, CardDefinition> byId,
            CardDefinition[] all,
            string contentVersion)
        {
            _byId = byId;
            _all = Array.AsReadOnly(all);
            ContentVersion = contentVersion;
        }

        public static CardCatalog Create(IReadOnlyList<CardDefinition> definitions, string contentVersion)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(contentVersion))
            {
                throw new ArgumentException("contentVersion is required.", nameof(contentVersion));
            }

            var byId = new Dictionary<CardId, CardDefinition>(definitions.Count);
            var all = new CardDefinition[definitions.Count];
            for (var i = 0; i < definitions.Count; i++)
            {
                var source = definitions[i];
                if (source == null)
                {
                    throw new ArgumentException("Catalog entry is missing.", nameof(definitions));
                }

                var sealedCard = Seal(source);
                if (byId.ContainsKey(sealedCard.CardId))
                {
                    throw new InvalidOperationException("Duplicate cardId " + sealedCard.CardId + ".");
                }

                byId.Add(sealedCard.CardId, sealedCard);
                all[i] = sealedCard;
            }

            return new CardCatalog(byId, all, contentVersion);
        }

        public string ContentVersion { get; }

        public int Count => _all.Count;

        public IReadOnlyList<CardDefinition> All => _all;

        public bool TryGet(CardId cardId, out CardDefinition definition)
        {
            return _byId.TryGetValue(cardId, out definition);
        }

        public IReadOnlyList<CardDefinition> Filter(
            CardType? type = null,
            string race = null,
            string classPath = null,
            int? costPa = null,
            string rarity = null,
            string keyword = null)
        {
            var matches = new List<CardDefinition>();
            for (var i = 0; i < _all.Count; i++)
            {
                var card = _all[i];
                if (type.HasValue && card.Type != type.Value)
                {
                    continue;
                }

                if (race != null && !string.Equals(card.Race, race, StringComparison.Ordinal))
                {
                    continue;
                }

                if (classPath != null && !string.Equals(card.ClassPath, classPath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (costPa.HasValue && card.CostPa != costPa)
                {
                    continue;
                }

                if (rarity != null && !string.Equals(card.Rarity, rarity, StringComparison.Ordinal))
                {
                    continue;
                }

                if (keyword != null && !HasKeyword(card, keyword))
                {
                    continue;
                }

                matches.Add(card);
            }

            return matches.AsReadOnly();
        }

        private static bool HasKeyword(CardDefinition card, string keyword)
        {
            var keywords = card.Keywords;
            for (var i = 0; i < keywords.Count; i++)
            {
                if (string.Equals(keywords[i], keyword, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static CardDefinition Seal(CardDefinition card)
        {
            return new CardDefinition(
                card.CardId,
                card.SetId,
                card.Number,
                card.Name,
                card.Type,
                card.Subtype,
                card.Race,
                card.ClassPath,
                card.CostPa,
                card.Power,
                card.Response,
                card.Rarity,
                card.Unique,
                card.Position,
                CopyStrings(card.Keywords),
                card.EffectText,
                card.Flavor,
                card.Archetype,
                card.Canon,
                card.Complexity,
                card.BalanceNotes,
                CopyEffects(card.Effects));
        }

        private static IReadOnlyList<string> CopyStrings(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<string>());
            }

            var copy = new string[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<EffectDefinition> CopyEffects(IReadOnlyList<EffectDefinition> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<EffectDefinition>());
            }

            var copy = new EffectDefinition[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return Array.AsReadOnly(copy);
        }
    }
}
