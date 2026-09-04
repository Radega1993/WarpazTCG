namespace Warpaz.Domain
{
    /// <summary>
    /// Intent submitted to the engine. Rejected commands must not mutate <see cref="GameState"/>.
    /// Mulligan payload is <see cref="Decision"/>; <see cref="KeepHand"/> is derived.
    /// </summary>
    public sealed class Command
    {
        public static readonly PlayerId HostActorId = PlayerId.Host;

        private Command(
            CommandId commandId,
            PlayerId actorId,
            CommandType type,
            int expectedEventIndex,
            MatchSetup matchSetup,
            MulliganDecision decision)
        {
            CommandId = commandId;
            ActorId = actorId;
            Type = type;
            ExpectedEventIndex = expectedEventIndex;
            MatchSetup = matchSetup;
            Decision = decision;
        }

        public CommandId CommandId { get; }

        public PlayerId ActorId { get; }

        public CommandType Type { get; }

        public int ExpectedEventIndex { get; }

        public MatchSetup MatchSetup { get; }

        public MulliganDecision Decision { get; }

        public bool KeepHand => Decision == MulliganDecision.KeepHand;

        public static Command CreateMatch(CommandId commandId, MatchSetup setup)
        {
            return new Command(commandId, HostActorId, CommandType.CreateMatch, 0, setup, MulliganDecision.KeepHand);
        }

        public static Command CreateMatch(string commandId, MatchSetup setup)
        {
            return CreateMatch(CommandId.Parse(commandId), setup);
        }

        public static Command ConfirmMulligan(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex,
            MulliganDecision decision)
        {
            return new Command(
                commandId,
                actorId,
                CommandType.ConfirmMulligan,
                expectedEventIndex,
                null,
                decision);
        }

        public static Command ConfirmMulligan(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex,
            bool keep)
        {
            return ConfirmMulligan(
                commandId,
                actorId,
                expectedEventIndex,
                keep ? MulliganDecision.KeepHand : MulliganDecision.ReplaceWholeHand);
        }

        public static Command ConfirmMulligan(string commandId, int actorId, int expectedEventIndex, bool keep)
        {
            return ConfirmMulligan(CommandId.Parse(commandId), PlayerId.FromIndex(actorId), expectedEventIndex, keep);
        }

        public static Command ConfirmMulligan(
            string commandId,
            int actorId,
            int expectedEventIndex,
            MulliganDecision decision)
        {
            return ConfirmMulligan(CommandId.Parse(commandId), PlayerId.FromIndex(actorId), expectedEventIndex, decision);
        }

        public static Command EndTurn(CommandId commandId, PlayerId actorId, int expectedEventIndex)
        {
            return new Command(commandId, actorId, CommandType.EndTurn, expectedEventIndex, null, MulliganDecision.KeepHand);
        }

        public static Command EndTurn(string commandId, PlayerId actorId, int expectedEventIndex)
        {
            return EndTurn(CommandId.Parse(commandId), actorId, expectedEventIndex);
        }

        public static Command EndTurn(string commandId, int actorId, int expectedEventIndex)
        {
            return EndTurn(CommandId.Parse(commandId), PlayerId.FromIndex(actorId), expectedEventIndex);
        }

        public static Command Unsupported(
            CommandId commandId,
            PlayerId actorId,
            CommandType type,
            int expectedEventIndex)
        {
            return new Command(commandId, actorId, type, expectedEventIndex, null, MulliganDecision.KeepHand);
        }

        public static Command Unsupported(string commandId, PlayerId actorId, CommandType type, int expectedEventIndex)
        {
            return Unsupported(CommandId.Parse(commandId), actorId, type, expectedEventIndex);
        }

        public static Command Unsupported(string commandId, int actorId, CommandType type, int expectedEventIndex)
        {
            return Unsupported(CommandId.Parse(commandId), PlayerId.FromIndex(actorId), type, expectedEventIndex);
        }
    }
}
