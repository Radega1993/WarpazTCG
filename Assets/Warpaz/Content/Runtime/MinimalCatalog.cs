using System;
using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Content
{
    public sealed class MinimalCatalog : ICardCatalog
    {
        public const string SetId = "WZ0";
        public const string ContentVersionValue = "WZ0-test";
        public static readonly CardId HeroA = new CardId("WZ0-001");
        public static readonly CardId HeroB = new CardId("WZ0-002");
        public static readonly CardId ClassA = new CardId("WZ0-003");
        public static readonly CardId ClassB = new CardId("WZ0-004");

        private readonly CardCatalog _inner;

        private MinimalCatalog(CardCatalog inner)
        {
            _inner = inner;
        }

        public static MinimalCatalog Create()
        {
            var cards = new List<CardDefinition>();
            cards.Add(Hero(HeroA, 1, "Hero A"));
            cards.Add(Hero(HeroB, 2, "Hero B"));
            cards.Add(Class(ClassA, 3, "Class A"));
            cards.Add(Class(ClassB, 4, "Class B"));

            for (var i = 1; i <= 20; i++)
            {
                var id = VanillaId(i);
                cards.Add(new CardDefinition(
                    id,
                    SetId,
                    i + 4,
                    "Vanilla " + i.ToString("00"),
                    CardType.Character,
                    "Test",
                    "Test",
                    "Test",
                    1,
                    1,
                    1,
                    "Common",
                    false,
                    "Cualquiera",
                    Array.Empty<string>(),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    1,
                    string.Empty,
                    Array.Empty<EffectDefinition>()));
            }

            return new MinimalCatalog(CardCatalog.Create(cards, ContentVersionValue));
        }

        public static CardId VanillaId(int number)
        {
            return new CardId("WZ0-" + (10 + number).ToString("000"));
        }

        public static IReadOnlyList<CardId> StandardDeck40()
        {
            var deck = new List<CardId>(40);
            for (var i = 1; i <= 10; i++)
            {
                var id = VanillaId(i);
                deck.Add(id);
                deck.Add(id);
                deck.Add(id);
            }

            for (var i = 11; i <= 15; i++)
            {
                var id = VanillaId(i);
                deck.Add(id);
                deck.Add(id);
            }

            return deck;
        }

        public string ContentVersion => _inner.ContentVersion;

        public int Count => _inner.Count;

        public IReadOnlyList<CardDefinition> All => _inner.All;

        public bool TryGet(CardId cardId, out CardDefinition definition)
        {
            return _inner.TryGet(cardId, out definition);
        }

        public IReadOnlyList<CardDefinition> Filter(
            CardType? type = null,
            string race = null,
            string classPath = null,
            int? costPa = null,
            string rarity = null,
            string keyword = null)
        {
            return _inner.Filter(type, race, classPath, costPa, rarity, keyword);
        }

        private static CardDefinition Hero(CardId id, int number, string name)
        {
            return new CardDefinition(
                id, SetId, number, name, CardType.Hero, "Hero", "Test", null,
                null, 5, null, "Identity", true, "Hero",
                Array.Empty<string>(), string.Empty, string.Empty, string.Empty, string.Empty, 1, string.Empty,
                Array.Empty<EffectDefinition>());
        }

        private static CardDefinition Class(CardId id, int number, string name)
        {
            return new CardDefinition(
                id, SetId, number, name, CardType.ClassPath, "Class", null, "Test",
                null, null, null, "Identity", true, "Class",
                Array.Empty<string>(), string.Empty, string.Empty, string.Empty, string.Empty, 1, string.Empty,
                Array.Empty<EffectDefinition>());
        }
    }
}
