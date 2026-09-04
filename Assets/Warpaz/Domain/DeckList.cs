using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Declared deck plus identities. <see cref="DeckId"/> and <see cref="DisplayName"/> are
    /// request metadata and must never be used as keys or stored on <see cref="GameState"/>.
    /// The main-deck length is validated by <see cref="DeckValidator"/>, not this constructor.
    /// </summary>
    public sealed class DeckList
    {
        public DeckList(
            string deckId,
            string displayName,
            CardId heroCardId,
            CardId classCardId,
            IReadOnlyList<CardId> mainDeckCardIds)
        {
            if (mainDeckCardIds == null)
            {
                throw new ArgumentNullException(nameof(mainDeckCardIds));
            }

            DeckId = deckId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            HeroCardId = heroCardId;
            ClassCardId = classCardId;

            var copy = new CardId[mainDeckCardIds.Count];
            for (var i = 0; i < mainDeckCardIds.Count; i++)
            {
                copy[i] = mainDeckCardIds[i];
            }

            MainDeckCardIds = Array.AsReadOnly(copy);
        }

        public DeckList(CardId heroCardId, CardId classCardId, IReadOnlyList<CardId> mainDeckCardIds)
            : this(string.Empty, string.Empty, heroCardId, classCardId, mainDeckCardIds)
        {
        }

        public string DeckId { get; }

        public string DisplayName { get; }

        public CardId HeroCardId { get; }

        public CardId ClassCardId { get; }

        public IReadOnlyList<CardId> MainDeckCardIds { get; }
    }
}
