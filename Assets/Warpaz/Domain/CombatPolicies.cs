namespace Warpaz.Domain
{
    /// <summary>
    /// Sprint 2 vanilla combat policies (ADR 0004). Presentation must not reimplement these.
    /// </summary>
    public static class CombatPolicies
    {
        public static bool CanIntercept(CardInstance card, ICardCatalog catalog, RuleConfig rules)
        {
            if (!rules.IsPreview) return CanIntercept(card);
            if (card == null || card.Zone != CardZone.Rearguard || card.ReadyState != ReadyState.Ready
                || !catalog.TryGet(card.DefinitionId, out var definition)) return false;
            foreach (var keyword in definition.Keywords) if (keyword == "Guardián") return true;
            return false;
        }

        public static bool CanPlayUnique(GameState state, CardDefinition definition, PlayerId actor, CardInstanceId replacement)
        {
            if (!state.Rules.IsPreview || !definition.Unique) return true;
            foreach (var slot in state.GetPlayer(actor).BoardSlots)
                if (slot.IsOccupied && slot.Occupant != replacement && state.Cards[slot.Occupant].DefinitionId == definition.CardId)
                    return false;
            return true;
        }
        /// <summary>P-HeroReach: Hero is a legal target iff defender has zero Vanguard Characters.</summary>
        public static bool CanTargetHero(PlayerState defender)
        {
            return defender != null && CountVanguardOccupants(defender) == 0;
        }

        /// <summary>P-AttackRow: attacker must be Vanguard, Ready, and without Recent.</summary>
        public static bool CanDeclareAttack(CardInstance attacker)
        {
            if (attacker == null || attacker.Zone != CardZone.Vanguard)
            {
                return false;
            }

            if (attacker.ReadyState != ReadyState.Ready)
            {
                return false;
            }

            return !HasRecent(attacker);
        }

        /// <summary>P-HeroPower: missing/null Power is treated as 0.</summary>
        public static int EffectivePower(CardDefinition definition)
        {
            if (definition == null || !definition.Power.HasValue)
            {
                return 0;
            }

            return definition.Power.Value;
        }

        public static CharacterCombatOutcome CompareCharacterPower(int attackerPower, int defenderPower)
        {
            if (attackerPower > defenderPower)
            {
                return CharacterCombatOutcome.AttackerWins;
            }

            if (attackerPower < defenderPower)
            {
                return CharacterCombatOutcome.DefenderWins;
            }

            return CharacterCombatOutcome.Tie;
        }

        /// <summary>P-HeroWin / P-HeroLose / P-HeroTie.</summary>
        public static HeroCombatOutcome CompareHeroPower(int attackerPower, int heroPower)
        {
            if (attackerPower > heroPower)
            {
                return HeroCombatOutcome.AttackerWins;
            }

            if (attackerPower < heroPower)
            {
                return HeroCombatOutcome.HeroWins;
            }

            return HeroCombatOutcome.Tie;
        }

        /// <summary>P-Interceptors: Ready Vanguard defenders; Recent may intercept.</summary>
        public static bool CanIntercept(CardInstance candidate)
        {
            return candidate != null
                && candidate.Zone == CardZone.Vanguard
                && candidate.ReadyState == ReadyState.Ready;
        }

        public static int CountVanguardOccupants(PlayerState player)
        {
            var count = 0;
            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var slot = player.BoardSlots[i];
                if (slot.IsVanguard && slot.IsOccupied)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountBoardOccupants(PlayerState player)
        {
            var count = 0;
            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                if (player.BoardSlots[i].IsOccupied)
                {
                    count++;
                }
            }

            return count;
        }

        public static bool HasRecent(CardInstance card)
        {
            if (card == null)
            {
                return false;
            }

            var statuses = card.Statuses;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (statuses[i] == StatusType.Recent)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public enum CharacterCombatOutcome
    {
        AttackerWins = 0,
        DefenderWins = 1,
        Tie = 2
    }

    public enum HeroCombatOutcome
    {
        AttackerWins = 0,
        HeroWins = 1,
        Tie = 2
    }
}
