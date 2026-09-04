using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// In-place match mutations shared by command handlers. Callers must validate first.
    /// </summary>
    internal static class EngineActions
    {
        internal static void StartTurn(GameState state, PlayerId playerId)
        {
            TurnCycle.BeginTurn(state, playerId);
            TurnCycle.AdvanceUntilDecision(state);
        }

        internal static void SetPhase(GameState state, MatchPhase phase)
        {
            state.Phase = phase;
            state.Emit(GameEventType.PhaseChanged, Command.HostActorId, phase.ToString());
        }

        internal static void Move(GameState state, PlayerState player, CardInstanceId instanceId, CardZone destination)
        {
            var card = state.Cards[instanceId];
            RemoveFromZone(player, instanceId, card.Zone);
            card.SetZone(destination);
            ListFor(player, destination).Add(instanceId);
            state.Emit(GameEventType.CardMoved, player.PlayerId, instanceId + ":" + destination, instanceId);
        }

        internal static CommandResult Snapshot(GameState state, int fromIndex)
        {
            var events = new List<GameEvent>(state.Events.Count - fromIndex);
            for (var i = fromIndex; i < state.Events.Count; i++)
            {
                events.Add(state.Events[i]);
            }

            return CommandResult.Accept(state, events);
        }

        private static void RemoveFromZone(PlayerState player, CardInstanceId instanceId, CardZone zone)
        {
            ListFor(player, zone).Remove(instanceId);
        }

        private static List<CardInstanceId> ListFor(PlayerState player, CardZone zone)
        {
            switch (zone)
            {
                case CardZone.Deck:
                    return player.Deck;
                case CardZone.Hand:
                    return player.Hand;
                case CardZone.Discard:
                    return player.Discard;
                default:
                    throw new InvariantViolationException("Cannot move identity or board cards with ListFor.");
            }
        }
    }
}
