using System;

namespace Warpaz.Domain
{
    /// <summary>
    /// Caller-assigned identity of a <see cref="Command"/>. Used to correlate logs; not a card name.
    /// </summary>
    public readonly struct CommandId : IEquatable<CommandId>
    {
        public CommandId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("CommandId cannot be null or whitespace.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public static CommandId Parse(string value)
        {
            return new CommandId(value);
        }

        public bool Equals(CommandId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CommandId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CommandId left, CommandId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CommandId left, CommandId right)
        {
            return !left.Equals(right);
        }
    }
}
