using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>Filtered or debug copy of a <see cref="GameEvent"/>.</summary>
    public sealed class GameEventView
    {
        public GameEventView(
            int eventIndex,
            GameEventType type,
            PlayerId actorId,
            string payload,
            GameEventVisibility visibility,
            CardInstanceId sourceInstanceId)
        {
            EventIndex = eventIndex;
            Type = type;
            ActorId = actorId;
            Payload = payload ?? string.Empty;
            Visibility = visibility;
            SourceInstanceId = sourceInstanceId;
        }

        public int EventIndex { get; }

        public GameEventType Type { get; }

        public PlayerId ActorId { get; }

        public string Payload { get; }

        public GameEventVisibility Visibility { get; }

        public CardInstanceId SourceInstanceId { get; }

        public static GameEventView From(GameEvent evt)
        {
            return new GameEventView(
                evt.EventIndex,
                evt.Type,
                evt.ActorId,
                evt.Payload,
                evt.Visibility,
                evt.SourceInstanceId);
        }
    }
}
