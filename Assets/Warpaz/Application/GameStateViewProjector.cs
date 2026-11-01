using System;
using System.Collections.Generic;
using System.Globalization;
using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Builds a <see cref="MatchView"/> for one seated viewer. Does not decide legality.
    /// </summary>
    public static class GameStateViewProjector
    {
        public static MatchView Project(
            GameState state,
            PlayerId viewerId,
            MatchViewMode mode = MatchViewMode.Normal)
        {
            if (state == null)
            {
                return null;
            }

            var isDebug = mode == MatchViewMode.Debug;
            var events = EventLogProjector.Project(state, viewerId, mode);
            DebugMatchView debug = null;
            if (isDebug)
            {
                var rng = state.SaveRng();
                debug = new DebugMatchView(
                    rng.Seed.ToString(CultureInfo.InvariantCulture)
                    + ":" + rng.State.ToString(CultureInfo.InvariantCulture),
                    CopyIds(state.GetPlayer(PlayerId.Player0).DeckInstanceIds),
                    CopyIds(state.GetPlayer(PlayerId.Player1).DeckInstanceIds),
                    CardsFrom(state, state.GetPlayer(PlayerId.Player0).HandInstanceIds),
                    CardsFrom(state, state.GetPlayer(PlayerId.Player1).HandInstanceIds),
                    EventLogProjector.Project(state, viewerId, MatchViewMode.Debug));
            }

            return new MatchView(
                viewerId,
                state.Turn,
                state.Phase,
                state.ActivePlayerId,
                state.PriorityPlayerId,
                state.FirstPlayerId,
                state.EventIndex,
                GameStateHasher.Hash(state),
                state.Seed,
                isDebug,
                BuildPlayer(state, PlayerId.Player0, viewerId),
                BuildPlayer(state, PlayerId.Player1, viewerId),
                events,
                debug);
        }

        public static MatchView Project(GameState state, int viewerId, MatchViewMode mode = MatchViewMode.Normal)
        {
            return Project(state, PlayerId.FromIndex(viewerId), mode);
        }

        private static PlayerMatchView BuildPlayer(GameState state, PlayerId playerId, PlayerId viewerId)
        {
            var player = state.GetPlayer(playerId);
            var visibleHand = playerId == viewerId
                ? CardsFrom(state, player.HandInstanceIds)
                : (IReadOnlyList<CardView>)Array.Empty<CardView>();

            return new PlayerMatchView(
                playerId,
                player.HeroId,
                player.ClassPathId,
                player.Resolution,
                player.PaCurrent,
                player.PaMax,
                player.DeckInstanceIds.Count,
                player.HandInstanceIds.Count,
                visibleHand,
                CardsFrom(state, player.DiscardInstanceIds),
                BoardFrom(state, player));
        }

        private static IReadOnlyList<BoardSlotView> BoardFrom(GameState state, PlayerState player)
        {
            var slots = new List<BoardSlotView>(player.BoardSlots.Count);
            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var slot = player.BoardSlots[i];
                CardView occupant = null;
                var ready = ReadyState.Ready;
                var recent = false;
                if (!slot.Occupant.IsNone && state.TryGetCard(slot.Occupant, out var card))
                {
                    occupant = new CardView(card.InstanceId, card.DefinitionId);
                    ready = card.ReadyState;
                    recent = HasStatus(card, StatusType.Recent);
                }

                slots.Add(new BoardSlotView(slot.Row, slot.Index, occupant, ready, recent));
            }

            return slots;
        }

        private static bool HasStatus(CardInstance card, StatusType status)
        {
            for (var i = 0; i < card.Statuses.Count; i++)
            {
                if (card.Statuses[i] == status)
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<CardView> CardsFrom(GameState state, IReadOnlyList<CardInstanceId> ids)
        {
            var cards = new List<CardView>(ids.Count);
            for (var i = 0; i < ids.Count; i++)
            {
                if (!state.TryGetCard(ids[i], out var card))
                {
                    continue;
                }

                cards.Add(new CardView(card.InstanceId, card.DefinitionId));
            }

            return cards;
        }

        private static IReadOnlyList<CardInstanceId> CopyIds(IReadOnlyList<CardInstanceId> source)
        {
            var copy = new List<CardInstanceId>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                copy.Add(source[i]);
            }

            return copy;
        }
    }
}
