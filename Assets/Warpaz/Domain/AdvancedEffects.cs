using System;
using System.Collections.Generic;
using System.Linq;

namespace Warpaz.Domain
{
    public sealed class CombatWindow
    {
        internal CombatWindow(CardInstanceId attacker, CardInstanceId defender, bool hero)
        { Attacker = attacker; Defender = defender; TargetsHero = hero; }
        public CardInstanceId Attacker { get; }
        public CardInstanceId Defender { get; }
        public bool TargetsHero { get; }
        public int DefenseBonus { get; internal set; }
        public int DrawOnDefenderKo { get; internal set; }
        public int AttackBonus { get; internal set; }
        public int ConsecutivePasses { get; internal set; }
        public int PendingReactionCount => Stack.Count;
        internal int CounterPenalty { get; set; }
        internal List<CardInstanceId> EvadeSources { get; } = new List<CardInstanceId>();
        internal bool[] ResponseUsed { get; } = new bool[2];
        internal bool[] ReactionUsed { get; } = new bool[2];
        internal List<CardInstanceId> Stack { get; } = new List<CardInstanceId>();
    }

    public enum EffectStepKind { Draw, Discard, BottomHand, OrderTop, LookBottom, ReturnCharacter, ReturnEquipment, EvadeToRear }
    internal sealed class EffectStep
    {
        internal EffectStep(PlayerId actor, CardInstanceId source, EffectStepKind kind, int amount = 1, CardInstanceId target = default)
        { Actor = actor; Source = source; Kind = kind; Amount = amount; Target = target; }
        internal PlayerId Actor { get; }
        internal CardInstanceId Source { get; }
        internal EffectStepKind Kind { get; }
        internal int Amount { get; }
        internal CardInstanceId Target { get; }
        internal List<CardInstanceId> Remaining { get; } = new List<CardInstanceId>();
        internal List<CardInstanceId> Ordered { get; } = new List<CardInstanceId>();
        internal bool Initialized { get; set; }
    }

    /// <summary>Versioned, bounded continuations. No callbacks or UI state enter the match.</summary>
    public static class AdvancedEffects
    {
        public static bool Supports(CardDefinition d)
        {
            if (d == null || d.Effects.Count != 1 || d.Effects[0] == null || d.Effects[0].Amount != 0
                || d.Effects[0].Target != EffectTarget.AnyCharacter) return false;
            var op = d.Effects[0].Opcode;
            switch (d.Type)
            {
                case CardType.Action: return op == EffectOpcode.SacrificeDrawDiscard || op == EffectOpcode.OrderDrawBottom;
                case CardType.Equipment: return op == EffectOpcode.EquipmentAttackCharacter || op == EffectOpcode.EquipmentDefense || op == EffectOpcode.EquipmentHighestCostAttack;
                case CardType.Reaction: return op >= EffectOpcode.ReactionDefense && op <= EffectOpcode.ReactionUnderdog;
                case CardType.Character: return op >= EffectOpcode.DeployLookBottom && op <= EffectOpcode.InterceptPower;
                default: return false;
            }
        }
        private static EffectOpcode? Program(ICardCatalog catalog, CardInstance card)
            => catalog.TryGet(card.DefinitionId, out var d) && Supports(d) ? d.Effects[0].Opcode : (EffectOpcode?)null;

        public static bool CanPlay(GameState s, ICardCatalog catalog, PlayerId actor, CardInstanceId sourceId, CardInstanceId targetId)
        {
            if (!s.Rules.AdvancedEnabled || s.Phase != MatchPhase.Main || actor != s.ActivePlayerId
                || !s.TryGetCard(sourceId, out var source) || source.OwnerId != actor || source.Zone != CardZone.Hand
                || !catalog.TryGet(source.DefinitionId, out var d) || !(Supports(d) || s.Rules.ExpandedEnabled && ExpandedEffects.Supports(d)) || (d.CostPa ?? 0) < 0
                || s.GetPlayer(actor).PaCurrent < (d.CostPa ?? 0)) return false;
            if (d.Type == CardType.Action && d.Effects[0].Opcode == EffectOpcode.OrderDrawBottom) return targetId.IsNone;
            return (d.Type == CardType.Equipment && s.Rules.MaximumEquipmentPerCharacter > 0 || d.Type == CardType.Action)
                && s.TryGetCard(targetId, out var target) && target.HasBoardSlot && target.ControllerId == actor;
        }
        internal static CommandResult Play(GameState s, Command c, ICardCatalog catalog)
        {
            if (!CanPlay(s, catalog, c.ActorId, c.PrimaryInstanceId, c.SecondaryInstanceId) || c.HasBoardSlotTarget || c.TargetsHero)
                return Reject(s);
            var source = s.Cards[c.PrimaryInstanceId]; catalog.TryGet(source.DefinitionId, out var d);
            if ((d.Type == CardType.Equipment ? CommandType.Equip : CommandType.Play) != c.Type) return Reject(s);
            var from = s.EventIndex; var player = s.GetPlayer(c.ActorId);
            EngineActions.SpendPa(s, player, d.CostPa ?? 0);
            if (d.Type == CardType.Equipment)
            {
                var target = s.Cards[c.SecondaryInstanceId]; Detach(s, target);
                player.Hand.Remove(source.InstanceId); source.SetZone(CardZone.Equipment); target.AttachedEquipmentId = source.InstanceId;
                s.Emit(GameEventType.EquipmentAttached, c.ActorId, source.InstanceId + ":" + target.InstanceId, source.InstanceId);
            }
            else
            {
                EngineActions.Move(s, player, source.InstanceId, CardZone.Discard);
                s.Emit(GameEventType.ActionPlayed, c.ActorId, source.InstanceId.ToString(), source.InstanceId);
                if (d.Effects[0].Opcode == EffectOpcode.SacrificeDrawDiscard)
                {
                    // Sacrifice is not K.O.; equipment still leaves play. Its triggers wait behind this program.
                    Queue(s, c.ActorId, source.InstanceId, EffectStepKind.Draw, 2);
                    Queue(s, c.ActorId, source.InstanceId, EffectStepKind.Discard);
                    EngineActions.ReplaceToDiscard(s, player, c.SecondaryInstanceId);
                }
                else
                {
                    Queue(s, c.ActorId, source.InstanceId, EffectStepKind.OrderTop, 4);
                    Queue(s, c.ActorId, source.InstanceId, EffectStepKind.Draw);
                    Queue(s, c.ActorId, source.InstanceId, EffectStepKind.BottomHand);
                }
            }
            Drain(s, catalog); GameStateInvariants.AssertValid(s); return EngineActions.Snapshot(s, from);
        }

        internal static void Detach(GameState s, CardInstance bearer)
        {
            if (bearer.AttachedEquipmentId.IsNone) return;
            var equip = s.Cards[bearer.AttachedEquipmentId]; bearer.AttachedEquipmentId = CardInstanceId.None;
            equip.SetZone(CardZone.Discard); s.GetPlayer(equip.OwnerId).Discard.Add(equip.InstanceId);
            s.Emit(GameEventType.EquipmentDiscarded, equip.OwnerId, equip.InstanceId.ToString(), equip.InstanceId);
            // Subscribers are captured by the command processor from this public event, in event/instance order.
        }

        internal static void CollectTriggers(GameState s, ICardCatalog catalog, int from, HashSet<CardInstanceId> witnesses)
        {
            if (!s.Rules.AdvancedEnabled || s.Result.IsFinished) return;
            var events = s.Events.Skip(from).ToArray();
            foreach (var e in events)
            {
                if (e.Type == GameEventType.CardMoved && (e.Payload.EndsWith(":Discard", StringComparison.Ordinal) || e.Payload.EndsWith(":Hand", StringComparison.Ordinal))) witnesses.Remove(e.SourceInstanceId);
                if (e.Type == GameEventType.CharacterPlayed && s.TryGetCard(e.SourceInstanceId, out var played) && played.HasBoardSlot)
                {
                    witnesses.Add(played.InstanceId);
                    var op = Program(catalog, played);
                    if (op == EffectOpcode.DeployLookBottom) Trigger(s, played, EffectStepKind.LookBottom);
                    if (op == EffectOpcode.DeployReturnCharacter) Trigger(s, played, EffectStepKind.ReturnCharacter);
                    if (op == EffectOpcode.DeployReturnEquipment) Trigger(s, played, EffectStepKind.ReturnEquipment);
                }
                if (e.Type == GameEventType.EquipmentDiscarded)
                    foreach (var card in s.Cards.Values.OrderBy(x => x.InstanceId.Value))
                        if (witnesses.Contains(card.InstanceId) && Program(catalog, card) == EffectOpcode.EquipmentDiscardLoot && card.LastTriggerTurn != s.Turn)
                        {
                            card.LastTriggerTurn = s.Turn;
                            Trigger(s, card, EffectStepKind.Draw);
                            Queue(s, card.ControllerId, card.InstanceId, EffectStepKind.Discard);
                        }
            }
        }
        private static void Trigger(GameState s, CardInstance card, EffectStepKind kind)
        {
            s.Emit(GameEventType.TriggerQueued, card.ControllerId, kind.ToString(), card.InstanceId);
            Queue(s, card.ControllerId, card.InstanceId, kind);
        }
        private static void Queue(GameState s, PlayerId actor, CardInstanceId source, EffectStepKind kind, int amount = 1)
            => s.EffectQueue.Add(new EffectStep(actor, source, kind, amount));

        internal static void Drain(GameState s, ICardCatalog catalog)
        {
            if (s.Result.IsFinished) { s.EffectQueue.Clear(); s.Combat = null; s.PendingDecision = PendingDecision.None; return; }
            if (s.PendingDecision.IsPending) return;
            while (s.EffectQueue.Count > 0 && !s.Result.IsFinished)
            {
                var step = s.EffectQueue[0]; var p = s.GetPlayer(step.Actor);
                if (step.Kind == EffectStepKind.Draw)
                {
                    s.EffectQueue.RemoveAt(0); Draw(s, step.Actor, step.Amount); continue;
                }
                var choices = new List<CardInstanceId>();
                switch (step.Kind)
                {
                    case EffectStepKind.Discard: case EffectStepKind.BottomHand: choices.AddRange(p.Hand); break;
                    case EffectStepKind.OrderTop:
                        if (!step.Initialized) { step.Remaining.AddRange(p.Deck.Take(step.Amount)); step.Initialized = true; }
                        choices.AddRange(step.Remaining); break;
                    case EffectStepKind.LookBottom:
                        if (p.Deck.Count > 0) { choices.Add(p.Deck[0]); choices.Add(CardInstanceId.None); } break;
                    case EffectStepKind.ReturnCharacter: case EffectStepKind.ReturnEquipment:
                        choices.AddRange(p.Discard.Where(id => catalog.TryGet(s.Cards[id].DefinitionId, out var d)
                            && d.Type == (step.Kind == EffectStepKind.ReturnCharacter ? CardType.Character : CardType.Equipment) && d.CostPa <= 2)); break;
                    case EffectStepKind.EvadeToRear:
                        if (s.TryGetCard(step.Target, out var defender) && defender.Zone == CardZone.Vanguard
                            && defender.ControllerId == step.Actor && p.BoardSlots.Any(slot => slot.IsRearguard && !slot.IsOccupied)
                            && ExpandedEffects.CanMoveByEffect(s, catalog, defender, step.Actor))
                        { choices.Add(step.Target); choices.Add(CardInstanceId.None); }
                        break;
                }
                if (choices.Count == 0) { s.EffectQueue.RemoveAt(0); continue; }
                s.PendingDecision = PendingDecision.Effect(step.Actor, choices); s.PriorityPlayerId = step.Actor;
                EngineActions.SetPhase(s, MatchPhase.PendingChoice);
                s.Emit(GameEventType.DecisionOpened, step.Actor, step.Kind.ToString()); return;
            }
            if (s.Result.IsFinished) { s.EffectQueue.Clear(); s.Combat = null; s.PendingDecision = PendingDecision.None; }
        }
        internal static CommandResult Choose(GameState s, Command c, ICardCatalog catalog)
        {
            if (!s.Rules.AdvancedEnabled || s.Phase != MatchPhase.PendingChoice || s.PendingDecision.Kind != PendingDecisionKind.EffectChoice
                || s.PendingDecision.ActorId != c.ActorId || s.EffectQueue.Count == 0 || !s.PendingDecision.ChoiceIds.Contains(c.PrimaryInstanceId)) return Reject(s);
            var from = s.EventIndex; var step = s.EffectQueue[0]; var p = s.GetPlayer(step.Actor); var id = c.PrimaryInstanceId;
            EngineActions.ClosePendingDecision(s);
            switch (step.Kind)
            {
                case EffectStepKind.Discard: EngineActions.Move(s, p, id, CardZone.Discard); break;
                case EffectStepKind.BottomHand:
                    p.Hand.Remove(id); p.Deck.Add(id); s.Cards[id].SetZone(CardZone.Deck); break;
                case EffectStepKind.LookBottom:
                    if (!id.IsNone) { p.Deck.Remove(id); p.Deck.Add(id); } break;
                case EffectStepKind.OrderTop:
                    step.Remaining.Remove(id); step.Ordered.Add(id);
                    if (step.Remaining.Count == 0)
                    {
                        foreach (var card in step.Ordered) p.Deck.Remove(card);
                        p.Deck.InsertRange(0, step.Ordered);
                    }
                    break;
                case EffectStepKind.ReturnCharacter: case EffectStepKind.ReturnEquipment: EngineActions.Move(s, p, id, CardZone.Hand); break;
                case EffectStepKind.EvadeToRear:
                    if (!id.IsNone)
                    {
                        var slot = p.BoardSlots.First(x => x.IsRearguard && !x.IsOccupied);
                        EngineActions.MoveBoardToEmptySlot(s, p, id, slot.Row, slot.Index);
                    }
                    break;
            }
            s.Emit(GameEventType.EffectResolved, step.Actor, step.Kind + ":" + id, step.Source, -1, GameEventVisibility.OwnerOnly);
            if (step.Kind != EffectStepKind.OrderTop || step.Remaining.Count == 0) s.EffectQueue.RemoveAt(0);
            Drain(s, catalog); GameStateInvariants.AssertValid(s); return EngineActions.Snapshot(s, from);
        }
        internal static void Draw(GameState s, PlayerId actor, int count)
        {
            var p = s.GetPlayer(actor);
            for (var i = 0; i < count && !s.Result.IsFinished; i++)
                if (p.Deck.Count > 0) MatchFactory.DrawCards(s, actor, 1);
                else
                {
                    s.Emit(GameEventType.DrawFailed, actor, "Effect");
                    var lost = Math.Min(p.Resolution, s.Rules.FailedDrawResolutionLoss); p.Resolution -= lost;
                    s.Emit(GameEventType.ResolutionRemoved, actor, lost.ToString());
                    if (p.Resolution == 0) TurnCycle.TryEndMatch(s, actor.Opponent, MatchEndCause.ResolutionDepleted);
                }
        }

        internal static bool OpenCombat(GameState s, ICardCatalog catalog, CardInstance attacker, CardInstance defender, bool hero)
        {
            if (!s.Rules.AdvancedEnabled || s.Combat != null) return false;
            s.Combat = new CombatWindow(attacker.InstanceId, defender.InstanceId, hero);
            // Interception is already locked; bonuses do not survive this combat.
            if (!hero && s.Events.LastOrDefault(e => e.Type == GameEventType.AttackIntercepted || e.Type == GameEventType.AttackDeclared)?.Type == GameEventType.AttackIntercepted
                && Program(catalog, defender) == EffectOpcode.InterceptPower)
            { s.Combat.DefenseBonus++; s.Emit(GameEventType.TriggerQueued, defender.ControllerId, "InterceptPower", defender.InstanceId); }
            s.PendingDecision = PendingDecision.Reaction(defender.ControllerId, attacker.InstanceId, defender.InstanceId, hero);
            s.PriorityPlayerId = defender.ControllerId; EngineActions.SetPhase(s, MatchPhase.PendingChoice);
            s.Emit(GameEventType.DecisionOpened, defender.ControllerId, "Reaction"); return true;
        }
        public static bool CanReact(GameState s, ICardCatalog catalog, PlayerId actor, CardInstanceId id)
        {
            if (!s.Rules.AdvancedEnabled || s.Combat == null || s.PendingDecision.Kind != PendingDecisionKind.Reaction || actor != s.PriorityPlayerId
                || s.Combat.ResponseUsed[actor.Index] || s.Cards[s.Combat.Defender].ControllerId != actor
                || !s.TryGetCard(id, out var card) || card.OwnerId != actor || card.Zone != CardZone.Hand
                || !catalog.TryGet(card.DefinitionId, out var d) || d.Type != CardType.Reaction || !(Supports(d) || s.Rules.ExpandedEnabled && ExpandedEffects.Supports(d))
                || (d.CostPa ?? 0) < 0 || s.GetPlayer(actor).PaCurrent < (d.CostPa ?? 0)) return false;
            return !s.Combat.TargetsHero || (d.Effects[0].Opcode != EffectOpcode.ReactionEscape && d.Effects[0].Opcode != EffectOpcode.ReactionKoDraw && d.Effects[0].Opcode != EffectOpcode.ReactionEvade);
        }
        public static bool CanRespond(GameState s, ICardCatalog catalog, PlayerId actor, CardInstanceId id)
            => s.Rules.AdvancedEnabled && s.Combat != null && s.PendingDecision.Kind == PendingDecisionKind.Reaction
                && actor == s.PriorityPlayerId && !s.Combat.ResponseUsed[actor.Index] && !s.Combat.ReactionUsed[actor.Index]
                && s.TryGetCard(id, out var card) && card.OwnerId == actor && card.Zone == CardZone.Hand
                && catalog.TryGet(card.DefinitionId, out var d) && (d.Response ?? 0) > 0;
        internal static CommandResult Respond(GameState s, Command c, ICardCatalog catalog)
        {
            if (!CanRespond(s, catalog, c.ActorId, c.PrimaryInstanceId)) return Reject(s);
            var from = s.EventIndex; var card = s.Cards[c.PrimaryInstanceId]; catalog.TryGet(card.DefinitionId, out var d);
            EngineActions.Move(s, s.GetPlayer(c.ActorId), card.InstanceId, CardZone.Discard);
            s.Combat.ResponseUsed[c.ActorId.Index] = true;
            if (s.Cards[s.Combat.Attacker].ControllerId == c.ActorId) s.Combat.AttackBonus += d.Response.Value;
            else s.Combat.DefenseBonus += d.Response.Value;
            s.Emit(GameEventType.ResponseUsed, c.ActorId, card.InstanceId.ToString(), card.InstanceId);
            s.Combat.ConsecutivePasses = 0; GivePriority(s, c.ActorId.Opponent); return EngineActions.Snapshot(s, from);
        }
        internal static CommandResult React(GameState s, Command c, ICardCatalog catalog)
        {
            if (!CanReact(s, catalog, c.ActorId, c.PrimaryInstanceId)) return Reject(s);
            var from = s.EventIndex; var source = s.Cards[c.PrimaryInstanceId]; catalog.TryGet(source.DefinitionId, out var d);
            var window = s.Combat; var p = s.GetPlayer(c.ActorId);
            EngineActions.SpendPa(s, p, d.CostPa ?? 0); EngineActions.Move(s, p, source.InstanceId, CardZone.Discard);
            s.Emit(GameEventType.ReactionPlayed, c.ActorId, source.InstanceId.ToString(), source.InstanceId);
            window.ReactionUsed[c.ActorId.Index] = true; window.Stack.Add(source.InstanceId); window.ConsecutivePasses = 0;
            GivePriority(s, c.ActorId.Opponent);
            GameStateInvariants.AssertValid(s); return EngineActions.Snapshot(s, from);
        }
        private static void GivePriority(GameState s, PlayerId actor)
        {
            s.PriorityPlayerId = actor;
            s.PendingDecision = PendingDecision.Reaction(actor, s.Combat.Attacker, s.Combat.Defender, s.Combat.TargetsHero);
        }
        private static void ResolveReaction(GameState s, ICardCatalog catalog, CardInstanceId sourceId)
        {
            var window = s.Combat; var source = s.Cards[sourceId]; var actor = source.OwnerId;
            var defender = s.Cards[window.Defender]; var p = s.GetPlayer(actor); catalog.TryGet(source.DefinitionId, out var d);
            s.Emit(GameEventType.EffectResolved, actor, d.Effects[0].Opcode.ToString(), sourceId);
            switch (d.Effects[0].Opcode)
            {
                case EffectOpcode.ReactionDefense: window.DefenseBonus += 2; break;
                case EffectOpcode.ReactionCounter: window.DefenseBonus += 2; window.CounterPenalty++; break;
                case EffectOpcode.ReactionEvade: window.DefenseBonus += 2; window.EvadeSources.Add(sourceId); break;
                case EffectOpcode.ReactionKoDraw: window.DefenseBonus += 2; window.DrawOnDefenderKo++; break;
                case EffectOpcode.ReactionEquippedDefense: window.DefenseBonus += defender.AttachedEquipmentId.IsNone ? 1 : 3; break;
                case EffectOpcode.ReactionUnderdog:
                    catalog.TryGet(defender.DefinitionId, out var dd); catalog.TryGet(s.Cards[window.Attacker].DefinitionId, out var ad);
                    window.DefenseBonus += CombatPolicies.EffectivePower(dd) < CombatPolicies.EffectivePower(ad) ? 2 : 1; break;
                case EffectOpcode.ReactionEscape:
                    EngineActions.ClearBoardSlot(s, p, defender.Slot); Detach(s, defender);
                    defender.SetZone(CardZone.Hand); defender.RemoveStatus(StatusType.Recent); defender.ReadyState = ReadyState.Ready; p.Hand.Add(defender.InstanceId);
                    s.Emit(GameEventType.CardMoved, actor, defender.InstanceId + ":Hand", defender.InstanceId);
                    EngineActions.ClosePendingDecision(s); s.Combat = null; break;
            }
        }
        internal static CommandResult Pass(GameState s, Command c, ICardCatalog catalog)
        {
            if (!s.Rules.AdvancedEnabled || s.Combat == null || s.PendingDecision.Kind != PendingDecisionKind.Reaction || s.PriorityPlayerId != c.ActorId) return Reject(s);
            var from = s.EventIndex; var window = s.Combat; var attacker = s.Cards[window.Attacker]; var defender = s.Cards[window.Defender];
            window.ConsecutivePasses++;
            if (window.ConsecutivePasses < 2) { GivePriority(s, c.ActorId.Opponent); return EngineActions.Snapshot(s, from); }
            window.ConsecutivePasses = 0;
            if (window.Stack.Count > 0)
            {
                var top = window.Stack[window.Stack.Count - 1]; window.Stack.RemoveAt(window.Stack.Count - 1);
                ResolveReaction(s, catalog, top);
                if (s.Combat != null) GivePriority(s, defender.ControllerId);
                return EngineActions.Snapshot(s, from);
            }
            EngineActions.ClosePendingDecision(s);
            var attackFailed = Power(s, catalog, attacker, true, window.TargetsHero) < Power(s, catalog, defender, false, window.TargetsHero);
            if (window.TargetsHero) CombatResolver.ResolveHeroCombat(s, catalog, attacker, s.GetPlayer(defender.ControllerId));
            else CombatResolver.ResolveCharacterCombat(s, catalog, attacker, defender);
            s.Combat = null;
            if (!s.Result.IsFinished && defender.Zone == CardZone.Discard && window.DrawOnDefenderKo > 0) Draw(s, defender.ControllerId, window.DrawOnDefenderKo);
            if (!s.Result.IsFinished && s.Rules.ExpandedEnabled)
            {
                if (attackFailed && attacker.HasBoardSlot && window.CounterPenalty > 0)
                    attacker.TurnPowerModifier -= window.CounterPenalty;
                foreach (var sourceId in window.EvadeSources)
                    s.EffectQueue.Add(new EffectStep(defender.ControllerId, sourceId, EffectStepKind.EvadeToRear, 1, defender.InstanceId));
            }
            Drain(s, catalog); GameStateInvariants.AssertValid(s); return EngineActions.Snapshot(s, from);
        }
        public static int Power(GameState s, ICardCatalog catalog, CardInstance card, bool attacking, bool heroTarget)
        {
            if (!catalog.TryGet(card.DefinitionId, out var definition)) throw new InvariantViolationException("Missing definition " + card.DefinitionId);
            var power = CombatPolicies.EffectivePower(definition) + card.TurnPowerModifier;
            if (!s.Rules.AdvancedEnabled) return Math.Max(0, power);
            if (!card.AttachedEquipmentId.IsNone)
            {
                var op = Program(catalog, s.Cards[card.AttachedEquipmentId]);
                if (op == EffectOpcode.EquipmentDefense && !attacking) power++;
                if (op == EffectOpcode.EquipmentAttackCharacter && attacking && !heroTarget) power++;
                if (op == EffectOpcode.EquipmentHighestCostAttack && attacking
                    && !s.Cards.Values.Any(c => c.HasBoardSlot && c.ControllerId == card.ControllerId
                        && catalog.TryGet(c.DefinitionId, out var other) && other.CostPa > definition.CostPa)) power += 2;
            }
            if (!attacking && s.Combat != null && s.Combat.Defender == card.InstanceId) power += s.Combat.DefenseBonus;
            if (attacking && s.Combat != null && s.Combat.Attacker == card.InstanceId) power += s.Combat.AttackBonus;
            power += ExpandedEffects.PowerBonus(s, catalog, card, attacking, heroTarget);
            return Math.Max(0, power);
        }
        private static CommandResult Reject(GameState s) => CommandResult.Reject(s, CommandRejectionCode.InvalidPayload, "Efecto, fase, actor u objetivo ilegal.");
    }
}
