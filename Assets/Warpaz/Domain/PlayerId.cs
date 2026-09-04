using System;
using System.Globalization;

namespace Warpaz.Domain
{
    /// <summary>
    /// Seat in a two-player match (<c>0</c> or <c>1</c>), or <see cref="Host"/> for system commands.
    /// </summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public static readonly PlayerId Player0 = new PlayerId(0);
        public static readonly PlayerId Player1 = new PlayerId(1);
        public static readonly PlayerId Host = new PlayerId(-1);

        public PlayerId(int value)
        {
            if (value != 0 && value != 1 && value != -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "PlayerId '" + value + "' is invalid. Use 0, 1, or -1 (host).");
            }

            Value = value;
        }

        public int Value { get; }

        public bool IsSeated => Value == 0 || Value == 1;

        public bool IsHost => Value == -1;

        public int Index
        {
            get
            {
                if (!IsSeated)
                {
                    throw new InvalidOperationException(
                        "PlayerId " + this + " is not a seated player and cannot index Players[].");
                }

                return Value;
            }
        }

        public PlayerId Opponent
        {
            get
            {
                if (!IsSeated)
                {
                    throw new InvalidOperationException("Only seated players have an opponent.");
                }

                return Value == 0 ? Player1 : Player0;
            }
        }

        public static PlayerId FromIndex(int index)
        {
            return new PlayerId(index);
        }

        public bool Equals(PlayerId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(PlayerId left, PlayerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerId left, PlayerId right)
        {
            return !left.Equals(right);
        }
    }
}
