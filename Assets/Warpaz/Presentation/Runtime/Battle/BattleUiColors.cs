using UnityEngine;

namespace Warpaz.Presentation
{
    /// <summary>Shared TCG card / table palette (Presentation only).</summary>
    public static class BattleUiColors
    {
        // Card plate — dark ink like modern TCGs, not flat grey.
        public static readonly Color FaceUpBackground = Hex(0x1B1612);
        public static readonly Color FaceUpHeader = Hex(0xE8D9BF);
        public static readonly Color FaceUpEmptyContent = Hex(0x0C0A09);
        public static readonly Color FaceUpRules = Hex(0xF2E6D0);
        public static readonly Color CardBorder = Hex(0xC9A45C);
        public static readonly Color CardBorderInner = Hex(0x8A6A32);
        public static readonly Color CardText = Hex(0x1A1410);
        public static readonly Color CardTextOnDark = Hex(0xF6F0E4);
        /// <summary>HUD / board labels on dark table chrome.</summary>
        public static readonly Color HudText = Hex(0xF6F0E4);
        public static readonly Color HudPanel = new Color(0.10f, 0.09f, 0.08f, 0.78f);
        public static readonly Color ActionButton = Hex(0xE8D9BF);
        public static readonly Color ActionButtonText = Hex(0x1A1410);
        public static readonly Color CostGem = Hex(0x8B1E2D);
        public static readonly Color PowerGem = Hex(0x1E2430);
        public static readonly Color TypeBar = Hex(0x2A221C);
        public static readonly Color FaceDownBackground = Hex(0x17181B);
        public static readonly Color SelectedBorder = Hex(0xF0D060);
        public static readonly Color PlayableBorder = Hex(0x6E9AD6);
        public static readonly Color LegalOverlay = new Color(0.35f, 0.55f, 0.90f, 0.32f);
        public static readonly Color SelectionOverlay = new Color(1f, 0.84f, 0.2f, 0.35f);

        public static Color Hex(int rgb)
        {
            var r = ((rgb >> 16) & 0xFF) / 255f;
            var g = ((rgb >> 8) & 0xFF) / 255f;
            var b = (rgb & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}
