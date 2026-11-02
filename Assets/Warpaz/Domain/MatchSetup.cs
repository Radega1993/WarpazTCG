namespace Warpaz.Domain
{
    /// <summary>
    /// Validated host payload for <see cref="CommandType.CreateMatch"/>.
    /// Seats are positional: <see cref="Player0"/> must use <see cref="PlayerId.Player0"/>.
    /// </summary>
    public sealed class MatchSetup
    {
        public MatchSetup(
            ulong seed,
            RuleConfig rules,
            string contentVersion,
            PlayerSetup player0,
            PlayerSetup player1,
            bool preserveMainDeckOrder = false,
            bool forceFirstPlayer0 = false)
        {
            Seed = seed;
            Rules = rules;
            ContentVersion = contentVersion;
            Player0 = player0;
            Player1 = player1;
            PreserveMainDeckOrder = preserveMainDeckOrder;
            ForceFirstPlayer0 = forceFirstPlayer0;
        }

        public ulong Seed { get; }

        public RuleConfig Rules { get; }

        public string ContentVersion { get; }

        public PlayerSetup Player0 { get; }

        public PlayerSetup Player1 { get; }

        /// <summary>
        /// When true, main decks keep declaration order (no Fisher–Yates). For tutorials/scripted matches.
        /// </summary>
        public bool PreserveMainDeckOrder { get; }

        /// <summary>When true, Player0 is always first player (skips RNG seat flip).</summary>
        public bool ForceFirstPlayer0 { get; }
    }
}
