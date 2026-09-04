using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Submits a command through the session. Domain decides accept or reject.
    /// </summary>
    public sealed class SubmitCommandUseCase
    {
        public CommandResult Execute(GameSession session, Command command)
        {
            if (session == null)
            {
                return CommandResult.Reject(null, CommandRejectionCode.InvalidPayload, "Session is missing.");
            }

            return session.ApplyCommand(command);
        }
    }
}
