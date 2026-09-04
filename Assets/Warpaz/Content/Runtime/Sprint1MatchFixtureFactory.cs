using System;
using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Content
{
    /// <summary>
    /// Builds legal Sprint 1 seats from a live catalog using <see cref="CardId"/> and
    /// <see cref="CardType"/> only. Does not copy <see cref="CardDefinition"/> or persist decks.
    /// </summary>
    public static class Sprint1MatchFixtureFactory
    {
        public static Sprint1MatchFixture Create(ICardCatalog catalog, Sprint1MatchFixtureOptions options = null)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var resolved = options ?? new Sprint1MatchFixtureOptions();
            var deck = BuildMainDeck(catalog, resolved.Rules.DeckSize, resolved.Rules.MaxCopiesPerCard);
            var player0 = Seat(
                catalog,
                PlayerId.Player0,
                resolved.Player1Name,
                0,
                0,
                deck);
            var player1 = Seat(
                catalog,
                PlayerId.Player1,
                resolved.Player2Name,
                1,
                1,
                deck);
            return new Sprint1MatchFixture(catalog, resolved, player0, player1);
        }

        public static CardId IdentityAt(ICardCatalog catalog, CardType type, int index)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var matches = catalog.Filter(type: type);
            if (index < 0 || index >= matches.Count)
            {
                throw new InvalidOperationException(
                    "Catalog has no " + type + " at index " + index + ".");
            }

            return matches[index].CardId;
        }

        public static IReadOnlyList<CardId> BuildMainDeck(ICardCatalog catalog, RuleConfig rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            return BuildMainDeck(catalog, rules.DeckSize, rules.MaxCopiesPerCard);
        }

        public static IReadOnlyList<CardId> BuildMainDeck(ICardCatalog catalog, int deckSize, int maxCopies)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (deckSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deckSize), "Deck size must be positive.");
            }

            if (maxCopies <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCopies), "Max copies must be positive.");
            }

            var pool = MainDeckPool(catalog);
            var neededUnique = (deckSize + maxCopies - 1) / maxCopies;
            if (pool.Count < neededUnique)
            {
                throw new InvalidOperationException(
                    "Catalog has " + pool.Count + " main-deck cards; need at least " + neededUnique + ".");
            }

            var deck = new List<CardId>(deckSize);
            var copies = 0;
            var index = 0;
            for (var i = 0; i < deckSize; i++)
            {
                deck.Add(pool[index]);
                copies++;
                if (copies == maxCopies)
                {
                    copies = 0;
                    index++;
                }
            }

            return deck;
        }

        private static PlayerSetup Seat(
            ICardCatalog catalog,
            PlayerId playerId,
            string displayName,
            int heroIndex,
            int classIndex,
            IReadOnlyList<CardId> mainDeck)
        {
            var heroId = IdentityAt(catalog, CardType.Hero, heroIndex);
            var classId = IdentityAt(catalog, CardType.ClassPath, classIndex);
            return new PlayerSetup(
                playerId,
                displayName,
                new DeckList(
                    "sprint1-" + playerId,
                    displayName,
                    heroId,
                    classId,
                    mainDeck));
        }

        private static List<CardId> MainDeckPool(ICardCatalog catalog)
        {
            var pool = new List<CardId>();
            var all = catalog.All;
            for (var i = 0; i < all.Count; i++)
            {
                if (!all[i].IsIdentity)
                {
                    pool.Add(all[i].CardId);
                }
            }

            return pool;
        }
    }
}
