using System;
using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Creates a match through the session. Seat mapping and version checks live here;
    /// deck legality and first-player choice stay in Domain.
    /// </summary>
    public sealed class CreateMatchUseCase
    {
        public CommandResult Execute(GameSession session, MatchSetup setup)
        {
            if (session == null)
            {
                return CommandResult.Reject(null, CommandRejectionCode.InvalidPayload, "Session is missing.");
            }

            return session.ApplyCommand(Command.CreateMatch(session.AllocateCommandId(), setup));
        }

        public CommandResult Execute(GameSession session, MatchSetupRequest request, RuleConfig rules)
        {
            if (session == null)
            {
                return CommandResult.Reject(null, CommandRejectionCode.InvalidPayload, "Session is missing.");
            }

            if (!TryPrepareSetup(session.Catalog, request, rules, out var setup, out var reason))
            {
                var rejected = CommandResult.Reject(session.State, CommandRejectionCode.InvalidPayload, reason);
                session.RememberResult(rejected);
                return rejected;
            }

            return Execute(session, setup);
        }

        internal static bool TryPrepareSetup(
            ICardCatalog catalog,
            MatchSetupRequest request,
            RuleConfig rules,
            out MatchSetup setup,
            out string reason)
        {
            setup = null;
            if (request == null)
            {
                reason = "Match setup is missing.";
                return false;
            }

            if (rules == null)
            {
                reason = "RuleConfig is missing.";
                return false;
            }

            if (catalog == null)
            {
                reason = "Catalog is missing.";
                return false;
            }

            if (!string.Equals(request.RuleVersion, rules.RuleVersion, StringComparison.Ordinal))
            {
                reason = "Incompatible rule version.";
                return false;
            }

            if (!string.Equals(request.ContentVersion, catalog.ContentVersion, StringComparison.Ordinal))
            {
                reason = "Incompatible content version.";
                return false;
            }

            if (request.Player1 == null || request.Player2 == null)
            {
                reason = "Player setup is missing.";
                return false;
            }

            if (!request.Player1.PlayerId.IsSeated || !request.Player2.PlayerId.IsSeated)
            {
                reason = "PlayerId must be a seated player.";
                return false;
            }

            if (request.Player1.PlayerId == request.Player2.PlayerId)
            {
                reason = "Duplicate PlayerId.";
                return false;
            }

            var player0 = request.Player1.PlayerId == PlayerId.Player0 ? request.Player1 : request.Player2;
            var player1 = request.Player1.PlayerId == PlayerId.Player1 ? request.Player1 : request.Player2;
            setup = new MatchSetup(request.Seed, rules, request.ContentVersion, player0, player1);
            reason = string.Empty;
            return true;
        }
    }
}
