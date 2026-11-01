using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>One board slot in a match projection. Occupant may be null if empty.</summary>
    public sealed class BoardSlotView
    {
        public BoardSlotView(
            BoardRow row,
            int index,
            CardView occupant,
            ReadyState readyState,
            bool hasRecent)
        {
            Row = row;
            Index = index;
            Occupant = occupant;
            ReadyState = readyState;
            HasRecent = hasRecent;
        }

        public BoardRow Row { get; }

        public int Index { get; }

        public CardView Occupant { get; }

        public bool IsOccupied => Occupant != null;

        public ReadyState ReadyState { get; }

        public bool HasRecent { get; }
    }
}
