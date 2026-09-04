using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Extra match information. Present only when <see cref="MatchView.IsDebug"/> is true.
    /// </summary>
    public sealed class DebugMatchView
    {
        public DebugMatchView(
            string rngState,
            IReadOnlyList<CardInstanceId> player0DeckOrder,
            IReadOnlyList<CardInstanceId> player1DeckOrder,
            IReadOnlyList<CardView> player0Hand,
            IReadOnlyList<CardView> player1Hand,
            IReadOnlyList<GameEventView> unfilteredEvents)
        {
            RngState = rngState ?? string.Empty;
            Player0DeckOrder = player0DeckOrder;
            Player1DeckOrder = player1DeckOrder;
            Player0Hand = player0Hand;
            Player1Hand = player1Hand;
            UnfilteredEvents = unfilteredEvents;
        }

        public string RngState { get; }

        public IReadOnlyList<CardInstanceId> Player0DeckOrder { get; }

        public IReadOnlyList<CardInstanceId> Player1DeckOrder { get; }

        public IReadOnlyList<CardView> Player0Hand { get; }

        public IReadOnlyList<CardView> Player1Hand { get; }

        public IReadOnlyList<GameEventView> UnfilteredEvents { get; }
    }
}
