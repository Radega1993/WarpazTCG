using System;
using System.Globalization;

namespace Warpaz.Domain
{
    /// <summary>
    /// Unique per-match identity of a <see cref="CardInstance"/>. Zero is reserved for “none”.
    /// </summary>
    public readonly struct CardInstanceId : IEquatable<CardInstanceId>
    {
        public static CardInstanceId None => default;

        public CardInstanceId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "CardInstanceId must be a positive integer. Zero is reserved for none.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool IsNone => Value == 0;

        public bool Equals(CardInstanceId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CardInstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(CardInstanceId left, CardInstanceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CardInstanceId left, CardInstanceId right)
        {
            return !left.Equals(right);
        }
    }
}
