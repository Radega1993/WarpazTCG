using System.Collections.Generic;

namespace Warpaz.Domain
{
    internal sealed class AttackHandler : ICommandHandler
    {
        private readonly ICardCatalog _catalog;

        public AttackHandler(ICardCatalog catalog)
        {
            _catalog = catalog;
        }

        public CommandType Type => CommandType.Attack;

        public CommandResult Handle(GameState state, Command command)
        {
            if (state.Phase != MatchPhase.Main)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPhase,
                    "Attack is only legal in Main.");
            }

            if (command.ActorId != state.ActivePlayerId)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.WrongActor,
                    "Actor cannot act in this phase.");
            }

            if (command.PrimaryInstanceId.IsNone
                || !state.TryGetCard(command.PrimaryInstanceId, out var attacker)
                || attacker.ControllerId != command.ActorId)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Attack requires an attacker you control.");
            }

            var canDeclare = state.Rules.TacticalEnabled
                ? TacticalEffects.CanDeclareAttack(state, _catalog, attacker)
                : CombatPolicies.CanDeclareAttack(attacker);
            if (!canDeclare)
            {
                return CommandResult.Reject(
                    state,
                    CommandRejectionCode.InvalidPayload,
                    "Attacker must be Ready, without Recent/Frozen, and in a legal attack row.");
            }

            var defender = state.GetPlayer(command.ActorId.Opponent);
            CardInstance targetCharacter = null;
            if (command.TargetsHero)
            {
                if (!command.SecondaryInstanceId.IsNone)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Hero attack must not name a character target.");
                }

                if (!CombatPolicies.CanTargetHero(defender))
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Hero is not a legal target while Vanguard Characters remain.");
                }
            }
            else
            {
                if (command.SecondaryInstanceId.IsNone
                    || !state.TryGetCard(command.SecondaryInstanceId, out targetCharacter)
                    || targetCharacter.ControllerId != defender.PlayerId
                    || !targetCharacter.HasBoardSlot)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Attack target must be an enemy board Character.");
                }

                if (state.Rules.TacticalEnabled)
                {
                    if (!TacticalEffects.CanAttackCharacter(state, _catalog, attacker, targetCharacter))
                    {
                        return CommandResult.Reject(
                            state,
                            CommandRejectionCode.InvalidPayload,
                            "Attacker cannot reach that row without A distancia.");
                    }
                }
                else if (targetCharacter.Zone != CardZone.Vanguard)
                {
                    return CommandResult.Reject(
                        state,
                        CommandRejectionCode.InvalidPayload,
                        "Attack target must be an enemy Vanguard Character.");
                }
            }

            var from = state.EventIndex;
            EngineActions.Exhaust(state, attacker);
            state.Emit(
                GameEventType.AttackDeclared,
                command.ActorId,
                command.TargetsHero
                    ? attacker.InstanceId + ":Hero:" + defender.PlayerId
                    : attacker.InstanceId + ":" + targetCharacter.InstanceId,
                attacker.InstanceId);

            if (!command.TargetsHero)
            {
                CombatResolver.ResolveCharacterCombat(state, _catalog, attacker, targetCharacter);
                GameStateInvariants.AssertValid(state);
                return EngineActions.Snapshot(state, from);
            }

            var interceptors = TacticalEffects.IsAmbushAttack(state, _catalog, attacker)
                ? new List<CardInstanceId>()
                : CollectInterceptors(state, defender);
            if (interceptors.Count > 0)
            {
                EngineActions.OpenInterceptDecision(
                    state,
                    defender.PlayerId,
                    attacker.InstanceId,
                    CardInstanceId.None,
                    true,
                    interceptors);
                GameStateInvariants.AssertValid(state);
                return EngineActions.Snapshot(state, from);
            }

            CombatResolver.ResolveHeroCombat(state, _catalog, attacker, defender);
            GameStateInvariants.AssertValid(state);
            return EngineActions.Snapshot(state, from);
        }

        internal List<CardInstanceId> CollectInterceptors(GameState state, PlayerState defender)
        {
            var list = new List<CardInstanceId>();
            for (var i = 0; i < defender.BoardSlots.Count; i++)
            {
                var slot = defender.BoardSlots[i];
                if (!slot.IsOccupied)
                {
                    continue;
                }

                var card = state.Cards[slot.Occupant];
                if (CombatPolicies.CanIntercept(card, _catalog, state.Rules))
                {
                    list.Add(card.InstanceId);
                }
            }

            list.Sort((a, b) => a.Value.CompareTo(b.Value));
            return list;
        }
    }
}
