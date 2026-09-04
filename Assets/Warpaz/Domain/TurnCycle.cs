namespace Warpaz.Domain
{
    /// <summary>
    /// Automatic turn phases. Stops on Main, a pending choice, a reaction window, or GameOver.
    /// No coroutines or Update; the safety cap is <see cref="RuleConfig.MaximumEventsPerTurn"/>.
    /// </summary>
    internal static class TurnCycle
    {
        internal static void BeginTurn(GameState state, PlayerId playerId)
        {
            var previousActive = state.ActivePlayerId;
            state.Turn++;
            state.ActivePlayerId = playerId;
            state.PriorityPlayerId = playerId;
            state.Emit(GameEventType.TurnStarted, playerId, state.Turn.ToString());
            if (previousActive != playerId)
            {
                state.Emit(GameEventType.ActivePlayerChanged, playerId, playerId.ToString());
            }

            EngineActions.SetPhase(state, MatchPhase.Preparation);
        }

        internal static void AdvanceUntilDecision(GameState state)
        {
            var steps = 0;
            while (IsAutomatic(state.Phase))
            {
                steps++;
                if (steps > state.Rules.MaximumEventsPerTurn)
                {
                    throw new InvariantViolationException(
                        "Automatic phase limit exceeded (" + state.Rules.MaximumEventsPerTurn + ").");
                }

                switch (state.Phase)
                {
                    case MatchPhase.Preparation:
                        RunPreparation(state);
                        break;
                    case MatchPhase.Pa:
                        RunPa(state);
                        break;
                    case MatchPhase.Draw:
                        RunDraw(state);
                        break;
                    case MatchPhase.Closing:
                        RunClosing(state);
                        break;
                    default:
                        throw new InvariantViolationException("Unexpected automatic phase " + state.Phase + ".");
                }
            }
        }

        private static bool IsAutomatic(MatchPhase phase)
        {
            return phase == MatchPhase.Preparation
                || phase == MatchPhase.Pa
                || phase == MatchPhase.Draw
                || phase == MatchPhase.Closing;
        }

        private static void RunPreparation(GameState state)
        {
            state.Emit(GameEventType.PreparationCompleted, state.ActivePlayerId, string.Empty);
            EngineActions.SetPhase(state, MatchPhase.Pa);
        }

        private static void RunPa(GameState state)
        {
            var playerId = state.ActivePlayerId;
            var player = state.GetPlayer(playerId);
            var nextMax = player.PaMax + state.Rules.PaIncreasePerTurn;
            if (nextMax > state.Rules.PaLimit)
            {
                nextMax = state.Rules.PaLimit;
            }

            player.PaMax = nextMax;
            state.Emit(GameEventType.PaMaxIncreased, playerId, nextMax.ToString());
            player.PaCurrent = player.PaMax;
            state.Emit(GameEventType.PaRefilled, playerId, player.PaCurrent.ToString());
            EngineActions.SetPhase(state, MatchPhase.Draw);
        }

        private static void RunDraw(GameState state)
        {
            var playerId = state.ActivePlayerId;
            var player = state.GetPlayer(playerId);
            if (player.Deck.Count == 0)
            {
                state.Emit(GameEventType.DrawFailed, playerId, "0");
                var penalty = state.Rules.FailedDrawResolutionLoss;
                var next = player.Resolution - penalty;
                if (next < 0)
                {
                    next = 0;
                }

                player.Resolution = next;
                state.Emit(GameEventType.ResolutionRemoved, playerId, penalty.ToString());
            }
            else
            {
                MatchFactory.DrawCards(state, playerId, 1);
            }

            if (player.Resolution == 0)
            {
                TryEndMatch(state, playerId.Opponent, MatchEndCause.ResolutionDepleted);
                return;
            }

            EngineActions.SetPhase(state, MatchPhase.Main);
        }

        private static void RunClosing(GameState state)
        {
            state.GetPlayer(state.ActivePlayerId).FreeReorganizationUsed = false;
            BeginTurn(state, state.ActivePlayerId.Opponent);
        }

        private static void TryEndMatch(GameState state, PlayerId winner, MatchEndCause cause)
        {
            state.Result = MatchResult.Ended(winner, cause);
            state.Emit(GameEventType.MatchEnded, Command.HostActorId, state.Result.ToString());
            EngineActions.SetPhase(state, MatchPhase.GameOver);
        }
    }
}
