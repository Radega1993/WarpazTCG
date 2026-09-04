using System;

namespace Warpaz.Domain
{
    /// <summary>Catalog type. Never stored as a free-form string in match state.</summary>
    public enum CardType
    {
        Character = 0,
        Hero = 1,
        ClassPath = 2,
        Equipment = 3,
        Action = 4,
        Reaction = 5
    }

    public static class CardTypeCatalog
    {
        public static bool TryFromLabel(string label, out CardType type)
        {
            switch (label)
            {
                case "Personaje":
                    type = CardType.Character;
                    return true;
                case "Héroe":
                    type = CardType.Hero;
                    return true;
                case "Clase":
                    type = CardType.ClassPath;
                    return true;
                case "Equipo":
                    type = CardType.Equipment;
                    return true;
                case "Acción":
                    type = CardType.Action;
                    return true;
                case "Reacción":
                    type = CardType.Reaction;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }

        public static string ToLabel(CardType type)
        {
            switch (type)
            {
                case CardType.Character:
                    return "Personaje";
                case CardType.Hero:
                    return "Héroe";
                case CardType.ClassPath:
                    return "Clase";
                case CardType.Equipment:
                    return "Equipo";
                case CardType.Action:
                    return "Acción";
                case CardType.Reaction:
                    return "Reacción";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown CardType.");
            }
        }
    }
}
