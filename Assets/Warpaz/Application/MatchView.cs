using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Presentation DTO for one viewer. Debug extras are null unless explicitly requested.
    /// </summary>
    public sealed class MatchView
    {
        public MatchView(
            PlayerId viewerId,
            int turn,
            MatchPhase phase,
            PlayerId activePlayerId,
            PlayerId priorityPlayerId,
            PlayerId firstPlayerId,
            int eventIndex,
            string hash,
            ulong seed,
            bool isDebug,
            PlayerMatchView player0,
            PlayerMatchView player1,
            IReadOnlyList<GameEventView> events,
            DebugMatchView debug)
        {
            ViewerId = viewerId;
            Turn = turn;
            Phase = phase;
            ActivePlayerId = activePlayerId;
            PriorityPlayerId = priorityPlayerId;
            FirstPlayerId = firstPlayerId;
            EventIndex = eventIndex;
            Hash = hash ?? string.Empty;
            Seed = seed;
            IsDebug = isDebug;
            Player0 = player0;
            Player1 = player1;
            Events = events;
            Debug = debug;
        }

        public PlayerId ViewerId { get; }

        public int Turn { get; }

        public MatchPhase Phase { get; }

        public PlayerId ActivePlayerId { get; }

        public PlayerId PriorityPlayerId { get; }

        public PlayerId FirstPlayerId { get; }

        public int EventIndex { get; }

        public string Hash { get; }

        public ulong Seed { get; }

        public bool IsDebug { get; }

        public PlayerMatchView Player0 { get; }

        public PlayerMatchView Player1 { get; }

        public IReadOnlyList<GameEventView> Events { get; }

        public DebugMatchView Debug { get; }

        public PlayerMatchView Viewer => ViewerId.Value == 0 ? Player0 : Player1;

        public PlayerMatchView Opponent => ViewerId.Value == 0 ? Player1 : Player0;
    }
}
