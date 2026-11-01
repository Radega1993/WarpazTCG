namespace Warpaz.Domain
{
    internal sealed class ChooseInterceptorHandler : ICommandHandler
    {
        private readonly ICardCatalog _catalog;

        public ChooseInterceptorHandler(ICardCatalog catalog)
        {
            _catalog = catalog;
        }

        public CommandType Type => CommandType.ChooseTargets;

        public CommandResult Handle(GameState state, Command command)
        {
            var pending = state.PendingDecision;
            if (state.Phase != MatchPhase.PendingChoice
                || pending.Kind != PendingDecisionKind.ChooseInterceptor)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPhase,
                    "ChooseInterceptor is only legal during an intercept window.");
            }

            if (command.ActorId != pending.ActorId)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.WrongActor,
                    "Only the defending player may choose an interceptor.");
            }

            if (command.PrimaryInstanceId.IsNone)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "ChooseInterceptor requires an interceptor instance.");
            }

            if (!IsLegalInterceptor(pending, command.PrimaryInstanceId))
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Interceptor is not legal for this attack.");
            }

            if (!state.TryGetCard(command.PrimaryInstanceId, out var interceptor)
                || !CombatPolicies.CanIntercept(interceptor))
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Interceptor must be a Ready Vanguard Character.");
            }

            if (!state.TryGetCard(pending.AttackerId, out var attacker) || !attacker.HasBoardSlot)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Attacker is no longer on the board.");
            }

            var from = state.EventIndex;
            EngineActions.Exhaust(state, interceptor);
            state.Emit(
                GameEventType.AttackIntercepted,
                command.ActorId,
                pending.AttackerId + ":" + interceptor.InstanceId,
                interceptor.InstanceId);
            EngineActions.ClosePendingDecision(state);
            CombatResolver.ResolveCharacterCombat(state, _catalog, attacker, interceptor);
            GameStateInvariants.AssertValid(state);
            return EngineActions.Snapshot(state, from);
        }

        private static bool IsLegalInterceptor(PendingDecision pending, CardInstanceId interceptorId)
        {
            for (var i = 0; i < pending.LegalInterceptorIds.Count; i++)
            {
                if (pending.LegalInterceptorIds[i] == interceptorId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
