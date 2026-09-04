using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Seated player entering a match. <see cref="DisplayName"/> is request metadata and
    /// must never be used as a key or stored on <see cref="GameState"/>.
    /// </summary>
    public sealed class PlayerSetup
    {
        public PlayerSetup(PlayerId playerId, string displayName, DeckList deckList)
        {
            if (deckList == null)
            {
                throw new ArgumentNullException(nameof(deckList));
            }

            PlayerId = playerId;
            DisplayName = displayName ?? string.Empty;
            DeckList = deckList;
        }

        public PlayerSetup(PlayerId playerId, DeckList deckList)
            : this(playerId, string.Empty, deckList)
        {
        }

        public PlayerSetup(
            PlayerId playerId,
            CardId heroCardId,
            CardId classCardId,
            IReadOnlyList<CardId> mainDeckCardIds)
            : this(playerId, string.Empty, new DeckList(heroCardId, classCardId, mainDeckCardIds))
        {
        }

        public PlayerId PlayerId { get; }

        public string DisplayName { get; }

        public DeckList DeckList { get; }

        public CardId HeroCardId => DeckList.HeroCardId;

        public CardId ClassCardId => DeckList.ClassCardId;

        public IReadOnlyList<CardId> MainDeckCardIds => DeckList.MainDeckCardIds;
    }
}
