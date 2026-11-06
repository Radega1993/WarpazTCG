using System;
using Warpaz.Domain;

namespace Warpaz.Content
{
    /// <summary>Reviewed compiled bindings, versioned by 0.3.0-effects; changed text fails closed.</summary>
    public static class Wz1EffectBindings
    {
        public static EffectDefinition[] ForCard(CardId id, string printedText)
        {
            switch (id.Value)
            {
                case "WZ1-078":
                    if (printedText != "Un Personaje de tu Vanguardia obtiene +2 Poder hasta el final del turno.") break;
                    return new[] { new EffectDefinition(EffectOpcode.PowerUntilEndTurn, EffectTarget.FriendlyVanguard, 2) };
                case "WZ1-079":
                    if (printedText != "Un Personaje enemigo de Vanguardia obtiene -2 Poder y no puede interceptar hasta el final del turno.") break;
                    return new[] { new EffectDefinition(EffectOpcode.PowerUntilEndTurn, EffectTarget.EnemyVanguard, -2), new EffectDefinition(EffectOpcode.CannotInterceptUntilEndTurn, EffectTarget.EnemyVanguard) };
                case "WZ1-082":
                    if (printedText != "Un Personaje enemigo obtiene -2 Poder y no puede ser preparado por efectos hasta el final del turno.") break;
                    return new[] { new EffectDefinition(EffectOpcode.PowerUntilEndTurn, EffectTarget.EnemyCharacter, -2), new EffectDefinition(EffectOpcode.CannotReadyUntilEndTurn, EffectTarget.EnemyCharacter) };
                case "WZ1-086":
                    if (printedText != "Un Personaje obtiene +2 Poder hasta el final del turno.") break;
                    return new[] { new EffectDefinition(EffectOpcode.PowerUntilEndTurn, EffectTarget.AnyCharacter, 2) };
            }
            return Array.Empty<EffectDefinition>();
        }
    }
}
