using System;
using System.Globalization;

namespace Warpaz.Domain
{
    /// <summary>
    /// One of the four character slots: two Vanguard and two Rearguard.
    /// Index is 0 or 1 within the row.
    /// </summary>
    public readonly struct BoardSlot : IEquatable<BoardSlot>
    {
        public const int SlotsPerRow = 2;
        public const int TotalCharacterSlots = 4;

        public BoardSlot(BoardRow row, int index, CardInstanceId occupant)
        {
            if (index < 0 || index >= SlotsPerRow)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    "BoardSlot index must be 0 or 1 (two slots per row). Received " + index + ".");
            }

            Row = row;
            Index = index;
            Occupant = occupant;
        }

        public BoardRow Row { get; }
        public int Index { get; }
        public CardInstanceId Occupant { get; }

        public bool IsOccupied => !Occupant.IsNone;

        public bool IsVanguard => Row == BoardRow.Vanguard;

        public bool IsRearguard => Row == BoardRow.Rearguard;

        public static BoardSlot Empty(BoardRow row, int index)
        {
            return new BoardSlot(row, index, CardInstanceId.None);
        }

        public static BoardSlot Vanguard(int index, CardInstanceId occupant = default)
        {
            return new BoardSlot(BoardRow.Vanguard, index, occupant);
        }

        public static BoardSlot Rearguard(int index, CardInstanceId occupant = default)
        {
            return new BoardSlot(BoardRow.Rearguard, index, occupant);
        }

        public BoardSlot WithOccupant(CardInstanceId occupant)
        {
            return new BoardSlot(Row, Index, occupant);
        }

        public bool Equals(BoardSlot other)
        {
            return Row == other.Row && Index == other.Index && Occupant == other.Occupant;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardSlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Row;
                hash = (hash * 397) ^ Index;
                hash = (hash * 397) ^ Occupant.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return Row + "[" + Index.ToString(CultureInfo.InvariantCulture) + "]=" + Occupant;
        }

        public static bool operator ==(BoardSlot left, BoardSlot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardSlot left, BoardSlot right)
        {
            return !left.Equals(right);
        }
    }
}
