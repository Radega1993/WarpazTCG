namespace Warpaz.Domain
{
    /// <summary>
    /// Shared command gate: stale index, actor, phase, duplicates, and dispatch.
    /// Rejected commands never mutate <see cref="GameState"/>, RNG, or the event log.
    /// </summary>
    public sealed class CommandProcessor
    {
        private readonly ICommandHandler[] _handlers;

        public CommandProcessor(ICardCatalog catalog)
        {
            _handlers = new ICommandHandler[]
            {
                new CreateMatchHandler(catalog),
                new ConfirmMulliganHandler(),
                new EndTurnHandler(),
                new PlayCharacterHandler(catalog),
                new ReorganizeHandler(),
                new AttackHandler(catalog),
                new ChooseInterceptorHandler(catalog),
                new DeclineInterceptionHandler(catalog)
            };
        }

        public CommandResult Submit(GameState current, Command command)
        {
            if (command == null)
            {
                return CommandResult.Reject(
                    current,
                    CommandRejectionCode.InvalidPayload,
                    "Command is missing.");
            }

            if (command.Type == CommandType.CreateMatch)
            {
                return Finalize(command, SubmitCreateMatch(current, command));
            }

            if (current == null)
            {
                return CommandResult.Reject(
                    null,
                    CommandRejectionCode.InvalidPhase,
                    "No match in progress.");
            }

            if (current.HasAcceptedCommand(command.CommandId))
            {
                return CommandResult.Reject(
                    current,
                    CommandRejectionCode.DuplicateCommand,
                    "Duplicate commandId.");
            }

            if (current.Result.IsFinished || current.Phase == MatchPhase.GameOver)
            {
                return CommandResult.Reject(
                    current,
                    CommandRejectionCode.MatchAlreadyFinished,
                    "Match already finished.");
            }

            if (command.ExpectedEventIndex != current.EventIndex)
            {
                return CommandResult.Reject(
                    current,
                    CommandRejectionCode.StaleState,
                    "expectedEventIndex mismatch.");
            }

            if (!command.ActorId.IsSeated)
            {
                return CommandResult.Reject(
                    current,
                    CommandRejectionCode.UnknownPlayer,
                    "Unknown player.");
            }

            var handler = FindHandler(command.Type);
            if (handler == null)
            {
                return CommandResult.Reject(
                    current,
                    CommandRejectionCode.CommandNotSupported,
                    "Command type " + command.Type + " is not available.");
            }

            return Finalize(command, handler.Handle(current, command));
        }

        private CommandResult SubmitCreateMatch(GameState current, Command command)
        {
            if (current != null)
            {
                return CommandResult.Reject(
                    current,
                    CommandRejectionCode.InvalidPhase,
                    "Match already started.");
            }

            if (command.ActorId != Command.HostActorId)
            {
                return CommandResult.Reject(
                    null,
                    CommandRejectionCode.WrongActor,
                    "CreateMatch must be issued by the host.");
            }

            if (command.ExpectedEventIndex != 0)
            {
                return CommandResult.Reject(
                    null,
                    CommandRejectionCode.StaleState,
                    "expectedEventIndex mismatch.");
            }

            var handler = FindHandler(CommandType.CreateMatch);
            return handler.Handle(current, command);
        }

        private ICommandHandler FindHandler(CommandType type)
        {
            for (var i = 0; i < _handlers.Length; i++)
            {
                if (_handlers[i].Type == type)
                {
                    return _handlers[i];
                }
            }

            return null;
        }

        private static CommandResult Finalize(Command command, CommandResult result)
        {
            if (result.Accepted && result.State != null)
            {
                result.State.RecordAcceptedCommand(command.CommandId);
            }

            return result;
        }
    }
}
