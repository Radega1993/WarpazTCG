using System;

namespace Warpaz.Domain
{
    /// <summary>
    /// Captured SplitMix64 seed and cursor. Enough to resume the same stream.
    /// </summary>
    public readonly struct RandomSnapshot : IEquatable<RandomSnapshot>
    {
        public RandomSnapshot(ulong seed, ulong state)
        {
            Seed = seed;
            State = state;
        }

        public ulong Seed { get; }
        public ulong State { get; }

        public bool Equals(RandomSnapshot other)
        {
            return Seed == other.Seed && State == other.State;
        }

        public override bool Equals(object obj)
        {
            return obj is RandomSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Seed.GetHashCode() * 397) ^ State.GetHashCode();
            }
        }

        public static bool operator ==(RandomSnapshot left, RandomSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RandomSnapshot left, RandomSnapshot right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Recorded, replayable randomness. Implementations must not call
    /// <see cref="System.Random"/> or UnityEngine.Random.
    /// </summary>
    public interface IDeterministicRandom
    {
        /// <summary>Seed that started this stream.</summary>
        ulong Seed { get; }

        /// <summary>Internal cursor. Combined with <see cref="Seed"/> it reconstructs the stream.</summary>
        ulong State { get; }

        RandomSnapshot Save();

        void Restore(RandomSnapshot snapshot);

        ulong NextUInt64();

        /// <summary>Unbiased integer in <c>[minInclusive, maxExclusive)</c>.</summary>
        int Next(int minInclusive, int maxExclusive);
    }
}
