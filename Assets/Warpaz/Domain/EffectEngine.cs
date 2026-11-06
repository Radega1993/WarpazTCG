using System;
using System.Linq;

namespace Warpaz.Domain
{
    /// <summary>Bounded synchronous programs: validate the entire program before paying or mutating.</summary>
    public static class EffectEngine
    {
        public const int MaximumInstructions = 16;
        public static bool Supports(CardDefinition card)
        {
            if (card == null || card.Type != CardType.Action || card.Effects.Count == 0 || card.Effects.Count > MaximumInstructions) return false;
            foreach (var effect in card.Effects)
                if (effect == null || effect.Opcode > EffectOpcode.CannotReadyUntilEndTurn || !Enum.IsDefined(typeof(EffectOpcode), effect.Opcode) || !Enum.IsDefined(typeof(EffectTarget), effect.Target)
                    || effect.Amount < -20 || effect.Amount > 20
                    || (effect.Opcode != EffectOpcode.PowerUntilEndTurn && effect.Amount != 0)) return false;
            return true;
        }
        public static bool CanTarget(GameState state, CardDefinition action, PlayerId actor, CardInstanceId id)
        {
            if (!state.Rules.EffectsEnabled || !Supports(action) || !state.TryGetCard(id, out var target) || !target.HasBoardSlot) return false;
            foreach (var effect in action.Effects)
            {
                switch (effect.Target)
                {
                    case EffectTarget.FriendlyVanguard: if (target.ControllerId != actor || target.Zone != CardZone.Vanguard) return false; break;
                    case EffectTarget.EnemyCharacter: if (target.ControllerId == actor) return false; break;
                    case EffectTarget.EnemyVanguard: if (target.ControllerId == actor || target.Zone != CardZone.Vanguard) return false; break;
                }
            }
            long next = target.TurnPowerModifier;
            foreach (var effect in action.Effects) if (effect.Opcode == EffectOpcode.PowerUntilEndTurn) next += effect.Amount;
            return next >= -1000 && next <= 1000;
        }
        internal static CommandResult Play(GameState state, Command command, ICardCatalog catalog)
        {
            if (state.Phase != MatchPhase.Main || command.ActorId != state.ActivePlayerId)
                return CommandResult.Reject(state, CommandRejectionCode.InvalidPhase, "Las Acciones se juegan en tu fase Main.");
            if (!state.TryGetCard(command.PrimaryInstanceId, out var source) || source.Zone != CardZone.Hand || source.OwnerId != command.ActorId
                || !catalog.TryGet(source.DefinitionId, out var definition) || !CanTarget(state, definition, command.ActorId, command.SecondaryInstanceId)
                || command.HasBoardSlotTarget || command.TargetsHero)
                return CommandResult.Reject(state, CommandRejectionCode.InvalidPayload, "Acción no soportada u objetivo ilegal.");
            var player = state.GetPlayer(command.ActorId); var cost = definition.CostPa ?? 0;
            if (cost < 0 || player.PaCurrent < cost) return CommandResult.Reject(state, CommandRejectionCode.InvalidPayload, "PA insuficientes.");
            var from = state.EventIndex;
            EngineActions.SpendPa(state, player, cost);
            EngineActions.Move(state, player, source.InstanceId, CardZone.Discard);
            var target = state.Cards[command.SecondaryInstanceId];
            state.Emit(GameEventType.ActionPlayed, command.ActorId, source.InstanceId + ":" + target.InstanceId, source.InstanceId);
            foreach (var effect in definition.Effects)
            {
                switch (effect.Opcode)
                {
                    case EffectOpcode.PowerUntilEndTurn: target.TurnPowerModifier += effect.Amount; break;
                    case EffectOpcode.CannotInterceptUntilEndTurn: target.CannotInterceptThisTurn = true; break;
                    case EffectOpcode.CannotReadyUntilEndTurn: target.CannotReadyThisTurn = true; break;
                }
                state.Emit(GameEventType.EffectResolved, command.ActorId, source.InstanceId + ":" + effect.Opcode + ":" + target.InstanceId, target.InstanceId);
            }
            GameStateInvariants.AssertValid(state);
            return EngineActions.Snapshot(state, from);
        }
        internal static void Expire(GameState state)
        {
            if (!state.Rules.EffectsEnabled) return;
            foreach (var card in state.Cards.Values.OrderBy(c => c.InstanceId.Value))
            {
                if (card.TurnPowerModifier == 0 && !card.CannotInterceptThisTurn && !card.CannotReadyThisTurn) continue;
                card.ClearTurnEffects();
                state.Emit(GameEventType.EffectExpired, card.ControllerId, card.InstanceId.ToString(), card.InstanceId);
            }
        }
        public static int EffectivePower(CardDefinition definition, CardInstance card) => Math.Max(0, CombatPolicies.EffectivePower(definition) + card.TurnPowerModifier);
    }
}
