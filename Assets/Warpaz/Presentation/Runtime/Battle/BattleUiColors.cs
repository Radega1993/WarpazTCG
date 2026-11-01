using UnityEngine;

namespace Warpaz.Presentation
{
    /// <summary>Shared grey-table palette for face-up / face-down cards (Presentation only).</summary>
    public static class BattleUiColors
    {
        public static readonly Color FaceUpBackground = Hex(0x55575B);
        public static readonly Color FaceUpHeader = Hex(0x62656A);
        public static readonly Color FaceUpEmptyContent = Hex(0x707277);
        public static readonly Color FaceUpRules = Hex(0x45474B);
        public static readonly Color CardBorder = Hex(0x8C734B);
        public static readonly Color CardText = Hex(0xF1EEE7);
        public static readonly Color FaceDownBackground = Hex(0x17181B);
        public static readonly Color SelectedBorder = Hex(0xD4AF37);
        public static readonly Color PlayableBorder = Hex(0x7B5EA7);
        public static readonly Color LegalOverlay = new Color(0.55f, 0.25f, 0.85f, 0.35f);
        public static readonly Color SelectionOverlay = new Color(1f, 0.84f, 0.2f, 0.45f);

        public static Color Hex(int rgb)
        {
            var r = ((rgb >> 16) & 0xFF) / 255f;
            var g = ((rgb >> 8) & 0xFF) / 255f;
            var b = (rgb & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}
