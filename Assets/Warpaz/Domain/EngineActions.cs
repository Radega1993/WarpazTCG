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

        internal static void PlaceOnBoard(
            GameState state,
            PlayerState player,
            CardInstanceId instanceId,
            BoardRow row,
            int slotIndex)
        {
            var card = state.Cards[instanceId];
            if (card.Zone != CardZone.Hand)
            {
                throw new InvariantViolationException("PlaceOnBoard requires the card to be in hand.");
            }

            RemoveFromZone(player, instanceId, CardZone.Hand);
            var zone = row == BoardRow.Vanguard ? CardZone.Vanguard : CardZone.Rearguard;
            var slot = new BoardSlot(row, slotIndex, instanceId);
            card.SetZone(zone, slot);
            player.SetBoardSlot(slot);
            state.Emit(GameEventType.CardMoved, player.PlayerId, instanceId + ":" + zone, instanceId);
        }

        internal static void ClearBoardSlot(GameState state, PlayerState player, BoardSlot slot)
        {
            if (!slot.IsOccupied)
            {
                return;
            }

            player.SetBoardSlot(BoardSlot.Empty(slot.Row, slot.Index));
        }

        internal static void SwapBoardSlots(
            GameState state,
            PlayerState player,
            CardInstanceId firstId,
            CardInstanceId secondId)
        {
            var first = state.Cards[firstId];
            var second = state.Cards[secondId];
            var firstSlot = first.Slot;
            var secondSlot = second.Slot;

            ClearBoardSlot(state, player, firstSlot);
            ClearBoardSlot(state, player, secondSlot);

            var firstDest = new BoardSlot(secondSlot.Row, secondSlot.Index, firstId);
            var secondDest = new BoardSlot(firstSlot.Row, firstSlot.Index, secondId);
            var firstZone = firstDest.Row == BoardRow.Vanguard ? CardZone.Vanguard : CardZone.Rearguard;
            var secondZone = secondDest.Row == BoardRow.Vanguard ? CardZone.Vanguard : CardZone.Rearguard;

            first.SetZone(firstZone, firstDest);
            second.SetZone(secondZone, secondDest);
            player.SetBoardSlot(firstDest);
            player.SetBoardSlot(secondDest);

            state.Emit(GameEventType.CardMoved, player.PlayerId, firstId + ":" + firstZone, firstId);
            state.Emit(GameEventType.CardMoved, player.PlayerId, secondId + ":" + secondZone, secondId);
        }

        internal static void MoveBoardToEmptySlot(
            GameState state,
            PlayerState player,
            CardInstanceId instanceId,
            BoardRow targetRow,
            int targetSlotIndex)
        {
            var card = state.Cards[instanceId];
            var from = card.Slot;
            ClearBoardSlot(state, player, from);

            var zone = targetRow == BoardRow.Vanguard ? CardZone.Vanguard : CardZone.Rearguard;
            var dest = new BoardSlot(targetRow, targetSlotIndex, instanceId);
            card.SetZone(zone, dest);
            player.SetBoardSlot(dest);
            state.Emit(GameEventType.CardMoved, player.PlayerId, instanceId + ":" + zone, instanceId);
        }

        internal static void KnockOutToDiscard(GameState state, PlayerState player, CardInstanceId instanceId)
        {
            SendBoardCharacterToDiscard(state, player, instanceId, emitKnockedOut: true);
        }

        /// <summary>P-ReplaceSlot: substitution discard without Last Breath / K.O. framing.</summary>
        internal static void ReplaceToDiscard(GameState state, PlayerState player, CardInstanceId instanceId)
        {
            SendBoardCharacterToDiscard(state, player, instanceId, emitKnockedOut: false);
        }

        private static void SendBoardCharacterToDiscard(
            GameState state,
            PlayerState player,
            CardInstanceId instanceId,
            bool emitKnockedOut)
        {
            var card = state.Cards[instanceId];
            if (card.HasBoardSlot)
            {
                ClearBoardSlot(state, player, card.Slot);
            }
            else if (card.Zone == CardZone.Hand)
            {
                RemoveFromZone(player, instanceId, CardZone.Hand);
            }
            else if (card.Zone == CardZone.Deck)
            {
                RemoveFromZone(player, instanceId, CardZone.Deck);
            }
            else if (card.Zone == CardZone.Discard)
            {
                return;
            }
            else
            {
                throw new InvariantViolationException("Cannot discard card in zone " + card.Zone + ".");
            }

            card.ReadyState = ReadyState.Ready;
            card.RemoveStatus(StatusType.Recent);
            card.SetZone(CardZone.Discard);
            player.Discard.Add(instanceId);
            if (emitKnockedOut)
            {
                state.Emit(GameEventType.CharacterKnockedOut, player.PlayerId, instanceId.ToString(), instanceId);
            }

            state.Emit(GameEventType.CardMoved, player.PlayerId, instanceId + ":" + CardZone.Discard, instanceId);
        }

        internal static void SpendPa(GameState state, PlayerState player, int amount)
        {
            player.PaCurrent -= amount;
            state.Emit(GameEventType.PaSpent, player.PlayerId, amount.ToString());
        }

        internal static void Exhaust(GameState state, CardInstance card)
        {
            card.ReadyState = ReadyState.Exhausted;
            state.Emit(GameEventType.CharacterExhausted, card.ControllerId, card.InstanceId.ToString(), card.InstanceId);
        }

        internal static void Ready(GameState state, CardInstance card)
        {
            if (card.CannotReadyThisTurn) return;
            card.ReadyState = ReadyState.Ready;
            state.Emit(GameEventType.CharacterReadied, card.ControllerId, card.InstanceId.ToString(), card.InstanceId);
        }

        internal static void ApplyRecent(GameState state, CardInstance card)
        {
            card.AddStatus(StatusType.Recent);
            state.Emit(GameEventType.RecentApplied, card.ControllerId, card.InstanceId.ToString(), card.InstanceId);
        }

        internal static void RemoveRecent(GameState state, CardInstance card)
        {
            card.RemoveStatus(StatusType.Recent);
            state.Emit(GameEventType.RecentRemoved, card.ControllerId, card.InstanceId.ToString(), card.InstanceId);
        }

        internal static void ClosePendingDecision(GameState state)
        {
            var pending = state.PendingDecision;
            state.PendingDecision = PendingDecision.None;
            state.Emit(GameEventType.DecisionClosed, pending.ActorId, pending.Kind.ToString());
            if (state.Phase == MatchPhase.PendingChoice)
            {
                SetPhase(state, MatchPhase.Main);
            }

            state.PriorityPlayerId = state.ActivePlayerId;
        }

        internal static void OpenInterceptDecision(
            GameState state,
            PlayerId defenderId,
            CardInstanceId attackerId,
            CardInstanceId declaredTargetId,
            bool targetsHero,
            IReadOnlyList<CardInstanceId> legalInterceptorIds)
        {
            state.PendingDecision = PendingDecision.Intercept(
                defenderId,
                attackerId,
                declaredTargetId,
                targetsHero,
                legalInterceptorIds);
            state.PriorityPlayerId = defenderId;
            SetPhase(state, MatchPhase.PendingChoice);
            state.Emit(GameEventType.DecisionOpened, defenderId, PendingDecisionKind.ChooseInterceptor.ToString());
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
