namespace Warpaz.Domain
{
    /// <summary>
    /// Stack item awaiting resolution. Operations and card effects are not modeled yet.
    /// </summary>
    public sealed class PendingEffect
    {
        public PendingEffect(CardInstanceId sourceInstanceId, int sequence)
        {
            SourceInstanceId = sourceInstanceId;
            Sequence = sequence;
        }

        public CardInstanceId SourceInstanceId { get; }
        public int Sequence { get; }
    }
}
