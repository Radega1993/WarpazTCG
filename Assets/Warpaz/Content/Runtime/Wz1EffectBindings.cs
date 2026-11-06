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
                case "WZ1-019": return Bind(printedText, "Mientras tenga un Equipo, obtiene +1 Poder al defender.", EffectOpcode.DefenseEquipped);
                case "WZ1-027": return Bind(printedText, "Los efectos enemigos no pueden mover este Personaje de la Vanguardia.", EffectOpcode.ImmovableVanguard);
                case "WZ1-034": return Bind(printedText, "Mientras controles otro Gnoll en Vanguardia, obtiene +1 Poder.", EffectOpcode.PowerOtherGnollVanguard);
                case "WZ1-035": return Bind(printedText, "Obtiene +1 Poder al defender contra un atacante con mayor Poder impreso.", EffectOpcode.DefenseAgainstHigherPrinted);
                case "WZ1-037": return Bind(printedText, "Tus otros Gnoll obtienen +1 Poder durante el turno rival.", EffectOpcode.AuraOtherGnollOpponentTurn);
                case "WZ1-040": return Bind(printedText, "Mientras esté equipado, obtiene +1 Poder al atacar.", EffectOpcode.AttackEquipped);
                case "WZ1-045": return Bind(printedText, "Tus otros Personajes de Vanguardia obtienen +1 Poder al defender.", EffectOpcode.AuraOtherVanguardDefense);
                case "WZ1-050": return Bind(printedText, "Este Personaje no puede interceptar.", EffectOpcode.CannotIntercept);
                case "WZ1-054": return Bind(printedText, "Mientras sea tu único Personaje de Vanguardia, obtiene +1 Poder.", EffectOpcode.PowerLoneVanguard);
                case "WZ1-056": return Bind(printedText, "Obtiene +1 Poder cuando ataca a un Personaje de coste impreso 5 o más.", EffectOpcode.AttackCostFive);
                case "WZ1-058": return Bind(printedText, "Mientras controles un Personaje de coste impreso 5 o más, obtiene +1 Poder.", EffectOpcode.PowerOwnCostFive);
                case "WZ1-059": return Bind(printedText, "Cuando ataque a un Personaje con menor coste impreso, obtiene +1 Poder durante ese combate.", EffectOpcode.AttackLowerCost);
                case "WZ1-060": return Bind(printedText, "No puede ser movido por efectos enemigos. Obtiene +1 Poder al defender.", EffectOpcode.DefenseAndImmovable);
                case "WZ1-073": return Bind(printedText, "Obtiene +2 Poder cuando ataca a un Personaje de coste impreso 6 o más.", EffectOpcode.AttackCostSix);
                case "WZ1-114": return Bind(printedText, "El portador obtiene +2 Poder al defender y no puede ser movido por efectos enemigos.", EffectOpcode.EquipmentArmor);
                case "WZ1-099": return Bind(printedText, "Reacción — Tu defensor obtiene +2 Poder durante este combate. Si el ataque falla, el atacante obtiene -1 Poder hasta el final del turno.", EffectOpcode.ReactionCounter);
                case "WZ1-104": return Bind(printedText, "Reacción — Tu Personaje defensor obtiene +2 Poder durante este combate. Si sobrevive y hay espacio, puedes moverlo a Retaguardia.", EffectOpcode.ReactionEvade);
                case "WZ1-018": return Bind(printedText, "Despliegue — Mira la primera carta de tu mazo. Puedes colocarla en la parte inferior.", EffectOpcode.DeployLookBottom);
                case "WZ1-024": return Bind(printedText, "Cuando este Personaje intercepte, obtiene +1 Poder durante ese combate.", EffectOpcode.InterceptPower);
                case "WZ1-036": return Bind(printedText, "Cuando intercepte, obtiene +1 Poder durante ese combate.", EffectOpcode.InterceptPower);
                case "WZ1-055": return Bind(printedText, "Cuando intercepte un ataque dirigido a tu Héroe, obtiene +1 Poder durante ese combate.", EffectOpcode.InterceptPower);
                case "WZ1-028": return Bind(printedText, "Despliegue — Devuelve a tu mano un Equipo de coste 2 o menos desde tu descarte.", EffectOpcode.DeployReturnEquipment);
                case "WZ1-069": return Bind(printedText, "Despliegue — Devuelve a tu mano un Personaje de coste 2 o menos desde tu descarte.", EffectOpcode.DeployReturnCharacter);
                case "WZ1-039": return Bind(printedText, "Una vez por turno, cuando un Equipo en juego vaya al descarte, roba una carta y después descarta una.", EffectOpcode.EquipmentDiscardLoot);
                case "WZ1-084": return Bind(printedText, "Sacrifica uno de tus Personajes. Roba dos cartas y después descarta una.", EffectOpcode.SacrificeDrawDiscard);
                case "WZ1-096": return Bind(printedText, "Mira las cuatro primeras cartas de tu mazo y colócalas en cualquier orden. Roba una carta y después coloca una carta de tu mano debajo del mazo.", EffectOpcode.OrderDrawBottom);
                case "WZ1-098": return Bind(printedText, "Reacción — Cuando uno de tus Héroes o Personajes defienda: obtiene +2 Poder durante ese combate.", EffectOpcode.ReactionDefense);
                case "WZ1-100": return Bind(printedText, "Reacción — Cuando uno de tus Personajes sea el defensor, devuélvelo a tu mano. Descarta su Equipo y termina el combate.", EffectOpcode.ReactionEscape);
                case "WZ1-102": return Bind(printedText, "Reacción — Un Personaje defensor obtiene +2 Poder durante este combate. Si aun así queda K.O., roba una carta.", EffectOpcode.ReactionKoDraw);
                case "WZ1-106": return Bind(printedText, "Reacción — Tu defensor obtiene +1 Poder durante este combate, o +3 si está equipado.", EffectOpcode.ReactionEquippedDefense);
                case "WZ1-107": return Bind(printedText, "Reacción — Tu defensor obtiene +2 Poder si su Poder impreso es menor que el del atacante; de lo contrario obtiene +1.", EffectOpcode.ReactionUnderdog);
                case "WZ1-108": return Bind(printedText, "El portador obtiene +1 Poder cuando ataca a un Personaje.", EffectOpcode.EquipmentAttackCharacter);
                case "WZ1-109": return Bind(printedText, "El portador obtiene +1 Poder al defender.", EffectOpcode.EquipmentDefense);
                case "WZ1-119": return Bind(printedText, "El portador obtiene +2 Poder al atacar mientras tenga el mayor coste impreso entre tus Personajes.", EffectOpcode.EquipmentHighestCostAttack);
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
        private static EffectDefinition[] Bind(string actual, string expected, EffectOpcode opcode)
            => actual == expected ? new[] { new EffectDefinition(opcode, EffectTarget.AnyCharacter) } : Array.Empty<EffectDefinition>();
    }
}
