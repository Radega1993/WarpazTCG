namespace Warpaz.Domain
{
    internal sealed class PlayCharacterHandler : ICommandHandler
    {
        private readonly ICardCatalog _catalog;

        public PlayCharacterHandler(ICardCatalog catalog)
        {
            _catalog = catalog;
        }

        public CommandType Type => CommandType.Play;

        public CommandResult Handle(GameState state, Command command)
        {
            if (state.Rules.AdvancedEnabled && state.TryGetCard(command.PrimaryInstanceId, out var advancedSource)
                && _catalog.TryGet(advancedSource.DefinitionId, out var advancedDefinition) && advancedDefinition.Type == CardType.Action
                && AdvancedEffects.Supports(advancedDefinition)) return AdvancedEffects.Play(state, command, _catalog);
            if (state.TryGetCard(command.PrimaryInstanceId, out var source) && _catalog.TryGet(source.DefinitionId, out var def) && def.Type == CardType.Action)
                return EffectEngine.Play(state, command, _catalog);
            if (state.Phase != MatchPhase.Main)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPhase,
                    "Play is only legal in Main.");
            }

            if (command.ActorId != state.ActivePlayerId)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.WrongActor,
                    "Actor cannot act in this phase.");
            }

            if (command.PrimaryInstanceId.IsNone)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Play requires a card instance.");
            }

            if (!state.TryGetCard(command.PrimaryInstanceId, out var card))
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Unknown card instance.");
            }

            if (card.OwnerId != command.ActorId || card.Zone != CardZone.Hand)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Card must be in the actor's hand.");
            }

            if (!_catalog.TryGet(card.DefinitionId, out var definition))
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Unknown card definition.");
            }

            if (definition.Type != CardType.Character)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Only Characters can be played onto the board.");
            }

            if (!CharacterPositions.TryParse(definition.Position, out var position))
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Card has no playable board position.");
            }

            var cost = definition.CostPa ?? 0;
            var player = state.GetPlayer(command.ActorId);
            if (player.PaCurrent < cost)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Insufficient PA.");
            }

            var boardCount = CombatPolicies.CountBoardOccupants(player);
            BoardRow targetRow;
            int targetIndex;
            CardInstanceId replaceId = CardInstanceId.None;

            if (!command.SecondaryInstanceId.IsNone)
            {
                if (boardCount < BoardSlot.TotalCharacterSlots)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Replace is only legal when the board is full.");
                }

                if (!state.TryGetCard(command.SecondaryInstanceId, out var replaced)
                    || replaced.ControllerId != command.ActorId
                    || !replaced.HasBoardSlot)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Replace target must be a board Character you control.");
                }

                if (!CharacterPositions.AllowsRow(position, replaced.Slot.Row))
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Card cannot occupy that row.");
                }

                replaceId = replaced.InstanceId;
                targetRow = replaced.Slot.Row;
                targetIndex = replaced.Slot.Index;
            }
            else
            {
                if (!command.HasBoardSlotTarget)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Play requires a board slot or replace target.");
                }

                if (command.TargetSlotIndex < 0 || command.TargetSlotIndex >= BoardSlot.SlotsPerRow)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Invalid board slot index.");
                }

                if (boardCount >= BoardSlot.TotalCharacterSlots)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Board is full; Play must replace a Character.");
                }

                if (!CharacterPositions.AllowsRow(position, command.TargetRow))
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Card cannot occupy that row.");
                }

                var existing = GetSlot(player, command.TargetRow, command.TargetSlotIndex);
                if (existing.IsOccupied)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Target board slot is occupied.");
                }

                targetRow = command.TargetRow;
                targetIndex = command.TargetSlotIndex;
            }

            if (!CombatPolicies.CanPlayUnique(state, definition, command.ActorId, replaceId))
                return CommandResult.Reject(state, CommandRejectionCode.InvalidPayload, "Ya controlas este personaje Único.");
            var from = state.EventIndex;
            if (cost > 0)
            {
                EngineActions.SpendPa(state, player, cost);
            }

            if (!replaceId.IsNone)
            {
                EngineActions.ReplaceToDiscard(state, player, replaceId);
                state.Emit(
                    GameEventType.CharacterReplaced,
                    command.ActorId,
                    replaceId + ":" + command.PrimaryInstanceId,
                    command.PrimaryInstanceId);
            }

            EngineActions.PlaceOnBoard(state, player, command.PrimaryInstanceId, targetRow, targetIndex);
            card.ReadyState = ReadyState.Ready;
            EngineActions.ApplyRecent(state, card);
            state.Emit(
                GameEventType.CharacterPlayed,
                command.ActorId,
                command.PrimaryInstanceId + ":" + targetRow + ":" + targetIndex,
                command.PrimaryInstanceId);

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
