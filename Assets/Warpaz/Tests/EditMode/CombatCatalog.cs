using System;
using System.Collections.Generic;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    /// <summary>Small catalog with known Power values for combat resolution tests.</summary>
    internal sealed class CombatCatalog : ICardCatalog
    {
        public static readonly CardId StrongChar = new CardId("WZ0-100");
        public static readonly CardId MidChar = new CardId("WZ0-105");
        public static readonly CardId WeakChar = new CardId("WZ0-101");
        public static readonly CardId RearChar = new CardId("WZ0-102");

        private readonly CardCatalog _inner;

        private CombatCatalog(CardCatalog inner)
        {
            _inner = inner;
        }

        public static CombatCatalog Create()
        {
            var cards = new List<CardDefinition>();
            cards.Add(Hero(MinimalCatalog.HeroA, 1, "Hero A", 5));
            cards.Add(Hero(MinimalCatalog.HeroB, 2, "Hero B", 5));
            cards.Add(Class(MinimalCatalog.ClassA, 3, "Class A"));
            cards.Add(Class(MinimalCatalog.ClassB, 4, "Class B"));
            cards.Add(Character(StrongChar, 10, "Strong", 1, 10, "Vanguardia"));
            cards.Add(Character(MidChar, 11, "Mid", 1, 5, "Vanguardia"));
            cards.Add(Character(WeakChar, 12, "Weak", 1, 1, "Vanguardia"));
            cards.Add(Character(RearChar, 13, "Rear", 1, 1, "Retaguardia"));

            for (var i = 1; i <= 14; i++)
            {
                cards.Add(Character(
                    new CardId("WZ0-" + (200 + i).ToString("000")),
                    20 + i,
                    "Filler " + i,
                    1,
                    1,
                    "Cualquiera"));
            }

            return new CombatCatalog(CardCatalog.Create(cards, "WZ0-combat"));
        }

        public static IReadOnlyList<CardId> Deck40()
        {
            var deck = new List<CardId>(40);
            AddCopies(deck, StrongChar, 3);
            AddCopies(deck, MidChar, 3);
            AddCopies(deck, WeakChar, 3);
            AddCopies(deck, RearChar, 3);
            for (var i = 1; i <= 14; i++)
            {
                AddCopies(deck, new CardId("WZ0-" + (200 + i).ToString("000")), 2);
            }

            return deck;
        }

        private static void AddCopies(List<CardId> deck, CardId id, int copies)
        {
            for (var i = 0; i < copies; i++)
            {
                deck.Add(id);
            }
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

        private static CardDefinition Hero(CardId id, int number, string name, int power)
        {
            return new CardDefinition(
                id, "WZ0", number, name, CardType.Hero, "Hero", "Test", null,
                null, power, null, "Identity", true, "Hero",
                Array.Empty<string>(), string.Empty, string.Empty, string.Empty, string.Empty, 1, string.Empty,
                Array.Empty<EffectDefinition>());
        }

        private static CardDefinition Class(CardId id, int number, string name)
        {
            return new CardDefinition(
                id, "WZ0", number, name, CardType.ClassPath, "Class", null, "Test",
                null, null, null, "Identity", true, "Class",
                Array.Empty<string>(), string.Empty, string.Empty, string.Empty, string.Empty, 1, string.Empty,
                Array.Empty<EffectDefinition>());
        }

        private static CardDefinition Character(
            CardId id,
            int number,
            string name,
            int cost,
            int power,
            string position)
        {
            return new CardDefinition(
                id, "WZ0", number, name, CardType.Character, "Test", "Test", "Test",
                cost, power, 1, "Common", false, position,
                Array.Empty<string>(), string.Empty, string.Empty, string.Empty, string.Empty, 1, string.Empty,
                Array.Empty<EffectDefinition>());
        }
    }
}
