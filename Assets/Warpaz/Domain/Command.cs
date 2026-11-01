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
            MulliganDecision decision,
            CardInstanceId primaryInstanceId,
            CardInstanceId secondaryInstanceId,
            BoardRow targetRow,
            int targetSlotIndex,
            bool hasBoardSlotTarget,
            bool targetsHero,
            ReorganizeMode reorganizeMode,
            bool hasReorganizeMode)
        {
            CommandId = commandId;
            ActorId = actorId;
            Type = type;
            ExpectedEventIndex = expectedEventIndex;
            MatchSetup = matchSetup;
            Decision = decision;
            PrimaryInstanceId = primaryInstanceId;
            SecondaryInstanceId = secondaryInstanceId;
            TargetRow = targetRow;
            TargetSlotIndex = targetSlotIndex;
            HasBoardSlotTarget = hasBoardSlotTarget;
            TargetsHero = targetsHero;
            ReorganizeMode = reorganizeMode;
            HasReorganizeMode = hasReorganizeMode;
        }

        public CommandId CommandId { get; }

        public PlayerId ActorId { get; }

        public CommandType Type { get; }

        public int ExpectedEventIndex { get; }

        public MatchSetup MatchSetup { get; }

        public MulliganDecision Decision { get; }

        public bool KeepHand => Decision == MulliganDecision.KeepHand;

        public CardInstanceId PrimaryInstanceId { get; }

        public CardInstanceId SecondaryInstanceId { get; }

        public BoardRow TargetRow { get; }

        public int TargetSlotIndex { get; }

        public bool HasBoardSlotTarget { get; }

        public bool TargetsHero { get; }

        public ReorganizeMode ReorganizeMode { get; }

        public bool HasReorganizeMode { get; }

        public static Command CreateMatch(CommandId commandId, MatchSetup setup)
        {
            return new Command(
                commandId,
                HostActorId,
                CommandType.CreateMatch,
                0,
                setup,
                MulliganDecision.KeepHand,
                CardInstanceId.None,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
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
                decision,
                CardInstanceId.None,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
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
            return new Command(
                commandId,
                actorId,
                CommandType.EndTurn,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                CardInstanceId.None,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static Command EndTurn(string commandId, PlayerId actorId, int expectedEventIndex)
        {
            return EndTurn(CommandId.Parse(commandId), actorId, expectedEventIndex);
        }

        public static Command EndTurn(string commandId, int actorId, int expectedEventIndex)
        {
            return EndTurn(CommandId.Parse(commandId), PlayerId.FromIndex(actorId), expectedEventIndex);
        }

        public static Command Play(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId instanceId,
            BoardRow targetRow,
            int targetSlotIndex)
        {
            return new Command(
                commandId,
                actorId,
                CommandType.Play,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                instanceId,
                CardInstanceId.None,
                targetRow,
                targetSlotIndex,
                true,
                false,
                default,
                false);
        }

        public static Command Play(
            string commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId instanceId,
            BoardRow targetRow,
            int targetSlotIndex)
        {
            return Play(CommandId.Parse(commandId), actorId, expectedEventIndex, instanceId, targetRow, targetSlotIndex);
        }

        public static Command PlayReplace(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId instanceId,
            CardInstanceId replaceInstanceId)
        {
            return new Command(
                commandId,
                actorId,
                CommandType.Play,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                instanceId,
                replaceInstanceId,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static Command PlayReplace(
            string commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId instanceId,
            CardInstanceId replaceInstanceId)
        {
            return PlayReplace(
                CommandId.Parse(commandId),
                actorId,
                expectedEventIndex,
                instanceId,
                replaceInstanceId);
        }

        public static Command Attack(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId attackerId,
            CardInstanceId targetCharacterId)
        {
            return new Command(
                commandId,
                actorId,
                CommandType.Attack,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                attackerId,
                targetCharacterId,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static Command Attack(
            string commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId attackerId,
            CardInstanceId targetCharacterId)
        {
            return Attack(
                CommandId.Parse(commandId),
                actorId,
                expectedEventIndex,
                attackerId,
                targetCharacterId);
        }

        public static Command AttackHero(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId attackerId)
        {
            return new Command(
                commandId,
                actorId,
                CommandType.Attack,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                attackerId,
                CardInstanceId.None,
                default,
                0,
                false,
                true,
                default,
                false);
        }

        public static Command AttackHero(
            string commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId attackerId)
        {
            return AttackHero(CommandId.Parse(commandId), actorId, expectedEventIndex, attackerId);
        }

        public static Command ReorganizeMove(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId instanceId,
            BoardRow targetRow,
            int targetSlotIndex)
        {
            return new Command(
                commandId,
                actorId,
                CommandType.Reorganize,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                instanceId,
                CardInstanceId.None,
                targetRow,
                targetSlotIndex,
                true,
                false,
                ReorganizeMode.MoveToEmptyOtherRow,
                true);
        }

        public static Command ReorganizeMove(
            string commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId instanceId,
            BoardRow targetRow,
            int targetSlotIndex)
        {
            return ReorganizeMove(
                CommandId.Parse(commandId),
                actorId,
                expectedEventIndex,
                instanceId,
                targetRow,
                targetSlotIndex);
        }

        public static Command ReorganizeSwap(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId firstInstanceId,
            CardInstanceId secondInstanceId)
        {
            return new Command(
                commandId,
                actorId,
                CommandType.Reorganize,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                firstInstanceId,
                secondInstanceId,
                default,
                0,
                false,
                false,
                ReorganizeMode.SwapRows,
                true);
        }

        public static Command ReorganizeSwap(
            string commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId firstInstanceId,
            CardInstanceId secondInstanceId)
        {
            return ReorganizeSwap(
                CommandId.Parse(commandId),
                actorId,
                expectedEventIndex,
                firstInstanceId,
                secondInstanceId);
        }

        public static Command ChooseInterceptor(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId interceptorId)
        {
            return new Command(
                commandId,
                actorId,
                CommandType.ChooseTargets,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                interceptorId,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static Command ChooseInterceptor(
            string commandId,
            PlayerId actorId,
            int expectedEventIndex,
            CardInstanceId interceptorId)
        {
            return ChooseInterceptor(
                CommandId.Parse(commandId),
                actorId,
                expectedEventIndex,
                interceptorId);
        }

        public static Command DeclineInterception(
            CommandId commandId,
            PlayerId actorId,
            int expectedEventIndex)
        {
            return new Command(
                commandId,
                actorId,
                CommandType.PassPriority,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                CardInstanceId.None,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static Command DeclineInterception(
            string commandId,
            PlayerId actorId,
            int expectedEventIndex)
        {
            return DeclineInterception(CommandId.Parse(commandId), actorId, expectedEventIndex);
        }

        public static Command Unsupported(
            CommandId commandId,
            PlayerId actorId,
            CommandType type,
            int expectedEventIndex)
        {
            return new Command(
                commandId,
                actorId,
                type,
                expectedEventIndex,
                null,
                MulliganDecision.KeepHand,
                CardInstanceId.None,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
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
