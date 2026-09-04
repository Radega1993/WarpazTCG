namespace Warpaz.Domain
{
    /// <summary>
    /// Immutable fact produced by an accepted command. Indexes form a dense log from 0.
    /// </summary>
    public sealed class GameEvent
    {
        public const int NoParent = -1;

        public GameEvent(
            int eventIndex,
            GameEventType type,
            PlayerId actorId,
            string payload,
            CardInstanceId sourceInstanceId = default,
            int parentEventIndex = NoParent,
            GameEventVisibility visibility = GameEventVisibility.Public)
        {
            EventIndex = eventIndex;
            Type = type;
            ActorId = actorId;
            Payload = payload ?? string.Empty;
            SourceInstanceId = sourceInstanceId;
            ParentEventIndex = parentEventIndex;
            Visibility = visibility;
        }

        public int EventIndex { get; }

        public GameEventType Type { get; }

        public PlayerId ActorId { get; }

        public string Payload { get; }

        public CardInstanceId SourceInstanceId { get; }

        public int ParentEventIndex { get; }

        public GameEventVisibility Visibility { get; }

        public bool IsVisibleTo(PlayerId viewer)
        {
            switch (Visibility)
            {
                case GameEventVisibility.Public:
                    return true;
                case GameEventVisibility.OwnerOnly:
                    return viewer == ActorId;
                default:
                    return false;
            }
        }
    }
}
