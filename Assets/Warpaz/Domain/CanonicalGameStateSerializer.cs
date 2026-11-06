using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Warpaz.Domain
{
    /// <summary>
    /// Culture-invariant UTF-8 document of a <see cref="GameState"/>.
    /// Dictionary iteration order, dates, paths, and object identities are not used.
    /// Newlines are always LF so the hash is the same on every OS.
    /// </summary>
    public static class CanonicalGameStateSerializer
    {
        public static string ToString(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var sb = new StringBuilder();
            AppendHeader(sb, state);
            AppendMatch(sb, state);
            AppendPlayers(sb, state);
            AppendCardsSorted(sb, state);
            if (state.Rules.EffectsEnabled)
                foreach (var card in System.Linq.Enumerable.OrderBy(state.Cards.Values, c => c.InstanceId.Value))
                    Write(sb, "effect." + Int(card.InstanceId.Value), Int(card.TurnPowerModifier) + "," + (card.CannotInterceptThisTurn ? "1" : "0") + "," + (card.CannotReadyThisTurn ? "1" : "0"));
            AppendPending(sb, state);
            AppendEvents(sb, state);
            return sb.ToString();
        }

        public static byte[] ToUtf8Bytes(GameState state)
        {
            return Encoding.UTF8.GetBytes(ToString(state));
        }

        private static void AppendHeader(StringBuilder sb, GameState state)
        {
            Write(sb, "schemaVersion", state.SchemaVersion);
            Write(sb, "contentVersion", state.ContentVersion);
            Write(sb, "ruleVersion", state.RuleVersion);
            Write(sb, "seed", state.Seed);
            Write(sb, "eventIndex", state.EventIndex);
        }

        private static void AppendMatch(StringBuilder sb, GameState state)
        {
            Write(sb, "turn", state.Turn);
            Write(sb, "phase", EnumName(state.Phase));
            Write(sb, "activePlayerId", state.ActivePlayerId.Value);
            Write(sb, "priorityPlayerId", state.PriorityPlayerId.Value);
            Write(sb, "firstPlayerId", state.FirstPlayerId.Value);
            Write(sb, "resultFinished", state.Result.IsFinished ? 1 : 0);
            Write(sb, "resultCause", EnumName(state.Result.Cause));
            Write(sb, "resultHasWinner", state.Result.HasWinner ? 1 : 0);
            Write(sb, "resultWinner", state.Result.Winner.Value);
            Write(sb, "pendingKind", EnumName(state.PendingDecision.Kind));
            Write(sb, "pendingActor", state.PendingDecision.ActorId.Value);
            Write(sb, "nextInstanceId", state.NextInstanceId);
            Write(sb, "rule.initialHandSize", state.Rules.InitialHandSize);
            Write(sb, "rule.initialResolution", state.Rules.InitialResolution);
            Write(sb, "rule.deckSize", state.Rules.DeckSize);
            Write(sb, "rule.paLimit", state.Rules.PaLimit);
            Write(sb, "rule.initialPaMax", state.Rules.InitialPaMax);
            Write(sb, "rule.paIncreasePerTurn", state.Rules.PaIncreasePerTurn);
        }

        private static void AppendPlayers(StringBuilder sb, GameState state)
        {
            for (var p = 0; p < 2; p++)
            {
                var player = state.Players[p];
                var prefix = "P" + Int(p) + ".";
                Write(sb, prefix + "playerId", player.PlayerId.Value);
                Write(sb, prefix + "heroId", player.HeroId.Value);
                Write(sb, prefix + "classPathId", player.ClassPathId.Value);
                Write(sb, prefix + "heroInstanceId", player.HeroInstanceId.Value);
                Write(sb, prefix + "classInstanceId", player.ClassInstanceId.Value);
                Write(sb, prefix + "ascended", player.Ascended ? 1 : 0);
                Write(sb, prefix + "freeReorganizationUsed", player.FreeReorganizationUsed ? 1 : 0);
                Write(sb, prefix + "resolution", player.Resolution);
                Write(sb, prefix + "paCurrent", player.PaCurrent);
                Write(sb, prefix + "paMax", player.PaMax);
                WriteIds(sb, prefix + "deck", player.Deck);
                WriteIds(sb, prefix + "hand", player.Hand);
                WriteIds(sb, prefix + "discard", player.Discard);
                for (var s = 0; s < player.BoardSlots.Count; s++)
                {
                    var slot = player.BoardSlots[s];
                    Write(
                        sb,
                        prefix + "slot" + Int(s),
                        EnumName(slot.Row) + "," + Int(slot.Index) + "," + Int(slot.Occupant.Value));
                }
            }
        }

        private static void AppendCardsSorted(StringBuilder sb, GameState state)
        {
            var ids = new CardInstanceId[state.Cards.Count];
            var n = 0;
            foreach (var id in state.Cards.Keys)
            {
                ids[n] = id;
                n++;
            }

            Array.Sort(ids, CompareInstanceIds);

            Write(sb, "cardCount", ids.Length);
            for (var i = 0; i < ids.Length; i++)
            {
                var card = state.Cards[ids[i]];
                Write(sb, "card", FormatCard(card));
            }
        }

        private static string FormatCard(CardInstance card)
        {
            var statuses = CopySortedStatuses(card.Statuses);
            var statusText = new StringBuilder();
            for (var i = 0; i < statuses.Length; i++)
            {
                if (i > 0)
                {
                    statusText.Append(',');
                }

                statusText.Append(EnumName(statuses[i]));
            }

            return Int(card.InstanceId.Value)
                + "," + card.DefinitionId.Value
                + "," + Int(card.OwnerId.Value)
                + "," + Int(card.ControllerId.Value)
                + "," + EnumName(card.Zone)
                + "," + EnumName(card.ReadyState)
                + "," + EnumName(card.Slot.Row)
                + "," + Int(card.Slot.Index)
                + "," + Int(card.Slot.Occupant.Value)
                + "," + Int(card.AttachedEquipmentId.Value)
                + "," + statusText;
        }

        private static void AppendPending(StringBuilder sb, GameState state)
        {
            var pending = state.PendingDecision;
            if (pending.Kind == PendingDecisionKind.ChooseInterceptor)
            {
                Write(sb, "pendingAttacker", pending.AttackerId.Value);
                Write(sb, "pendingDeclaredTarget", pending.DeclaredTargetId.Value);
                Write(sb, "pendingTargetsHero", pending.TargetsHero ? 1 : 0);
                var interceptorIds = new CardInstanceId[pending.LegalInterceptorIds.Count];
                for (var i = 0; i < pending.LegalInterceptorIds.Count; i++)
                {
                    interceptorIds[i] = pending.LegalInterceptorIds[i];
                }

                Array.Sort(interceptorIds, CompareInstanceIds);
                WriteIds(sb, "pendingInterceptors", new List<CardInstanceId>(interceptorIds));
            }

            var effects = new PendingEffect[state.PendingEffects.Count];
            for (var i = 0; i < state.PendingEffects.Count; i++)
            {
                effects[i] = state.PendingEffects[i];
            }

            Array.Sort(effects, ComparePendingEffects);
            Write(sb, "pendingEffectCount", effects.Length);
            for (var i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                Write(
                    sb,
                    "pendingEffect",
                    Int(effect.Sequence) + "," + Int(effect.SourceInstanceId.Value));
            }
        }

        private static void AppendEvents(StringBuilder sb, GameState state)
        {
            Write(sb, "eventCount", state.Events.Count);
            for (var i = 0; i < state.Events.Count; i++)
            {
                var evt = state.Events[i];
                Write(
                    sb,
                    "event",
                    Int(evt.EventIndex)
                    + "," + EnumName(evt.Type)
                    + "," + Int(evt.ActorId.Value)
                    + "," + Escape(evt.Payload)
                    + "," + Int(evt.SourceInstanceId.Value)
                    + "," + Int(evt.ParentEventIndex)
                    + "," + EnumName(evt.Visibility));
            }
        }

        private static StatusType[] CopySortedStatuses(IReadOnlyList<StatusType> statuses)
        {
            var copy = new StatusType[statuses.Count];
            for (var i = 0; i < statuses.Count; i++)
            {
                copy[i] = statuses[i];
            }

            Array.Sort(copy);
            return copy;
        }

        private static int CompareInstanceIds(CardInstanceId a, CardInstanceId b)
        {
            return a.Value.CompareTo(b.Value);
        }

        private static int ComparePendingEffects(PendingEffect a, PendingEffect b)
        {
            var bySequence = a.Sequence.CompareTo(b.Sequence);
            if (bySequence != 0)
            {
                return bySequence;
            }

            return a.SourceInstanceId.Value.CompareTo(b.SourceInstanceId.Value);
        }

        private static void WriteIds(StringBuilder sb, string key, List<CardInstanceId> ids)
        {
            var value = new StringBuilder();
            for (var i = 0; i < ids.Count; i++)
            {
                if (i > 0)
                {
                    value.Append(',');
                }

                value.Append(Int(ids[i].Value));
            }

            Write(sb, key, value.ToString());
        }

        private static void Write(StringBuilder sb, string key, string value)
        {
            sb.Append(key).Append('=').Append(value ?? string.Empty).Append('\n');
        }

        private static void Write(StringBuilder sb, string key, int value)
        {
            Write(sb, key, Int(value));
        }

        private static void Write(StringBuilder sb, string key, ulong value)
        {
            Write(sb, key, value.ToString(CultureInfo.InvariantCulture));
        }

        private static string Int(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string EnumName<T>(T value) where T : struct, Enum
        {
            return value.ToString();
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace(",", "\\,");
        }
    }
}
