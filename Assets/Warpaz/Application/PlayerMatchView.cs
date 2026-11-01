using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Per-player slice of a match view. Opponent hands expose count only.
    /// Board slots are always visible (public information).
    /// </summary>
    public sealed class PlayerMatchView
    {
        public PlayerMatchView(
            PlayerId playerId,
            CardId heroId,
            CardId classPathId,
            int resolution,
            int paCurrent,
            int paMax,
            int deckCount,
            int handCount,
            IReadOnlyList<CardView> visibleHand,
            IReadOnlyList<CardView> discard,
            IReadOnlyList<BoardSlotView> boardSlots)
        {
            PlayerId = playerId;
            HeroId = heroId;
            ClassPathId = classPathId;
            Resolution = resolution;
            PaCurrent = paCurrent;
            PaMax = paMax;
            DeckCount = deckCount;
            HandCount = handCount;
            VisibleHand = visibleHand;
            Discard = discard;
            BoardSlots = boardSlots;
        }

        public PlayerId PlayerId { get; }

        public CardId HeroId { get; }

        public CardId ClassPathId { get; }

        public int Resolution { get; }

        public int PaCurrent { get; }

        public int PaMax { get; }

        public int DeckCount { get; }

        public int HandCount { get; }

        public IReadOnlyList<CardView> VisibleHand { get; }

        public IReadOnlyList<CardView> Discard { get; }

        public IReadOnlyList<BoardSlotView> BoardSlots { get; }
    }
}
