namespace Warpaz.Domain
{
    internal sealed class ReorganizeHandler : ICommandHandler
    {
        public CommandType Type => CommandType.Reorganize;

        public CommandResult Handle(GameState state, Command command)
        {
            if (state.Phase != MatchPhase.Main)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPhase,
                    "Reorganize is only legal in Main.");
            }

            if (command.ActorId != state.ActivePlayerId)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.WrongActor,
                    "Actor cannot act in this phase.");
            }

            var player = state.GetPlayer(command.ActorId);
            if (player.FreeReorganizationUsed)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Free reorganization already used this turn.");
            }

            if (!command.HasReorganizeMode)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Reorganize mode is required.");
            }

            if (command.PrimaryInstanceId.IsNone
                || !state.TryGetCard(command.PrimaryInstanceId, out var first)
                || first.ControllerId != command.ActorId
                || !first.HasBoardSlot)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Reorganize requires a board Character you control.");
            }

            if (state.Rules.TacticalEnabled && !TacticalEffects.CanReorganize(first))
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Frozen or Immobilized Characters cannot reorganize.");
            }

            var from = state.EventIndex;
            if (command.ReorganizeMode == ReorganizeMode.MoveToEmptyOtherRow)
            {
                if (!command.HasBoardSlotTarget)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Move reorganization requires an empty destination slot.");
                }

                if (command.TargetSlotIndex < 0 || command.TargetSlotIndex >= BoardSlot.SlotsPerRow)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Invalid board slot index.");
                }

                if (command.TargetRow == first.Slot.Row)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Move reorganization must target the other row.");
                }

                var dest = GetSlot(player, command.TargetRow, command.TargetSlotIndex);
                if (dest.IsOccupied)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Destination slot is occupied.");
                }

                EngineActions.MoveBoardToEmptySlot(
                    state,
                    player,
                    first.InstanceId,
                    command.TargetRow,
                    command.TargetSlotIndex);
            }
            else if (command.ReorganizeMode == ReorganizeMode.SwapRows)
            {
                if (command.SecondaryInstanceId.IsNone
                    || !state.TryGetCard(command.SecondaryInstanceId, out var second)
                    || second.ControllerId != command.ActorId
                    || !second.HasBoardSlot)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Swap reorganization requires two board Characters you control.");
                }

                if (state.Rules.TacticalEnabled && !TacticalEffects.CanReorganize(second))
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Frozen or Immobilized Characters cannot reorganize.");
                }

                if (first.Slot.Row == second.Slot.Row)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Swap reorganization must be Vanguard <-> Rearguard.");
                }

                EngineActions.SwapBoardSlots(state, player, first.InstanceId, second.InstanceId);
            }
            else
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Unknown reorganize mode.");
            }

            player.FreeReorganizationUsed = true;
            GameStateInvariants.AssertValid(state);
            return EngineActions.Snapshot(state, from);
        }

        private static BoardSlot GetSlot(PlayerState player, BoardRow row, int index)
        {
            var offset = row == BoardRow.Vanguard ? 0 : BoardSlot.SlotsPerRow;
            return player.BoardSlots[offset + index];
        }
    }
}
