using System;
using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Application
{
    public sealed class EffectDecisionOption
    {
        public EffectDecisionOption(LegalAction action, string label) { Action = action; Label = label; }
        public LegalAction Action { get; }
        public string Label { get; }
    }
    public sealed class EffectDecisionView
    {
        public EffectDecisionView(string title, IReadOnlyList<EffectDecisionOption> options) { Title = title; Options = options; }
        public string Title { get; }
        public IReadOnlyList<EffectDecisionOption> Options { get; }
        public static EffectDecisionView Project(GameState state, ICardCatalog catalog, PlayerId viewer)
        {
            if (state == null || viewer != state.PriorityPlayerId || !state.Rules.AdvancedEnabled
                || (state.PendingDecision.Kind != PendingDecisionKind.EffectChoice && state.PendingDecision.Kind != PendingDecisionKind.Reaction)) return null;
            var reaction = state.PendingDecision.Kind == PendingDecisionKind.Reaction;
            var title = reaction ? "Combate: Reacción, Respuesta o pasar prioridad" : TitleFor(state.PendingEffectKind.Value);
            if (reaction && state.TryGetCard(state.Combat.Attacker, out var attacker) && state.TryGetCard(state.Combat.Defender, out var defender))
                title += "\nAtaque " + AdvancedEffects.Power(state, catalog, attacker, true, state.Combat.TargetsHero)
                    + " · Defensa " + AdvancedEffects.Power(state, catalog, defender, false, state.Combat.TargetsHero)
                    + " · Pila " + state.Combat.PendingReactionCount + " · Pases " + state.Combat.ConsecutivePasses + "/2";
            var options = new List<EffectDecisionOption>();
            foreach (var action in LegalActions.For(state, catalog))
            {
                var label = "Mantener arriba";
                if (state.PendingEffectKind == EffectStepKind.EvadeToRear) label = "No mover al defensor";
                if (!action.PrimaryInstanceId.IsNone && state.TryGetCard(action.PrimaryInstanceId, out var card) && catalog.TryGet(card.DefinitionId, out var d))
                    label = d.Name + " [" + d.CardId + "]";
                if (action.Type == CommandType.React) label = "Reacción: " + label;
                if (action.Type == CommandType.Respond) label = "Descartar como Respuesta: " + label;
                if (action.Type == CommandType.PassPriority) label = "Pasar prioridad";
                options.Add(new EffectDecisionOption(action, label));
            }
            return new EffectDecisionView(title, Array.AsReadOnly(options.ToArray()));
        }
        private static string TitleFor(EffectStepKind kind)
        {
            switch (kind)
            {
                case EffectStepKind.Discard: return "Elige una carta de tu mano para descartar";
                case EffectStepKind.BottomHand: return "Elige una carta de tu mano para poner debajo del mazo";
                case EffectStepKind.OrderTop: return "Ordena el mazo: elige la siguiente carta, de arriba hacia abajo";
                case EffectStepKind.LookBottom: return "Elige la carta para ponerla debajo, o mantenla arriba";
                case EffectStepKind.ReturnCharacter: return "Devuelve a tu mano un Personaje de coste 2 o menos";
                case EffectStepKind.ReturnEquipment: return "Devuelve a tu mano un Equipo de coste 2 o menos";
                case EffectStepKind.EvadeToRear: return "Puedes mover al defensor superviviente a la primera casilla libre de Retaguardia";
                default: return "Elige cómo resolver el efecto";
            }
        }
    }
}
