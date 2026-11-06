using System.Linq;

namespace Warpaz.Domain
{
    /// <summary>v0.5 continuous effects. Evaluated from the live board, never cached into printed stats.</summary>
    public static class ExpandedEffects
    {
        public static bool Supports(CardDefinition definition)
        {
            if (definition == null || definition.Effects.Count != 1) return false;
            var effect = definition.Effects[0];
            if (effect == null || effect.Amount != 0 || effect.Target != EffectTarget.AnyCharacter) return false;
            if (definition.Type == CardType.Character)
                return effect.Opcode >= EffectOpcode.DefenseEquipped && effect.Opcode <= EffectOpcode.AttackCostSix;
            if (definition.Type == CardType.Equipment) return effect.Opcode == EffectOpcode.EquipmentArmor;
            return definition.Type == CardType.Reaction && (effect.Opcode == EffectOpcode.ReactionCounter || effect.Opcode == EffectOpcode.ReactionEvade);
        }
        internal static EffectOpcode? Program(ICardCatalog catalog, CardInstance card)
            => catalog.TryGet(card.DefinitionId, out var d) && Supports(d) ? d.Effects[0].Opcode : (EffectOpcode?)null;

        /// <summary>Movement effects must check this before paying/mutating; K.O. and sacrifice are not movement effects.</summary>
        public static bool CanMoveByEffect(GameState state, ICardCatalog catalog, CardInstance card, PlayerId effectController)
        {
            if (card == null || !card.HasBoardSlot || !effectController.IsSeated) return false;
            if (!state.Rules.ExpandedEnabled || effectController == card.ControllerId) return true;
            var op = Program(catalog, card);
            return op != EffectOpcode.DefenseAndImmovable
                && !(op == EffectOpcode.ImmovableVanguard && card.Zone == CardZone.Vanguard)
                && (card.AttachedEquipmentId.IsNone || Program(catalog, state.Cards[card.AttachedEquipmentId]) != EffectOpcode.EquipmentArmor);
        }

        internal static int PowerBonus(GameState state, ICardCatalog catalog, CardInstance card, bool attacking, bool heroTarget)
        {
            if (!state.Rules.ExpandedEnabled || !card.HasBoardSlot) return 0;
            catalog.TryGet(card.DefinitionId, out var definition);
            CardDefinition opponent = null;
            if (state.Combat != null && card.InstanceId == (attacking ? state.Combat.Attacker : state.Combat.Defender))
            {
                var opponentId = attacking ? state.Combat.Defender : state.Combat.Attacker;
                catalog.TryGet(state.Cards[opponentId].DefinitionId, out opponent);
            }
            var allies = state.Cards.Values.Where(c => c.HasBoardSlot && c.ControllerId == card.ControllerId).OrderBy(c => c.InstanceId.Value).ToArray();
            var bonus = 0;
            switch (Program(catalog, card))
            {
                case EffectOpcode.DefenseEquipped: if (!attacking && !card.AttachedEquipmentId.IsNone) bonus++; break;
                case EffectOpcode.PowerOtherGnollVanguard:
                    if (allies.Any(c => c.InstanceId != card.InstanceId && c.Zone == CardZone.Vanguard && catalog.TryGet(c.DefinitionId, out var d) && d.Race == "Gnoll")) bonus++; break;
                case EffectOpcode.DefenseAgainstHigherPrinted:
                    if (!attacking && opponent != null && CombatPolicies.EffectivePower(opponent) > CombatPolicies.EffectivePower(definition)) bonus++; break;
                case EffectOpcode.AttackEquipped: if (attacking && !card.AttachedEquipmentId.IsNone) bonus++; break;
                case EffectOpcode.PowerLoneVanguard:
                    if (card.Zone == CardZone.Vanguard && allies.Count(c => c.Zone == CardZone.Vanguard) == 1) bonus++; break;
                case EffectOpcode.AttackCostFive: if (attacking && !heroTarget && opponent?.CostPa >= 5) bonus++; break;
                case EffectOpcode.PowerOwnCostFive:
                    if (allies.Any(c => catalog.TryGet(c.DefinitionId, out var d) && d.CostPa >= 5)) bonus++; break;
                case EffectOpcode.AttackLowerCost: if (attacking && !heroTarget && opponent?.CostPa < definition.CostPa) bonus++; break;
                case EffectOpcode.DefenseAndImmovable: if (!attacking) bonus++; break;
                case EffectOpcode.AttackCostSix: if (attacking && !heroTarget && opponent?.CostPa >= 6) bonus += 2; break;
            }
            foreach (var ally in allies)
            {
                if (ally.InstanceId == card.InstanceId) continue;
                var aura = Program(catalog, ally);
                if (aura == EffectOpcode.AuraOtherGnollOpponentTurn && definition.Race == "Gnoll" && state.ActivePlayerId != card.ControllerId) bonus++;
                if (aura == EffectOpcode.AuraOtherVanguardDefense && !attacking && card.Zone == CardZone.Vanguard) bonus++;
            }
            if (!attacking && !card.AttachedEquipmentId.IsNone && Program(catalog, state.Cards[card.AttachedEquipmentId]) == EffectOpcode.EquipmentArmor) bonus += 2;
            return bonus;
        }
    }
}
