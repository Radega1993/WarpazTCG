using System;
using System.Collections.Generic;
using System.IO;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    /// <summary>
    /// Legal WZ1 decks keyed only by <see cref="CardId"/> and <see cref="CardType"/>.
    /// Never uses card names as keys.
    /// </summary>
    internal static class Wz1MatchFixtures
    {
        private static CardCatalog _catalog;

        public static CardCatalog Catalog
        {
            get
            {
                if (_catalog == null)
                {
                    _catalog = LoadOfficial();
                }

                return _catalog;
            }
        }

        public static MatchSetup Standard(ulong seed, RuleConfig rules = null)
        {
            var resolved = rules ?? RuleConfig.AlphaDefaults();
            return new MatchSetup(
                seed,
                resolved,
                Catalog.ContentVersion,
                Seat(PlayerId.Player0),
                Seat(PlayerId.Player1));
        }

        public static PlayerSetup Seat(PlayerId playerId)
        {
            var heroIndex = playerId.Index;
            var classIndex = playerId.Index;
            return new PlayerSetup(
                playerId,
                "Player " + playerId,
                new DeckList(
                    "wz1-" + playerId,
                    "WZ1 seat " + playerId,
                    HeroId(heroIndex),
                    ClassId(classIndex),
                    MainDeck40()));
        }

        public static CardId HeroId(int index)
        {
            return Catalog.Filter(type: CardType.Hero)[index].CardId;
        }

        public static CardId ClassId(int index)
        {
            return Catalog.Filter(type: CardType.ClassPath)[index].CardId;
        }

        public static IReadOnlyList<CardId> MainDeckPool()
        {
            var pool = new List<CardId>();
            var all = Catalog.All;
            for (var i = 0; i < all.Count; i++)
            {
                if (!all[i].IsIdentity)
                {
                    pool.Add(all[i].CardId);
                }
            }

            return pool;
        }

        public static IReadOnlyList<CardId> MainDeck40()
        {
            return MainDeckOfSize(40);
        }

        public static IReadOnlyList<CardId> MainDeckOfSize(int size)
        {
            var pool = MainDeckPool();
            var deck = new List<CardId>(size);
            var copies = 0;
            var index = 0;
            for (var i = 0; i < size; i++)
            {
                deck.Add(pool[index]);
                copies++;
                if (copies == 3)
                {
                    copies = 0;
                    index++;
                }
            }

            return deck;
        }

        public static IReadOnlyList<CardId> DeckWithFourthCopy()
        {
            var deck = new List<CardId>(MainDeck40());
            deck[deck.Count - 1] = deck[0];
            return deck;
        }

        public static IReadOnlyList<CardId> DeckWithUnknownId()
        {
            var deck = new List<CardId>(MainDeck40());
            deck[0] = new CardId("WZ1-999");
            return deck;
        }

        public static IReadOnlyList<CardId> DeckWithIdentity()
        {
            var deck = new List<CardId>(MainDeck40());
            deck[0] = HeroId(0);
            return deck;
        }

        public static PlayerSetup Player(
            PlayerId playerId,
            CardId heroId,
            CardId classCardId,
            IReadOnlyList<CardId> mainDeck)
        {
            return new PlayerSetup(playerId, heroId, classCardId, mainDeck);
        }

        private static CardCatalog LoadOfficial()
        {
            var directory = Path.Combine(UnityEngine.Application.dataPath, "StreamingAssets", "Data");
            var result = CardCatalogLoader.LoadFromDirectory(directory);
            if (!result.Success)
            {
                throw new InvalidOperationException("WZ1 catalog failed to load for fixtures.");
            }

            return result.Catalog;
        }
    }
}
