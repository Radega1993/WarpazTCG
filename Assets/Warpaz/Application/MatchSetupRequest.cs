using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Inbound match request. <see cref="RuleVersion"/> is resolved against a loaded
    /// <see cref="RuleConfig"/> before the engine sees a <see cref="MatchSetup"/>.
    /// Player seats are assigned from <see cref="PlayerSetup.PlayerId"/>, not field order.
    /// </summary>
    public sealed class MatchSetupRequest
    {
        public MatchSetupRequest(
            PlayerSetup player1,
            PlayerSetup player2,
            ulong seed,
            string ruleVersion,
            string contentVersion)
        {
            Player1 = player1;
            Player2 = player2;
            Seed = seed;
            RuleVersion = ruleVersion ?? string.Empty;
            ContentVersion = contentVersion ?? string.Empty;
        }

        public PlayerSetup Player1 { get; }

        public PlayerSetup Player2 { get; }

        public ulong Seed { get; }

        public string RuleVersion { get; }

        public string ContentVersion { get; }
    }
}
