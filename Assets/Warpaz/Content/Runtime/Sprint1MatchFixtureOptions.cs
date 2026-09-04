using Warpaz.Domain;

namespace Warpaz.Content
{
    /// <summary>
    /// Dev knobs for a Sprint 1 match. Names are display-only; legality uses card ids.
    /// Prompt “Player 1 / Player 2” map to seats <see cref="PlayerId.Player0"/> / <see cref="PlayerId.Player1"/>.
    /// </summary>
    public sealed class Sprint1MatchFixtureOptions
    {
        public const string DefaultPlayer1Name = "Player 1";
        public const string DefaultPlayer2Name = "Player 2";

        public Sprint1MatchFixtureOptions(
            ulong seed = 1UL,
            string player1Name = null,
            string player2Name = null,
            MulliganDecision mulliganDecision = MulliganDecision.KeepHand,
            bool debugVisibility = false,
            RuleConfig rules = null)
        {
            Seed = seed;
            Player1Name = string.IsNullOrEmpty(player1Name) ? DefaultPlayer1Name : player1Name;
            Player2Name = string.IsNullOrEmpty(player2Name) ? DefaultPlayer2Name : player2Name;
            MulliganDecision = mulliganDecision;
            DebugVisibility = debugVisibility;
            Rules = rules ?? RuleConfig.AlphaDefaults();
        }

        public ulong Seed { get; }

        public string Player1Name { get; }

        public string Player2Name { get; }

        /// <summary>
        /// Recorded mulligan choice for the caller. The factory does not submit it.
        /// </summary>
        public MulliganDecision MulliganDecision { get; }

        /// <summary>Opt-in hidden information. Default is off.</summary>
        public bool DebugVisibility { get; }

        public RuleConfig Rules { get; }
    }
}
