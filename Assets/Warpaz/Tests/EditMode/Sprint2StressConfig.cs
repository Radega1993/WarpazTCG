namespace Warpaz.Tests.EditMode
{
    public sealed class Sprint2StressConfig
    {
        public const int DefaultMatchCount = 50;
        public const ulong DefaultBaseSeed = 9000UL;
        public const int DefaultMaxTurns = 150;

        public Sprint2StressConfig(
            int matchCount = DefaultMatchCount,
            ulong baseSeed = DefaultBaseSeed,
            int maxTurns = DefaultMaxTurns,
            bool abortOnFirstFailure = true)
        {
            MatchCount = matchCount;
            BaseSeed = baseSeed;
            MaxTurns = maxTurns;
            MaxCommandsPerMatch = 2000;
            AbortOnFirstFailure = abortOnFirstFailure;
        }

        public int MatchCount { get; }

        public ulong BaseSeed { get; }

        public int MaxTurns { get; }

        public int MaxCommandsPerMatch { get; }

        public bool AbortOnFirstFailure { get; }
    }
}
