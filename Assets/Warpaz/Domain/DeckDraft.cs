using System;
using System.Collections.Generic;
using System.Linq;

namespace Warpaz.Domain
{
    /// <summary>Editable, incomplete deck. Match admission remains the validator's responsibility.</summary>
    public sealed class DeckDraft
    {
        private readonly List<CardId> _cards;
        public string Id { get; }
        public string Name { get; private set; }
        public CardId Hero { get; private set; }
        public CardId Class { get; private set; }
        public IReadOnlyList<CardId> Cards => _cards.AsReadOnly();
        public DeckDraft(DeckList deck)
        {
            if (deck == null || string.IsNullOrWhiteSpace(deck.DeckId)) throw new ArgumentException("Missing deck id.");
            Id = deck.DeckId; Rename(deck.DisplayName); Hero = deck.HeroCardId; Class = deck.ClassCardId;
            _cards = new List<CardId>(deck.MainDeckCardIds);
        }
        public void Rename(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length < 1 || name.Length > 80) throw new ArgumentException("Nombre: entre 1 y 80 caracteres.");
            Name = name;
        }
        public int Count(CardId id) => _cards.Count(c => c == id);
        public bool Add(CardId id, ICardCatalog catalog, RuleConfig rules)
        {
            if (!catalog.TryGet(id, out var card) || card.IsIdentity || _cards.Count >= rules.DeckSize || Count(id) >= rules.MaxCopiesPerCard) return false;
            _cards.Add(id); return true;
        }
        public bool Remove(CardId id) => _cards.Remove(id);
        public void SetIdentity(CardId id, ICardCatalog catalog)
        {
            if (!catalog.TryGet(id, out var card) || !card.IsIdentity) throw new ArgumentException("Identidad no válida.");
            if (card.Type == CardType.Hero) Hero = id; else Class = id;
        }
        public DeckList ToDeck() => new DeckList(Id, Name, Hero, Class, _cards);
        public DeckDraft Duplicate() => new DeckDraft(new DeckList(Guid.NewGuid().ToString("N"),
            Name.Substring(0, Math.Min(Name.Length, 73)) + " copia", Hero, Class, _cards));
    }
}
