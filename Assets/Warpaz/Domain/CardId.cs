using System;

namespace Warpaz.Domain
{
    /// <summary>
    /// Stable catalog identity. Names are display-only and must never be used as keys.
    /// Valid values match <c>^WZ[0-9]+-[0-9]{3}$</c> (for example <c>WZ1-001</c>).
    /// </summary>
    public readonly struct CardId : IEquatable<CardId>
    {
        public const string Pattern = "^WZ[0-9]+-[0-9]{3}$";

        public CardId(string value)
        {
            if (!IsValid(value))
            {
                throw new ArgumentException(
                    "CardId '" + (value ?? "<null>") + "' is invalid. Expected pattern " + Pattern + ".",
                    nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public static bool IsValid(string value)
        {
            if (value == null || value.Length < 6)
            {
                return false;
            }

            if (value[0] != 'W' || value[1] != 'Z')
            {
                return false;
            }

            var index = 2;
            if (index >= value.Length || !IsDigit(value[index]))
            {
                return false;
            }

            while (index < value.Length && IsDigit(value[index]))
            {
                index++;
            }

            if (index >= value.Length || value[index] != '-')
            {
                return false;
            }

            index++;
            return index + 3 == value.Length
                && IsDigit(value[index])
                && IsDigit(value[index + 1])
                && IsDigit(value[index + 2]);
        }

        public static bool TryParse(string value, out CardId cardId)
        {
            if (!IsValid(value))
            {
                cardId = default;
                return false;
            }

            cardId = new CardId(value);
            return true;
        }

        public bool Equals(CardId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CardId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CardId left, CardId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CardId left, CardId right)
        {
            return !left.Equals(right);
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }
    }
}
