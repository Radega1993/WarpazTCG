namespace Warpaz.Domain
{
    internal sealed class ConfirmMulliganHandler : ICommandHandler
    {
        public CommandType Type => CommandType.ConfirmMulligan;

        public CommandResult Handle(GameState state, Command command)
        {
            var expectedPlayer = state.Phase == MatchPhase.MulliganP0
                ? PlayerId.Player0
                : state.Phase == MatchPhase.MulliganP1
                    ? PlayerId.Player1
                    : PlayerId.Host;
            if (!expectedPlayer.IsSeated)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPhase,
                    "Mulligan is not available in this phase.");
            }

            if (command.ActorId != expectedPlayer)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.WrongActor,
                    "Actor cannot act in this phase.");
            }

            var from = state.EventIndex;
            var player = state.GetPlayer(expectedPlayer);
            if (command.Decision == MulliganDecision.KeepHand)
            {
                state.Emit(
                    GameEventType.MulliganKept,
                    command.ActorId,
                    expectedPlayer.ToString());
            }
            else
            {
                var returned = player.Hand.Count;
                ReturnHandToDeck(state, player);
                state.Emit(
                    GameEventType.MulliganReturned,
                    command.ActorId,
                    returned.ToString());
                MatchFactory.ShuffleDeck(state, expectedPlayer);
                MatchFactory.DrawCards(state, expectedPlayer, state.Rules.InitialHandSize);
                state.Emit(
                    GameEventType.InitialHandDrawn,
                    command.ActorId,
                    player.Hand.Count.ToString());
            }

            if (state.Phase == MatchPhase.MulliganP0)
            {
                state.Phase = MatchPhase.MulliganP1;
                state.PriorityPlayerId = PlayerId.Player1;
                state.Emit(GameEventType.PhaseChanged, Command.HostActorId, MatchPhase.MulliganP1.ToString());
            }
            else
            {
                TurnCycle.BeginTurn(state, state.FirstPlayerId);
                TurnCycle.AdvanceUntilDecision(state);
            }

            GameStateInvariants.AssertValid(state);
            return EngineActions.Snapshot(state, from);
        }

        private static void ReturnHandToDeck(GameState state, PlayerState player)
        {
            while (player.Hand.Count > 0)
            {
                var id = player.Hand[0];
                player.Hand.RemoveAt(0);
                player.Deck.Add(id);
                state.Cards[id].SetZone(CardZone.Deck);
            }
        }
    }
}
