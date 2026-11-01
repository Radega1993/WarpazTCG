using System.Globalization;

namespace Warpaz.Domain
{
    /// <summary>Shared combat resolution for attack and intercept paths.</summary>
    internal static class CombatResolver
    {
        internal static void ResolveCharacterCombat(
            GameState state,
            ICardCatalog catalog,
            CardInstance attacker,
            CardInstance defender)
        {
            var attackerPower = PowerOf(catalog, attacker);
            var defenderPower = PowerOf(catalog, defender);
            var outcome = CombatPolicies.CompareCharacterPower(attackerPower, defenderPower);

            state.Emit(
                GameEventType.CombatResolved,
                attacker.ControllerId,
                "Character:"
                + outcome
                + ":"
                + attacker.InstanceId
                + ":"
                + defender.InstanceId
                + ":"
                + attackerPower.ToString(CultureInfo.InvariantCulture)
                + ":"
                + defenderPower.ToString(CultureInfo.InvariantCulture),
                attacker.InstanceId);

            if (outcome == CharacterCombatOutcome.AttackerWins || outcome == CharacterCombatOutcome.Tie)
            {
                EngineActions.KnockOutToDiscard(state, state.GetPlayer(defender.ControllerId), defender.InstanceId);
            }

            if (outcome == CharacterCombatOutcome.DefenderWins || outcome == CharacterCombatOutcome.Tie)
            {
                EngineActions.KnockOutToDiscard(state, state.GetPlayer(attacker.ControllerId), attacker.InstanceId);
            }
        }

        internal static void ResolveHeroCombat(
            GameState state,
            ICardCatalog catalog,
            CardInstance attacker,
            PlayerState defender)
        {
            var attackerPower = PowerOf(catalog, attacker);
            var hero = state.Cards[defender.HeroInstanceId];
            var heroPower = PowerOf(catalog, hero);
            var outcome = CombatPolicies.CompareHeroPower(attackerPower, heroPower);

            state.Emit(
                GameEventType.CombatResolved,
                attacker.ControllerId,
                "Hero:"
                + outcome
                + ":"
                + attacker.InstanceId
                + ":"
                + defender.PlayerId
                + ":"
                + attackerPower.ToString(CultureInfo.InvariantCulture)
                + ":"
                + heroPower.ToString(CultureInfo.InvariantCulture),
                attacker.InstanceId);

            if (outcome == HeroCombatOutcome.AttackerWins || outcome == HeroCombatOutcome.Tie)
            {
                LoseResolution(state, defender, 1);
            }

            if (outcome == HeroCombatOutcome.HeroWins || outcome == HeroCombatOutcome.Tie)
            {
                if (state.Cards.ContainsKey(attacker.InstanceId) && attacker.HasBoardSlot)
                {
                    EngineActions.KnockOutToDiscard(
                        state,
                        state.GetPlayer(attacker.ControllerId),
                        attacker.InstanceId);
                }
            }

            if (defender.Resolution == 0 && !state.Result.IsFinished)
            {
                TurnCycle.TryEndMatch(state, attacker.ControllerId, MatchEndCause.ResolutionDepleted);
            }
        }

        private static void LoseResolution(GameState state, PlayerState player, int amount)
        {
            var next = player.Resolution - amount;
            if (next < 0)
            {
                next = 0;
            }

            var lost = player.Resolution - next;
            player.Resolution = next;
            if (lost > 0)
            {
                state.Emit(
                    GameEventType.ResolutionRemoved,
                    player.PlayerId,
                    lost.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static int PowerOf(ICardCatalog catalog, CardInstance card)
        {
            if (!catalog.TryGet(card.DefinitionId, out var definition))
            {
                throw new InvariantViolationException("Missing definition " + card.DefinitionId + ".");
            }

            return CombatPolicies.EffectivePower(definition);
        }
    }
}
