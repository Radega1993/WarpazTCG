namespace Warpaz.Domain
{
    internal sealed class DeclineInterceptionHandler : ICommandHandler
    {
        private readonly ICardCatalog _catalog;

        public DeclineInterceptionHandler(ICardCatalog catalog)
        {
            _catalog = catalog;
        }

        public CommandType Type => CommandType.PassPriority;

        public CommandResult Handle(GameState state, Command command)
        {
            var pending = state.PendingDecision;
            if (state.Phase != MatchPhase.PendingChoice
                || pending.Kind != PendingDecisionKind.ChooseInterceptor)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPhase,
                    "DeclineInterception is only legal during an intercept window.");
            }

            if (command.ActorId != pending.ActorId)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.WrongActor,
                    "Only the defending player may decline interception.");
            }

            if (!pending.TargetsHero)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "DeclineInterception applies only to hero-targeted attacks.");
            }

            if (!state.TryGetCard(pending.AttackerId, out var attacker) || !attacker.HasBoardSlot)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Attacker is no longer on the board.");
            }

            var defender = state.GetPlayer(pending.ActorId);
            var from = state.EventIndex;
            state.Emit(
                GameEventType.InterceptionDeclined,
                command.ActorId,
                pending.AttackerId.ToString(),
                pending.AttackerId);
            EngineActions.ClosePendingDecision(state);
            CombatResolver.ResolveHeroCombat(state, _catalog, attacker, defender);
            GameStateInvariants.AssertValid(state);
            return EngineActions.Snapshot(state, from);
        }
    }
}
