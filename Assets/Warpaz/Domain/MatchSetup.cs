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
            PlayerSetup player1)
        {
            Seed = seed;
            Rules = rules;
            ContentVersion = contentVersion;
            Player0 = player0;
            Player1 = player1;
        }

        public ulong Seed { get; }

        public RuleConfig Rules { get; }

        public string ContentVersion { get; }

        public PlayerSetup Player0 { get; }

        public PlayerSetup Player1 { get; }
    }
}
