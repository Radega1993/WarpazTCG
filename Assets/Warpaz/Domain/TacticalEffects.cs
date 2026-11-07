using System.Linq;

namespace Warpaz.Domain
{
    /// <summary>v0.6 statuses and ranged targeting. Keywords stay on definitions; opcodes only when text needs a program.</summary>
    public static class TacticalEffects
    {
        public const string RangedKeyword = "A distancia";
        public const string HiddenKeyword = "Oculto";
        public const string AmbushPrefix = "Emboscada";
        public const string ExposedPrefix = "Desprotegido";

        public static bool Supports(CardDefinition definition)
        {
            if (definition == null || definition.Effects.Count != 1) return false;
            var effect = definition.Effects[0];
            if (effect == null || effect.Target != EffectTarget.AnyCharacter) return false;
            switch (definition.Type)
            {
                case CardType.Action:
                    return (effect.Opcode == EffectOpcode.FreezeExhaust || effect.Opcode == EffectOpcode.DetectEnemy)
                        && effect.Amount == 0;
                case CardType.Character:
                    return (effect.Opcode == EffectOpcode.DeployFreeze
                            || effect.Opcode == EffectOpcode.DeployClearCrowdControl
                            || effect.Opcode == EffectOpcode.DeployDetect)
                        && effect.Amount == 0;
                case CardType.Equipment:
                    return effect.Opcode == EffectOpcode.EquipmentAttackRearguard && effect.Amount == 0;
                default:
                    return false;
            }
        }

        internal static EffectOpcode? Program(ICardCatalog catalog, CardInstance card)
            => catalog.TryGet(card.DefinitionId, out var d) && Supports(d) ? d.Effects[0].Opcode : (EffectOpcode?)null;

        public static bool HasKeyword(CardDefinition definition, string keyword)
        {
            if (definition?.Keywords == null || string.IsNullOrEmpty(keyword)) return false;
            for (var i = 0; i < definition.Keywords.Count; i++)
                if (definition.Keywords[i] == keyword) return true;
            return false;
        }

        public static bool HasStatus(CardInstance card, StatusType status)
        {
            if (card == null) return false;
            var statuses = card.Statuses;
            for (var i = 0; i < statuses.Count; i++)
                if (statuses[i] == status) return true;
            return false;
        }

        public static bool HasRanged(GameState state, ICardCatalog catalog, CardInstance card)
        {
            if (card == null || catalog == null || !catalog.TryGet(card.DefinitionId, out var definition)) return false;
            if (HasKeyword(definition, RangedKeyword)) return true;
            if (card.AttachedEquipmentId.IsNone) return false;
            return catalog.TryGet(state.Cards[card.AttachedEquipmentId].DefinitionId, out var equipment)
                && HasKeyword(equipment, RangedKeyword);
        }

        public static bool HasAmbush(GameState state, ICardCatalog catalog, CardInstance card)
        {
            return AmbushValue(state, catalog, card) > 0;
        }

        public static int AmbushValue(GameState state, ICardCatalog catalog, CardInstance card)
        {
            if (card == null || catalog == null || !catalog.TryGet(card.DefinitionId, out var definition)) return 0;
            var value = KeywordValue(definition, AmbushPrefix);
            if (!card.AttachedEquipmentId.IsNone
                && catalog.TryGet(state.Cards[card.AttachedEquipmentId].DefinitionId, out var equipment))
                value += KeywordValue(equipment, AmbushPrefix);
            return value;
        }

        public static int ExposedValue(CardDefinition definition) => KeywordValue(definition, ExposedPrefix);

        private static int KeywordValue(CardDefinition definition, string prefix)
        {
            if (definition?.Keywords == null) return 0;
            var total = 0;
            for (var i = 0; i < definition.Keywords.Count; i++)
            {
                var keyword = definition.Keywords[i];
                if (keyword == null || !keyword.StartsWith(prefix, System.StringComparison.Ordinal)) continue;
                if (keyword.Length == prefix.Length) { total += 1; continue; }
                if (keyword.Length > prefix.Length + 1 && keyword[prefix.Length] == ' '
                    && int.TryParse(keyword.Substring(prefix.Length + 1), out var parsed) && parsed > 0)
                    total += parsed;
            }
            return total;
        }

        public static bool IsAmbushAttack(GameState state, ICardCatalog catalog, CardInstance attacker)
            => state.Rules.TacticalEnabled && attacker != null && HasAmbush(state, catalog, attacker)
               && attacker.Zone == CardZone.Rearguard;

        public static bool CanDeclareAttack(GameState state, ICardCatalog catalog, CardInstance attacker)
        {
            if (attacker == null || attacker.ReadyState != ReadyState.Ready || CombatPolicies.HasRecent(attacker)) return false;
            if (HasStatus(attacker, StatusType.Frozen)) return false;
            if (attacker.Zone == CardZone.Vanguard) return true;
            return state.Rules.TacticalEnabled
                && attacker.Zone == CardZone.Rearguard
                && (HasRanged(state, catalog, attacker) || HasAmbush(state, catalog, attacker));
        }

        public static bool CanAttackCharacter(
            GameState state,
            ICardCatalog catalog,
            CardInstance attacker,
            CardInstance target)
        {
            if (!CanDeclareAttack(state, catalog, attacker)
                || target == null
                || !target.HasBoardSlot
                || target.ControllerId == attacker.ControllerId)
                return false;
            if (IsHiddenFrom(attacker.ControllerId, target)) return false;
            if (target.Zone == CardZone.Vanguard) return true;
            return state.Rules.TacticalEnabled
                && (HasRanged(state, catalog, attacker) || HasAmbush(state, catalog, attacker));
        }

        public static bool IsHiddenFrom(PlayerId viewer, CardInstance card)
        {
            if (card == null || viewer == card.ControllerId) return false;
            return HasStatus(card, StatusType.Hidden) && !HasStatus(card, StatusType.Detected);
        }

        public static bool CanReorganize(CardInstance card)
        {
            return card != null
                && card.HasBoardSlot
                && !HasStatus(card, StatusType.Frozen)
                && !HasStatus(card, StatusType.Immobilized);
        }

        public static bool CanPlay(GameState state, ICardCatalog catalog, PlayerId actor, CardInstanceId sourceId, CardInstanceId targetId)
        {
            if (!state.Rules.TacticalEnabled || state.Phase != MatchPhase.Main || actor != state.ActivePlayerId
                || !state.TryGetCard(sourceId, out var source) || source.OwnerId != actor || source.Zone != CardZone.Hand
                || !catalog.TryGet(source.DefinitionId, out var definition) || !Supports(definition)
                || (definition.CostPa ?? 0) < 0 || state.GetPlayer(actor).PaCurrent < (definition.CostPa ?? 0))
                return false;
            if (definition.Type != CardType.Action) return false;
            if (!state.TryGetCard(targetId, out var target) || !target.HasBoardSlot || target.ControllerId == actor) return false;
            if (definition.Effects[0].Opcode == EffectOpcode.FreezeExhaust)
                return catalog.TryGet(target.DefinitionId, out var targetDefinition)
                    && CombatPolicies.EffectivePower(targetDefinition) <= 4;
            if (definition.Effects[0].Opcode == EffectOpcode.DetectEnemy) return true;
            return false;
        }

        internal static CommandResult Play(GameState state, Command command, ICardCatalog catalog)
        {
            if (!CanPlay(state, catalog, command.ActorId, command.PrimaryInstanceId, command.SecondaryInstanceId)
                || command.HasBoardSlotTarget || command.TargetsHero || command.Type != CommandType.Play)
                return CommandResult.Reject(state, CommandRejectionCode.InvalidPayload, "Acción táctica ilegal.");
            var source = state.Cards[command.PrimaryInstanceId];
            catalog.TryGet(source.DefinitionId, out var definition);
            var target = state.Cards[command.SecondaryInstanceId];
            var from = state.EventIndex;
            var player = state.GetPlayer(command.ActorId);
            EngineActions.SpendPa(state, player, definition.CostPa ?? 0);
            EngineActions.Move(state, player, source.InstanceId, CardZone.Discard);
            state.Emit(GameEventType.ActionPlayed, command.ActorId, source.InstanceId + ":" + target.InstanceId, source.InstanceId);
            if (definition.Effects[0].Opcode == EffectOpcode.FreezeExhaust)
            {
                ApplyFreezeExhaust(state, target);
                state.Emit(GameEventType.EffectResolved, command.ActorId, source.InstanceId + ":FreezeExhaust:" + target.InstanceId, target.InstanceId);
            }
            else if (definition.Effects[0].Opcode == EffectOpcode.DetectEnemy)
            {
                ApplyDetect(state, target);
                state.EffectQueue.Add(new EffectStep(command.ActorId, source.InstanceId, EffectStepKind.Draw));
                state.EffectQueue.Add(new EffectStep(command.ActorId, source.InstanceId, EffectStepKind.BottomHand));
                AdvancedEffects.Drain(state, catalog);
            }
            GameStateInvariants.AssertValid(state);
            return EngineActions.Snapshot(state, from);
        }

        internal static void ApplyHiddenOnPlay(GameState state, ICardCatalog catalog, CardInstance card)
        {
            if (!state.Rules.TacticalEnabled || card == null || card.Zone != CardZone.Rearguard) return;
            if (!catalog.TryGet(card.DefinitionId, out var definition) || !HasKeyword(definition, HiddenKeyword)) return;
            card.AddStatus(StatusType.Hidden);
            state.Emit(GameEventType.EffectResolved, card.ControllerId, "Hidden:" + card.InstanceId, card.InstanceId);
        }

        internal static void ApplyDetect(GameState state, CardInstance target)
        {
            if (target == null) return;
            if (HasStatus(target, StatusType.Hidden)) target.RemoveStatus(StatusType.Hidden);
            target.AddStatus(StatusType.Detected);
            state.Emit(GameEventType.EffectResolved, target.ControllerId, "Detected:" + target.InstanceId, target.InstanceId);
        }

        internal static void ApplyFreezeExhaust(GameState state, CardInstance target)
        {
            var alreadyExhausted = target.ReadyState == ReadyState.Exhausted;
            if (!alreadyExhausted) EngineActions.Exhaust(state, target);
            if (alreadyExhausted)
            {
                target.AddStatus(StatusType.Frozen);
                state.Emit(GameEventType.EffectResolved, target.ControllerId, "Frozen:" + target.InstanceId, target.InstanceId);
            }
        }

        internal static void ApplyFreeze(GameState state, CardInstance target)
        {
            target.AddStatus(StatusType.Frozen);
            if (target.ReadyState == ReadyState.Ready) EngineActions.Exhaust(state, target);
            state.Emit(GameEventType.EffectResolved, target.ControllerId, "Frozen:" + target.InstanceId, target.InstanceId);
        }

        internal static void ClearCrowdControl(GameState state, CardInstance target)
        {
            var cleared = false;
            if (HasStatus(target, StatusType.Frozen))
            {
                target.RemoveStatus(StatusType.Frozen);
                cleared = true;
            }

            if (HasStatus(target, StatusType.Immobilized))
            {
                target.RemoveStatus(StatusType.Immobilized);
                cleared = true;
            }

            if (cleared)
                state.Emit(GameEventType.EffectResolved, target.ControllerId, "ClearCrowdControl:" + target.InstanceId, target.InstanceId);
        }

        internal static int PowerBonus(GameState state, ICardCatalog catalog, CardInstance card, bool attacking, bool heroTarget)
        {
            if (!state.Rules.TacticalEnabled || !attacking || heroTarget || state.Combat == null) return 0;
            var bonus = 0;
            var defender = state.Cards[state.Combat.Defender];
            if (!card.AttachedEquipmentId.IsNone
                && Program(catalog, state.Cards[card.AttachedEquipmentId]) == EffectOpcode.EquipmentAttackRearguard
                && defender.Zone == CardZone.Rearguard)
                bonus++;
            if (catalog.TryGet(defender.DefinitionId, out var defenderDefinition))
                bonus += ExposedValue(defenderDefinition);
            return bonus;
        }

        internal static void CollectTriggers(GameState state, ICardCatalog catalog, int from)
        {
            if (!state.Rules.TacticalEnabled || state.Result.IsFinished) return;
            var events = state.Events.Skip(from).ToArray();
            for (var i = 0; i < events.Length; i++)
            {
                var e = events[i];
                if (e.Type != GameEventType.CharacterPlayed || !state.TryGetCard(e.SourceInstanceId, out var played) || !played.HasBoardSlot)
                    continue;
                ApplyHiddenOnPlay(state, catalog, played);
                var op = Program(catalog, played);
                if (op == EffectOpcode.DeployFreeze)
                {
                    state.Emit(GameEventType.TriggerQueued, played.ControllerId, "DeployFreeze", played.InstanceId);
                    state.EffectQueue.Add(new EffectStep(played.ControllerId, played.InstanceId, EffectStepKind.FreezeTarget, 3));
                }
                else if (op == EffectOpcode.DeployClearCrowdControl)
                {
                    state.Emit(GameEventType.TriggerQueued, played.ControllerId, "DeployClearCrowdControl", played.InstanceId);
                    state.EffectQueue.Add(new EffectStep(played.ControllerId, played.InstanceId, EffectStepKind.ClearCrowdControl));
                }
                else if (op == EffectOpcode.DeployDetect)
                {
                    state.Emit(GameEventType.TriggerQueued, played.ControllerId, "DeployDetect", played.InstanceId);
                    state.EffectQueue.Add(new EffectStep(played.ControllerId, played.InstanceId, EffectStepKind.DetectTarget));
                }
            }
        }
    }
}
