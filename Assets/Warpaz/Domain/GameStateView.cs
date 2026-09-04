using System.Collections.Generic;

namespace Warpaz.Domain
{
    public sealed class PlayerView
    {
        public PlayerView(
            PlayerId playerId,
            CardId heroId,
            CardId classPathId,
            int resolution,
            int paCurrent,
            int paMax,
            int deckCount,
            int handCount,
            IReadOnlyList<CardInstance> visibleHand,
            IReadOnlyList<CardInstance> discard)
        {
            PlayerId = playerId;
            HeroId = heroId;
            ClassPathId = classPathId;
            Resolution = resolution;
            PaCurrent = paCurrent;
            PaMax = paMax;
            DeckCount = deckCount;
            HandCount = handCount;
            VisibleHand = visibleHand;
            Discard = discard;
        }

        public PlayerId PlayerId { get; }
        public CardId HeroId { get; }
        public CardId ClassPathId { get; }
        public int Resolution { get; }
        public int PaCurrent { get; }
        public int PaMax { get; }
        public int DeckCount { get; }
        public int HandCount { get; }
        public IReadOnlyList<CardInstance> VisibleHand { get; }
        public IReadOnlyList<CardInstance> Discard { get; }
    }

    public sealed class GameStateView
    {
        public GameStateView(
            PlayerId viewerId,
            int turn,
            MatchPhase phase,
            PlayerId activePlayerId,
            PlayerId priorityPlayerId,
            PlayerId firstPlayerId,
            int eventIndex,
            string hash,
            PlayerView player0,
            PlayerView player1)
        {
            ViewerId = viewerId;
            Turn = turn;
            Phase = phase;
            ActivePlayerId = activePlayerId;
            PriorityPlayerId = priorityPlayerId;
            FirstPlayerId = firstPlayerId;
            EventIndex = eventIndex;
            Hash = hash;
            Player0 = player0;
            Player1 = player1;
        }

        public PlayerId ViewerId { get; }
        public int Turn { get; }
        public MatchPhase Phase { get; }
        public PlayerId ActivePlayerId { get; }
        public PlayerId PriorityPlayerId { get; }
        public PlayerId FirstPlayerId { get; }
        public int EventIndex { get; }
        public string Hash { get; }
        public PlayerView Player0 { get; }
        public PlayerView Player1 { get; }

        public PlayerView Viewer => ViewerId.Value == 0 ? Player0 : Player1;
        public PlayerView Opponent => ViewerId.Value == 0 ? Player1 : Player0;

        public static GameStateView For(GameState state, PlayerId viewerId)
        {
            return new GameStateView(
                viewerId,
                state.Turn,
                state.Phase,
                state.ActivePlayerId,
                state.PriorityPlayerId,
                state.FirstPlayerId,
                state.EventIndex,
                GameStateHasher.Hash(state),
                BuildPlayer(state, PlayerId.Player0, viewerId),
                BuildPlayer(state, PlayerId.Player1, viewerId));
        }

        public static GameStateView For(GameState state, int viewerId)
        {
            return For(state, PlayerId.FromIndex(viewerId));
        }

        private static PlayerView BuildPlayer(GameState state, PlayerId playerId, PlayerId viewerId)
        {
            var player = state.GetPlayer(playerId);
            var showHand = playerId == viewerId;
            var visibleHand = new List<CardInstance>();
            if (showHand)
            {
                foreach (var id in player.Hand)
                {
                    visibleHand.Add(state.Cards[id]);
                }
            }

            var discard = new List<CardInstance>(player.Discard.Count);
            foreach (var id in player.Discard)
            {
                discard.Add(state.Cards[id]);
            }

            return new PlayerView(
                playerId,
                player.HeroId,
                player.ClassPathId,
                player.Resolution,
                player.PaCurrent,
                player.PaMax,
                player.Deck.Count,
                player.Hand.Count,
                visibleHand,
                discard);
        }
    }
}
