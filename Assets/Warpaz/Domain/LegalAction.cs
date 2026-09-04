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
            string reason)
        {
            ActorId = actorId;
            Type = type;
            HasMulliganDecision = hasMulliganDecision;
            MulliganDecision = mulliganDecision;
            Reason = reason ?? string.Empty;
        }

        public PlayerId ActorId { get; }

        public CommandType Type { get; }

        public bool HasMulliganDecision { get; }

        public MulliganDecision MulliganDecision { get; }

        public string Reason { get; }

        public static LegalAction ConfirmMulligan(PlayerId actorId, MulliganDecision decision, string reason)
        {
            return new LegalAction(actorId, CommandType.ConfirmMulligan, true, decision, reason);
        }

        public static LegalAction EndTurn(PlayerId actorId, string reason)
        {
            return new LegalAction(actorId, CommandType.EndTurn, false, MulliganDecision.KeepHand, reason);
        }
    }
}
