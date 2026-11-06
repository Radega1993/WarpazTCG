namespace Warpaz.Domain
{
    internal sealed class EndTurnHandler : ICommandHandler
    {
        public CommandType Type => CommandType.EndTurn;

        public CommandResult Handle(GameState state, Command command)
        {
            if (state.Phase != MatchPhase.Main)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPhase,
                    "EndTurn is only legal in Main.");
            }

            if (command.ActorId != state.ActivePlayerId)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.WrongActor,
                    "Actor cannot act in this phase.");
            }

            var from = state.EventIndex;
            EffectEngine.Expire(state);
            state.Emit(GameEventType.TurnEnded, command.ActorId, state.ActivePlayerId.ToString());
            EngineActions.SetPhase(state, MatchPhase.Closing);
            TurnCycle.AdvanceUntilDecision(state);
            GameStateInvariants.AssertValid(state);
            return EngineActions.Snapshot(state, from);
        }
    }
}
