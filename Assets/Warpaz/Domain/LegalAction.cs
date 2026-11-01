namespace Warpaz.Domain
{
    /// <summary>
    /// A command the engine would currently accept from this actor.
    /// Presentation must not invent extra legal actions.
    /// </summary>
    public sealed class LegalAction
    {
        private LegalAction(
            PlayerId actorId,
            CommandType type,
            bool hasMulliganDecision,
            MulliganDecision mulliganDecision,
            string reason,
            CardInstanceId primaryInstanceId,
            CardInstanceId secondaryInstanceId,
            BoardRow targetRow,
            int targetSlotIndex,
            bool hasBoardSlotTarget,
            bool targetsHero,
            ReorganizeMode reorganizeMode,
            bool hasReorganizeMode)
        {
            ActorId = actorId;
            Type = type;
            HasMulliganDecision = hasMulliganDecision;
            MulliganDecision = mulliganDecision;
            Reason = reason ?? string.Empty;
            PrimaryInstanceId = primaryInstanceId;
            SecondaryInstanceId = secondaryInstanceId;
            TargetRow = targetRow;
            TargetSlotIndex = targetSlotIndex;
            HasBoardSlotTarget = hasBoardSlotTarget;
            TargetsHero = targetsHero;
            ReorganizeMode = reorganizeMode;
            HasReorganizeMode = hasReorganizeMode;
        }

        public PlayerId ActorId { get; }

        public CommandType Type { get; }

        public bool HasMulliganDecision { get; }

        public MulliganDecision MulliganDecision { get; }

        public string Reason { get; }

        public CardInstanceId PrimaryInstanceId { get; }

        public CardInstanceId SecondaryInstanceId { get; }

        public BoardRow TargetRow { get; }

        public int TargetSlotIndex { get; }

        public bool HasBoardSlotTarget { get; }

        public bool TargetsHero { get; }

        public ReorganizeMode ReorganizeMode { get; }

        public bool HasReorganizeMode { get; }

        public static LegalAction ConfirmMulligan(PlayerId actorId, MulliganDecision decision, string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.ConfirmMulligan,
                true,
                decision,
                reason,
                CardInstanceId.None,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static LegalAction EndTurn(PlayerId actorId, string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.EndTurn,
                false,
                MulliganDecision.KeepHand,
                reason,
                CardInstanceId.None,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static LegalAction Play(
            PlayerId actorId,
            CardInstanceId instanceId,
            BoardRow targetRow,
            int targetSlotIndex,
            string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.Play,
                false,
                MulliganDecision.KeepHand,
                reason,
                instanceId,
                CardInstanceId.None,
                targetRow,
                targetSlotIndex,
                true,
                false,
                default,
                false);
        }

        public static LegalAction PlayReplace(
            PlayerId actorId,
            CardInstanceId instanceId,
            CardInstanceId replaceInstanceId,
            string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.Play,
                false,
                MulliganDecision.KeepHand,
                reason,
                instanceId,
                replaceInstanceId,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static LegalAction ReorganizeMove(
            PlayerId actorId,
            CardInstanceId instanceId,
            BoardRow targetRow,
            int targetSlotIndex,
            string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.Reorganize,
                false,
                MulliganDecision.KeepHand,
                reason,
                instanceId,
                CardInstanceId.None,
                targetRow,
                targetSlotIndex,
                true,
                false,
                ReorganizeMode.MoveToEmptyOtherRow,
                true);
        }

        public static LegalAction ReorganizeSwap(
            PlayerId actorId,
            CardInstanceId firstInstanceId,
            CardInstanceId secondInstanceId,
            string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.Reorganize,
                false,
                MulliganDecision.KeepHand,
                reason,
                firstInstanceId,
                secondInstanceId,
                default,
                0,
                false,
                false,
                ReorganizeMode.SwapRows,
                true);
        }

        public static LegalAction Attack(
            PlayerId actorId,
            CardInstanceId attackerId,
            CardInstanceId targetCharacterId,
            string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.Attack,
                false,
                MulliganDecision.KeepHand,
                reason,
                attackerId,
                targetCharacterId,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static LegalAction AttackHero(PlayerId actorId, CardInstanceId attackerId, string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.Attack,
                false,
                MulliganDecision.KeepHand,
                reason,
                attackerId,
                CardInstanceId.None,
                default,
                0,
                false,
                true,
                default,
                false);
        }

        public static LegalAction ChooseInterceptor(
            PlayerId actorId,
            CardInstanceId interceptorId,
            string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.ChooseTargets,
                false,
                MulliganDecision.KeepHand,
                reason,
                interceptorId,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
        }

        public static LegalAction DeclineInterception(PlayerId actorId, string reason)
        {
            return new LegalAction(
                actorId,
                CommandType.PassPriority,
                false,
                MulliganDecision.KeepHand,
                reason,
                CardInstanceId.None,
                CardInstanceId.None,
                default,
                0,
                false,
                false,
                default,
                false);
        }
    }
}
