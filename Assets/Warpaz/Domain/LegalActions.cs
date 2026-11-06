using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Legal Sprint 2 actions for the current phase. Application only forwards this list.
    /// </summary>
    public static class LegalActions
    {
        public static IReadOnlyList<LegalAction> For(GameState state)
        {
            return For(state, null);
        }

        public static IReadOnlyList<LegalAction> For(GameState state, ICardCatalog catalog)
        {
            if (state == null)
            {
                return Array.Empty<LegalAction>();
            }

            switch (state.Phase)
            {
                case MatchPhase.MulliganP0:
                    return MulliganFor(PlayerId.Player0);
                case MatchPhase.MulliganP1:
                    return MulliganFor(PlayerId.Player1);
                case MatchPhase.Main:
                    return MainFor(state, catalog);
                case MatchPhase.PendingChoice:
                    if (state.PendingDecision.Kind == PendingDecisionKind.EffectChoice)
                    {
                        var choices = new List<LegalAction>();
                        foreach (var id in state.PendingDecision.ChoiceIds)
                            choices.Add(LegalAction.ChooseInterceptor(state.PriorityPlayerId, id, "EffectChoice"));
                        return choices;
                    }
                    if (state.PendingDecision.Kind == PendingDecisionKind.Reaction)
                    {
                        var reactions = new List<LegalAction> { LegalAction.DeclineInterception(state.PriorityPlayerId, "ResolveCombat") };
                        if (catalog != null)
                            foreach (var id in state.GetPlayer(state.PriorityPlayerId).Hand)
                            {
                                if (AdvancedEffects.CanReact(state, catalog, state.PriorityPlayerId, id))
                                    reactions.Add(LegalAction.CardIntent(state.PriorityPlayerId, CommandType.React, id, CardInstanceId.None, "React"));
                                if (AdvancedEffects.CanRespond(state, catalog, state.PriorityPlayerId, id))
                                    reactions.Add(LegalAction.CardIntent(state.PriorityPlayerId, CommandType.Respond, id, CardInstanceId.None, "Respond"));
                            }
                        return reactions;
                    }
                    return PendingInterceptFor(state);
                default:
                    return Array.Empty<LegalAction>();
            }
        }

        private static IReadOnlyList<LegalAction> MulliganFor(PlayerId actorId)
        {
            return new[]
            {
                LegalAction.ConfirmMulligan(actorId, MulliganDecision.KeepHand, "KeepHand"),
                LegalAction.ConfirmMulligan(actorId, MulliganDecision.ReplaceWholeHand, "ReplaceWholeHand")
            };
        }

        private static IReadOnlyList<LegalAction> MainFor(GameState state, ICardCatalog catalog)
        {
            var actor = state.ActivePlayerId;
            var list = new List<LegalAction>
            {
                LegalAction.EndTurn(actor, "EndTurn")
            };

            if (catalog != null)
            {
                AppendPlays(list, state, catalog, actor);
            }

            AppendReorganizations(list, state, actor);
            AppendAttacks(list, state, actor);
            return list;
        }

        private static void AppendPlays(
            List<LegalAction> list,
            GameState state,
            ICardCatalog catalog,
            PlayerId actor)
        {
            var player = state.GetPlayer(actor);
            var boardFull = CombatPolicies.CountBoardOccupants(player) >= BoardSlot.TotalCharacterSlots;

            for (var h = 0; h < player.Hand.Count; h++)
            {
                var instanceId = player.Hand[h];
                var card = state.Cards[instanceId];
                if (state.Rules.AdvancedEnabled && catalog.TryGet(card.DefinitionId, out var advanced) && advanced.Type != CardType.Character
                    && (AdvancedEffects.Supports(advanced) || state.Rules.ExpandedEnabled && ExpandedEffects.Supports(advanced)))
                {
                    var targets = new List<CardInstanceId> { CardInstanceId.None };
                    targets.AddRange(System.Linq.Enumerable.Select(System.Linq.Enumerable.OrderBy(state.Cards.Values, c => c.InstanceId.Value), c => c.InstanceId));
                    foreach (var target in targets)
                        if (AdvancedEffects.CanPlay(state, catalog, actor, instanceId, target))
                            list.Add(LegalAction.CardIntent(actor, advanced.Type == CardType.Equipment ? CommandType.Equip : CommandType.Play, instanceId, target, "AdvancedPlay"));
                    continue;
                }
                if (catalog.TryGet(card.DefinitionId, out var action) && action.Type == CardType.Action)
                {
                    if ((action.CostPa ?? 0) >= 0 && player.PaCurrent >= (action.CostPa ?? 0))
                        foreach (var target in System.Linq.Enumerable.OrderBy(state.Cards.Values, c => c.InstanceId.Value))
                            if (EffectEngine.CanTarget(state, action, actor, target.InstanceId))
                                list.Add(LegalAction.PlayReplace(actor, instanceId, target.InstanceId, "PlayAction"));
                    continue;
                }
                if (!catalog.TryGet(card.DefinitionId, out var definition)
                    || definition.Type != CardType.Character
                    || !CharacterPositions.TryParse(definition.Position, out var position))
                {
                    continue;
                }

                var cost = definition.CostPa ?? 0;
                if (player.PaCurrent < cost)
                {
                    continue;
                }

                if (boardFull)
                {
                    for (var s = 0; s < player.BoardSlots.Count; s++)
                    {
                        var slot = player.BoardSlots[s];
                        if (!slot.IsOccupied || !CharacterPositions.AllowsRow(position, slot.Row))
                        {
                            continue;
                        }

                        if (!CombatPolicies.CanPlayUnique(state, definition, actor, slot.Occupant)) continue;
                        list.Add(LegalAction.PlayReplace(
                            actor,
                            instanceId,
                            slot.Occupant,
                            "PlayReplace"));
                    }
                }
                else
                {
                    if (CombatPolicies.CanPlayUnique(state, definition, actor, CardInstanceId.None))
                        AppendEmptySlotPlays(list, player, actor, instanceId, position);
                }
            }
        }

        private static void AppendEmptySlotPlays(
            List<LegalAction> list,
            PlayerState player,
            PlayerId actor,
            CardInstanceId instanceId,
            CharacterPosition position)
        {
            for (var s = 0; s < player.BoardSlots.Count; s++)
            {
                var slot = player.BoardSlots[s];
                if (slot.IsOccupied || !CharacterPositions.AllowsRow(position, slot.Row))
                {
                    continue;
                }

                list.Add(LegalAction.Play(actor, instanceId, slot.Row, slot.Index, "Play"));
            }
        }

        private static void AppendReorganizations(List<LegalAction> list, GameState state, PlayerId actor)
        {
            var player = state.GetPlayer(actor);
            if (player.FreeReorganizationUsed)
            {
                return;
            }

            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var slot = player.BoardSlots[i];
                if (!slot.IsOccupied)
                {
                    continue;
                }

                var otherRow = slot.Row == BoardRow.Vanguard ? BoardRow.Rearguard : BoardRow.Vanguard;
                for (var index = 0; index < BoardSlot.SlotsPerRow; index++)
                {
                    var dest = GetSlot(player, otherRow, index);
                    if (!dest.IsOccupied)
                    {
                        list.Add(LegalAction.ReorganizeMove(
                            actor,
                            slot.Occupant,
                            otherRow,
                            index,
                            "ReorganizeMove"));
                    }
                    else if (slot.IsVanguard)
                    {
                        list.Add(LegalAction.ReorganizeSwap(
                            actor,
                            slot.Occupant,
                            dest.Occupant,
                            "ReorganizeSwap"));
                    }
                }
            }
        }

        private static void AppendAttacks(List<LegalAction> list, GameState state, PlayerId actor)
        {
            var attackerPlayer = state.GetPlayer(actor);
            var defender = state.GetPlayer(actor.Opponent);

            for (var i = 0; i < attackerPlayer.BoardSlots.Count; i++)
            {
                var slot = attackerPlayer.BoardSlots[i];
                if (!slot.IsOccupied)
                {
                    continue;
                }

                var attacker = state.Cards[slot.Occupant];
                if (!CombatPolicies.CanDeclareAttack(attacker))
                {
                    continue;
                }

                for (var t = 0; t < defender.BoardSlots.Count; t++)
                {
                    var targetSlot = defender.BoardSlots[t];
                    if (targetSlot.IsVanguard && targetSlot.IsOccupied)
                    {
                        list.Add(LegalAction.Attack(
                            actor,
                            attacker.InstanceId,
                            targetSlot.Occupant,
                            "Attack"));
                    }
                }

                if (CombatPolicies.CanTargetHero(defender))
                {
                    list.Add(LegalAction.AttackHero(actor, attacker.InstanceId, "AttackHero"));
                }
            }
        }

        private static IReadOnlyList<LegalAction> PendingInterceptFor(GameState state)
        {
            var pending = state.PendingDecision;
            if (pending.Kind != PendingDecisionKind.ChooseInterceptor)
            {
                return Array.Empty<LegalAction>();
            }

            var list = new List<LegalAction>(pending.LegalInterceptorIds.Count + 1);
            for (var i = 0; i < pending.LegalInterceptorIds.Count; i++)
            {
                list.Add(LegalAction.ChooseInterceptor(
                    pending.ActorId,
                    pending.LegalInterceptorIds[i],
                    "ChooseInterceptor"));
            }

            list.Add(LegalAction.DeclineInterception(pending.ActorId, "DeclineInterception"));
            return list;
        }

        private static BoardSlot GetSlot(PlayerState player, BoardRow row, int index)
        {
            var offset = row == BoardRow.Vanguard ? 0 : BoardSlot.SlotsPerRow;
            return player.BoardSlots[offset + index];
        }
    }
}
