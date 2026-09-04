using System.Collections.Generic;

namespace Warpaz.Domain
{
    public sealed class DeckValidationResult
    {
        public DeckValidationResult(bool isValid, string reason)
        {
            IsValid = isValid;
            Reason = reason ?? string.Empty;
        }

        public bool IsValid { get; }

        public string Reason { get; }

        public static DeckValidationResult Ok()
        {
            return new DeckValidationResult(true, string.Empty);
        }

        public static DeckValidationResult Fail(string reason)
        {
            return new DeckValidationResult(false, reason);
        }
    }

    public static class DeckValidator
    {
        public static DeckValidationResult Validate(
            PlayerSetup setup,
            ICardCatalog catalog,
            RuleConfig rules)
        {
            if (setup == null || setup.DeckList == null)
            {
                return DeckValidationResult.Fail("Player setup is missing.");
            }

            return Validate(setup.DeckList, catalog, rules);
        }

        public static DeckValidationResult Validate(
            DeckList deckList,
            ICardCatalog catalog,
            RuleConfig rules)
        {
            if (deckList == null)
            {
                return DeckValidationResult.Fail("Player setup is missing.");
            }

            if (catalog == null)
            {
                return DeckValidationResult.Fail("Catalog is missing.");
            }

            if (rules == null)
            {
                return DeckValidationResult.Fail("RuleConfig is missing.");
            }

            if (!TryRequireType(catalog, deckList.HeroCardId, CardType.Hero, out var heroReason))
            {
                return DeckValidationResult.Fail(heroReason);
            }

            if (!TryRequireType(catalog, deckList.ClassCardId, CardType.ClassPath, out var classReason))
            {
                return DeckValidationResult.Fail(classReason);
            }

            if (deckList.MainDeckCardIds == null)
            {
                return DeckValidationResult.Fail("Deck is missing.");
            }

            if (deckList.MainDeckCardIds.Count != rules.DeckSize)
            {
                return DeckValidationResult.Fail(
                    "Deck must contain exactly " + rules.DeckSize + " cards.");
            }

            var copies = new Dictionary<CardId, int>();
            foreach (var cardId in deckList.MainDeckCardIds)
            {
                if (!catalog.TryGet(cardId, out var definition))
                {
                    return DeckValidationResult.Fail("Unknown card id " + cardId + ".");
                }

                if (definition.IsIdentity)
                {
                    return DeckValidationResult.Fail("Identity " + cardId + " cannot be in the main deck.");
                }

                copies.TryGetValue(cardId, out var count);
                count++;
                if (count > rules.MaxCopiesPerCard)
                {
                    return DeckValidationResult.Fail(
                        "Card " + cardId + " exceeds the " + rules.MaxCopiesPerCard + "-copy limit.");
                }

                copies[cardId] = count;
            }

            return DeckValidationResult.Ok();
        }

        private static bool TryRequireType(
            ICardCatalog catalog,
            CardId cardId,
            CardType expectedType,
            out string reason)
        {
            if (!catalog.TryGet(cardId, out var definition))
            {
                reason = "Identity " + cardId + " is missing.";
                return false;
            }

            if (definition.Type != expectedType)
            {
                reason = "Card " + cardId + " is not a " + expectedType + ".";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
