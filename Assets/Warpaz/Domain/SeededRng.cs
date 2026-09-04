using System;

namespace Warpaz.Domain
{
    /// <summary>
    /// SplitMix64. Identical seeds and restored snapshots produce identical sequences
    /// on every runtime. Does not use <see cref="Random"/> or UnityEngine.Random.
    /// </summary>
    public sealed class SeededRng : IDeterministicRandom
    {
        private ulong _seed;
        private ulong _state;

        public SeededRng(ulong seed)
        {
            _seed = seed;
            _state = seed;
        }

        public SeededRng(RandomSnapshot snapshot)
        {
            Restore(snapshot);
        }

        public ulong Seed => _seed;

        public ulong State => _state;

        public RandomSnapshot Save()
        {
            return new RandomSnapshot(_seed, _state);
        }

        public void Restore(RandomSnapshot snapshot)
        {
            _seed = snapshot.Seed;
            _state = snapshot.State;
        }

        public ulong NextUInt64()
        {
            _state += 0x9E3779B97F4A7C15UL;
            var z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentException("Range must be non-empty.", nameof(maxExclusive));
            }

            var range = (ulong)(maxExclusive - minInclusive);
            var threshold = ulong.MaxValue - (ulong.MaxValue % range);
            ulong sample;
            do
            {
                sample = NextUInt64();
            }
            while (sample >= threshold);

            return minInclusive + (int)(sample % range);
        }
    }
}
