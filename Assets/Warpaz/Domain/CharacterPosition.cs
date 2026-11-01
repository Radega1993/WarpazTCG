using System;

namespace Warpaz.Domain
{
    /// <summary>
    /// Printed board-row restriction for Characters. Parsed from <see cref="CardDefinition.Position"/>.
    /// </summary>
    public enum CharacterPosition
    {
        Vanguard = 0,
        Rearguard = 1,
        Any = 2
    }

    /// <summary>Parses catalog position labels (Spanish and English).</summary>
    public static class CharacterPositions
    {
        public static bool TryParse(string value, out CharacterPosition position)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                position = default;
                return false;
            }

            switch (value.Trim())
            {
                case "Vanguardia":
                case "Vanguard":
                    position = CharacterPosition.Vanguard;
                    return true;
                case "Retaguardia":
                case "Rearguard":
                    position = CharacterPosition.Rearguard;
                    return true;
                case "Cualquiera":
                case "Any":
                    position = CharacterPosition.Any;
                    return true;
                default:
                    position = default;
                    return false;
            }
        }

        public static CharacterPosition Parse(string value)
        {
            if (!TryParse(value, out var position))
            {
                throw new ArgumentException(
                    "Unknown character position '" + value + "'. Expected Vanguardia/Retaguardia/Cualquiera.",
                    nameof(value));
            }

            return position;
        }

        public static bool AllowsRow(CharacterPosition position, BoardRow row)
        {
            switch (position)
            {
                case CharacterPosition.Vanguard:
                    return row == BoardRow.Vanguard;
                case CharacterPosition.Rearguard:
                    return row == BoardRow.Rearguard;
                case CharacterPosition.Any:
                    return true;
                default:
                    return false;
            }
        }

        public static bool AllowsRow(string positionLabel, BoardRow row)
        {
            return TryParse(positionLabel, out var position) && AllowsRow(position, row);
        }
    }
}
