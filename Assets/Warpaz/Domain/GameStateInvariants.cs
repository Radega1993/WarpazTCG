using System.Collections.Generic;

namespace Warpaz.Domain
{
    public static class GameStateInvariants
    {
        public static void AssertValid(GameState state)
        {
            var reason = FindViolation(state);
            if (reason != null)
            {
                throw new InvariantViolationException(reason);
            }
        }

        public static string FindViolation(GameState state)
        {
            if (state == null)
            {
                return "GameState is null.";
            }

            if (state.EventIndex != state.Events.Count)
            {
                return "eventIndex does not match event log length.";
            }

            var seen = new HashSet<CardInstanceId>();
            var attached = new HashSet<CardInstanceId>();
            foreach (var card in state.Cards.Values)
            {
                if (!card.AttachedEquipmentId.IsNone)
                {
                    if (!state.Rules.AdvancedEnabled || !card.HasBoardSlot || !attached.Add(card.AttachedEquipmentId)
                        || !state.TryGetCard(card.AttachedEquipmentId, out var equipment) || equipment.Zone != CardZone.Equipment
                        || equipment.OwnerId != card.ControllerId || equipment.ControllerId != card.ControllerId)
                        return "Invalid or multiply attached equipment.";
                }
                if (card.TurnPowerModifier < -1000 || card.TurnPowerModifier > 1000)
                    return "Effect power modifier out of bounds.";
                if ((!state.Rules.EffectsEnabled || !card.HasBoardSlot) && (card.TurnPowerModifier != 0 || card.CannotInterceptThisTurn || card.CannotReadyThisTurn))
                    return "Effects outside supported rules or board zone.";
                if (!seen.Add(card.InstanceId))
                {
                    return "Duplicate instance id " + card.InstanceId + ".";
                }
            }

            foreach (var card in state.Cards.Values)
                if (card.Zone == CardZone.Equipment && !attached.Contains(card.InstanceId)) return "Orphan equipment.";
            if (state.Rules.AdvancedEnabled)
            {
                if (state.PendingDecision.IsPending != (state.Phase == MatchPhase.PendingChoice)) return "Pending phase mismatch.";
                if (state.PendingDecision.IsPending && state.PriorityPlayerId != state.PendingDecision.ActorId) return "Pending priority mismatch.";
                if (state.PendingDecision.Kind == PendingDecisionKind.EffectChoice && (state.EffectQueue.Count == 0 || state.PendingDecision.ChoiceIds.Count == 0)) return "Empty effect continuation.";
                if (state.PendingDecision.Kind == PendingDecisionKind.Reaction && state.Combat == null) return "Missing pending combat.";
                if (state.Result.IsFinished && (state.Combat != null || state.EffectQueue.Count != 0 || state.PendingDecision.IsPending)) return "Terminal continuation.";
            }

            for (var p = 0; p < 2; p++)
            {
                var player = state.Players[p];
                if (player == null)
                {
                    return "Missing player " + p + ".";
                }

                if (player.PaCurrent < 0 || player.PaMax < 0)
                {
                    return "PA is negative.";
                }

                if (player.PaCurrent > player.PaMax)
                {
                    return "paCurrent exceeds paMax.";
                }

                if (player.PaMax > state.Rules.PaLimit)
                {
                    return "paMax exceeds rule maximum.";
                }

                if (player.Resolution < 0 || player.Resolution > state.Rules.InitialResolution)
                {
                    return "Resolution out of range.";
                }

                if (OverlappingZoneLists(player))
                {
                    return "A CardInstance appears in more than one zone list for player " + p + ".";
                }

                if (CountZone(state, player, player.Deck, CardZone.Deck) < 0)
                {
                    return "Deck zone invariant failed for player " + p + ".";
                }

                if (CountZone(state, player, player.Hand, CardZone.Hand) < 0)
                {
                    return "Hand zone invariant failed for player " + p + ".";
                }

                if (CountZone(state, player, player.Discard, CardZone.Discard) < 0)
                {
                    return "Discard zone invariant failed for player " + p + ".";
                }

                var boardCount = CountBoardOccupants(state, player, out var boardReason);
                if (boardReason != null)
                {
                    return boardReason;
                }

                var equipmentCount = 0;
                foreach (var id in attached) if (state.Cards[id].OwnerId == player.PlayerId) equipmentCount++;
                if (player.Deck.Count + player.Hand.Count + player.Discard.Count + boardCount + equipmentCount != state.Rules.DeckSize)
                {
                    return "Player " + p + " does not have " + state.Rules.DeckSize + " main-deck instances.";
                }

                if (!IdentityInZone(state, player.HeroInstanceId, player.PlayerId, CardZone.Hero) ||
                    !IdentityInZone(state, player.ClassInstanceId, player.PlayerId, CardZone.ClassPath))
                {
                    return "Identity cards missing for player " + p + ".";
                }
            }

            return null;
        }

        private static bool OverlappingZoneLists(PlayerState player)
        {
            var seen = new HashSet<CardInstanceId>();
            if (!AddAll(seen, player.Deck) || !AddAll(seen, player.Hand) || !AddAll(seen, player.Discard))
            {
                return true;
            }

            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var occupant = player.BoardSlots[i].Occupant;
                if (occupant.IsNone)
                {
                    continue;
                }

                if (!seen.Add(occupant))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AddAll(HashSet<CardInstanceId> seen, List<CardInstanceId> ids)
        {
            foreach (var id in ids)
            {
                if (!seen.Add(id))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountBoardOccupants(GameState state, PlayerState player, out string reason)
        {
            reason = null;
            var vanguard = 0;
            var rearguard = 0;
            var occupants = new HashSet<CardInstanceId>();

            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var slot = player.BoardSlots[i];
                if (!slot.IsOccupied)
                {
                    continue;
                }

                if (!occupants.Add(slot.Occupant))
                {
                    reason = "Duplicate board occupant " + slot.Occupant + " for player " + player.PlayerId + ".";
                    return -1;
                }

                if (!state.Cards.TryGetValue(slot.Occupant, out var card))
                {
                    reason = "Board occupant " + slot.Occupant + " missing from Cards.";
                    return -1;
                }

                if (card.OwnerId != player.PlayerId || card.ControllerId != player.PlayerId)
                {
                    reason = "Board occupant " + slot.Occupant + " has wrong owner/controller.";
                    return -1;
                }

                var expectedZone = slot.Row == BoardRow.Vanguard ? CardZone.Vanguard : CardZone.Rearguard;
                if (card.Zone != expectedZone)
                {
                    reason = "Board occupant " + slot.Occupant + " zone mismatch.";
                    return -1;
                }

                if (!card.HasBoardSlot
                    || card.Slot.Row != slot.Row
                    || card.Slot.Index != slot.Index
                    || card.Slot.Occupant != slot.Occupant)
                {
                    reason = "Board occupant " + slot.Occupant + " slot metadata mismatch.";
                    return -1;
                }

                if (slot.IsVanguard)
                {
                    vanguard++;
                }
                else
                {
                    rearguard++;
                }
            }

            if (vanguard > BoardSlot.SlotsPerRow || rearguard > BoardSlot.SlotsPerRow)
            {
                reason = "Board row occupancy exceeds max 2 for player " + player.PlayerId + ".";
                return -1;
            }

            return occupants.Count;
        }

        private static int CountZone(
            GameState state,
            PlayerState player,
            List<CardInstanceId> ids,
            CardZone expected)
        {
            foreach (var id in ids)
            {
                if (!state.Cards.TryGetValue(id, out var card))
                {
                    return -1;
                }

                if (card.OwnerId != player.PlayerId || card.Zone != expected)
                {
                    return -1;
                }
            }

            return ids.Count;
        }

        private static bool IdentityInZone(
            GameState state,
            CardInstanceId instanceId,
            PlayerId playerId,
            CardZone expected)
        {
            return state.Cards.TryGetValue(instanceId, out var card)
                && card.OwnerId == playerId
                && card.Zone == expected;
        }
    }

    public sealed class InvariantViolationException : System.Exception
    {
        public InvariantViolationException(string message)
            : base(message)
        {
        }
    }
}
