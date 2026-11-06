using TMPro;
using UnityEngine;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Card-specific type ramp: display serif for names, italic for type, body serif for rules.
    /// Avoids the default Liberation Sans look on face-up cards.
    /// </summary>
    public static class CardTypography
    {
        private static TMP_FontAsset _display;
        private static TMP_FontAsset _body;
        private static TMP_FontAsset _italic;
        private static bool _attempted;

        public static void ApplyName(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            EnsureFonts();
            if (_display != null)
            {
                text.font = _display;
            }

            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 0f;
            text.lineSpacing = -15f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.maxVisibleLines = 2;
            Outline(text, new Color(0.12f, 0.08f, 0.04f, 0.35f), 0.12f);
        }

        public static void ApplyTypeLine(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            EnsureFonts();
            if (_italic != null)
            {
                text.font = _italic;
            }

            text.fontStyle = FontStyles.Italic;
            text.characterSpacing = 1f;
            text.fontSizeMax = Mathf.Max(text.fontSizeMax, 13f);
            Outline(text, new Color(0f, 0f, 0f, 0.45f), 0.08f);
        }

        public static void ApplyRules(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            EnsureFonts();
            if (_body != null)
            {
                text.font = _body;
            }

            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0.5f;
            text.lineSpacing = -8f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static void ApplyStat(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            EnsureFonts();
            if (_display != null)
            {
                text.font = _display;
            }

            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = -2f;
            Outline(text, new Color(0f, 0f, 0f, 0.65f), 0.2f);
        }

        public static void ApplyCaption(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            EnsureFonts();
            if (_body != null)
            {
                text.font = _body;
            }

            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 6f;
        }

        private static void Outline(TMP_Text text, Color color, float thickness)
        {
            text.outlineColor = color;
            text.outlineWidth = thickness;
        }

        private static void EnsureFonts()
        {
            if (_attempted)
            {
                return;
            }

            _attempted = true;
            _display = CreateFromResources("Warpaz/UI/Fonts/NotoSerifDisplay-Bold");
            _body = CreateFromResources("Warpaz/UI/Fonts/NotoSerif-Regular");
            _italic = CreateFromResources("Warpaz/UI/Fonts/NotoSerif-Italic");
        }

        private static TMP_FontAsset CreateFromResources(string path)
        {
            var source = Resources.Load<Font>(path);
            if (source == null)
            {
                Debug.LogWarning("WARPAZ: Missing card font Resources/" + path);
                return null;
            }

            try
            {
                var asset = TMP_FontAsset.CreateFontAsset(source);
                if (asset != null)
                {
                    asset.name = "WarpazRuntime_" + source.name;
                }

                return asset;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("WARPAZ: TMP font create failed for " + path + ": " + ex.Message);
                return null;
            }
        }
    }
}
