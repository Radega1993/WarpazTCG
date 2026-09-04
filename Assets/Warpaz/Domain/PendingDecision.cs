namespace Warpaz.Domain
{
    /// <summary>
    /// Choice the engine is waiting on. <see cref="None"/> means the match is not blocked on input.
    /// Concrete targeting is not implemented yet.
    /// </summary>
    public sealed class PendingDecision
    {
        public static readonly PendingDecision None = new PendingDecision(
            PendingDecisionKind.None,
            PlayerId.Host);

        public PendingDecision(PendingDecisionKind kind, PlayerId actorId)
        {
            Kind = kind;
            ActorId = actorId;
        }

        public PendingDecisionKind Kind { get; }
        public PlayerId ActorId { get; }
        public bool IsPending => Kind != PendingDecisionKind.None;
    }
}
