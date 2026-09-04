using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    /// <summary>
    /// Configurable knobs for the Sprint-1 stress runner (test infrastructure only).
    /// </summary>
    public sealed class Sprint1StressConfig
    {
        public const int DefaultMatchCount = 100;
        public const ulong DefaultBaseSeed = 1000UL;
        public const int DefaultMaxTurns = 20;

        public Sprint1StressConfig(
            int matchCount = DefaultMatchCount,
            ulong baseSeed = DefaultBaseSeed,
            int maxTurns = DefaultMaxTurns,
            bool abortOnFirstFailure = true)
        {
            MatchCount = matchCount;
            BaseSeed = baseSeed;
            MaxTurns = maxTurns;
            AbortOnFirstFailure = abortOnFirstFailure;
            MaxCommandsPerMatch = 1 + 2 + maxTurns + 2;
            MaxEventsPerMatch = RuleConfig.DefaultMaximumEventsPerTurn * (maxTurns + 2);
            MaxPhaseTransitionsPerMatch = RuleConfig.DefaultMaximumEventsPerTurn * (maxTurns + 2);
        }

        public int MatchCount { get; }
        public ulong BaseSeed { get; }
        public int MaxTurns { get; }
        public bool AbortOnFirstFailure { get; }
        public int MaxCommandsPerMatch { get; }
        public int MaxEventsPerMatch { get; }
        public int MaxPhaseTransitionsPerMatch { get; }

        /// <summary>Deterministic mulligan: KeepHand for even seeds, ReplaceWholeHand for odd.</summary>
        public static MulliganDecision MulliganForSeed(ulong seed)
        {
            return (seed & 1UL) == 0UL
                ? MulliganDecision.KeepHand
                : MulliganDecision.ReplaceWholeHand;
        }
    }
}
