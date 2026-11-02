using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Runtime Main Menu look: warlike castle-gate backdrop + parchment buttons.
    /// </summary>
    public static class MainMenuLayoutBuilder
    {
        private const string CastleGateResourcePath = "Warpaz/UI/Menu/castle_gate";

        private static readonly Color Parchment = new Color(0.90f, 0.83f, 0.70f, 1f);
        private static readonly Color ParchmentHover = new Color(0.96f, 0.90f, 0.78f, 1f);
        private static readonly Color Ink = new Color(0.08f, 0.07f, 0.06f, 1f);
        private static readonly Color Border = new Color(0.12f, 0.10f, 0.08f, 1f);
        private static readonly Color TitleLight = new Color(0.96f, 0.90f, 0.76f, 1f);
        private static readonly Color Scrim = new Color(0.05f, 0.04f, 0.03f, 0.42f);

        public static void Ensure(MainMenuController host)
        {
            if (host == null)
            {
                return;
            }

            var canvas = host.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = host.GetComponent<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            var root = canvas.transform;
            ApplyCastleBackground(root);
            HideLegacyPanel(root);
            StyleExistingButtons(host);
            PlaceButtons(host);
            StyleTitleAndVersion(host, root);
        }

        private static void ApplyCastleBackground(Transform root)
        {
            var bg = root.Find("Background");
            if (bg == null)
            {
                return;
            }

            var image = bg.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var sprite = Resources.Load<Sprite>(CastleGateResourcePath);
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>(CastleGateResourcePath);
                if (tex != null)
                {
                    sprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                // Fallback stone tone if the art asset is missing.
                image.sprite = null;
                image.color = new Color(0.22f, 0.20f, 0.18f, 1f);
            }

            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            Stretch(bg as RectTransform);

            EnsureScrim(bg);
        }

        private static void EnsureScrim(Transform background)
        {
            var existing = background.Find("MenuScrim");
            GameObject go;
            if (existing == null)
            {
                go = new GameObject("MenuScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(background, false);
                go.transform.SetAsFirstSibling();
            }
            else
            {
                go = existing.gameObject;
            }

            Stretch(go.transform as RectTransform);
            var img = go.GetComponent<Image>();
            img.sprite = null;
            img.color = Scrim;
            img.raycastTarget = false;
            // Keep scrim under the legacy Panel (if any) but above the photo.
            go.transform.SetSiblingIndex(0);
        }

        private static void HideLegacyPanel(Transform root)
        {
            var panel = root.Find("Background/Panel");
            if (panel == null)
            {
                panel = root.Find("Panel");
            }

            if (panel == null)
            {
                return;
            }

            var img = panel.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = false;
            }

            Stretch(panel as RectTransform);
        }

        private static void StyleTitleAndVersion(MainMenuController host, Transform root)
        {
            var title = FindTmp(host, root, "Title");
            if (title != null)
            {
                title.text = "WARPAZ TCG";
                title.fontSize = 64f;
                title.color = TitleLight;
                title.faceColor = TitleLight;
                title.fontStyle = FontStyles.Bold;
                title.alignment = TextAlignmentOptions.Center;
                var outline = title.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = title.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
                outline.effectDistance = new Vector2(2f, -2f);
                var rt = title.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.82f);
                rt.anchorMax = new Vector2(0.5f, 0.82f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(720f, 80f);
                rt.anchoredPosition = Vector2.zero;
            }

            var version = FindTmp(host, root, "VersionText");
            if (version != null)
            {
                version.fontSize = 16f;
                version.color = new Color(0.90f, 0.84f, 0.72f, 0.88f);
                version.alignment = TextAlignmentOptions.BottomRight;
                var rt = version.rectTransform;
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-24f, 16f);
                rt.sizeDelta = new Vector2(640f, 40f);
            }

            var soon = FindTmp(host, root, "ComingSoonText");
            if (soon != null)
            {
                soon.color = TitleLight;
                soon.alignment = TextAlignmentOptions.Center;
                var rt = soon.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.08f);
                rt.anchorMax = new Vector2(0.5f, 0.08f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(560f, 40f);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        private static void StyleExistingButtons(MainMenuController host)
        {
            StyleParchmentButton(host.PlayButton, "Jugar", 36f);
            StyleParchmentButton(host.TutorialButton, "Tutorial", 28f);
            StyleParchmentButton(host.DecksButton, "Mazos", 24f);
            StyleParchmentButton(host.CardsButton, "Cartas", 24f);
            StyleParchmentButton(host.SettingsButton, "Ajustes", 24f);
            StyleParchmentButton(host.QuitButton, "Salir", 22f);
        }

        private static void PlaceButtons(MainMenuController host)
        {
            // Break out of vertical layout if present.
            DetachFromLayout(host.PlayButton);
            DetachFromLayout(host.TutorialButton);
            DetachFromLayout(host.DecksButton);
            DetachFromLayout(host.CardsButton);
            DetachFromLayout(host.SettingsButton);
            DetachFromLayout(host.QuitButton);

            Place(host.PlayButton, 0.5f, 0.62f, 420f, 78f);
            Place(host.TutorialButton, 0.5f, 0.50f, 360f, 64f);
            Place(host.DecksButton, 0.22f, 0.58f, 260f, 56f);
            Place(host.CardsButton, 0.22f, 0.48f, 260f, 56f);
            Place(host.SettingsButton, 0.78f, 0.58f, 260f, 56f);
            // Quit also lives in AppWindowChrome; keep a parchment Quit mid-bottom for discoverability.
            Place(host.QuitButton, 0.5f, 0.18f, 220f, 48f);
        }

        private static void StyleParchmentButton(Button button, string label, float fontSize)
        {
            if (button == null)
            {
                return;
            }

            // Remove previous full-rect "border" child that covered face+text.
            var badBorder = button.transform.Find("ParchmentBorder");
            if (badBorder != null)
            {
                Object.Destroy(badBorder.gameObject);
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.color = Parchment;
                image.type = Image.Type.Simple;
                image.raycastTarget = true;
            }

            // ColorTint multiplies graphic color — keep normal white so parchment stays readable.
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = ParchmentHover;
            colors.pressedColor = new Color(0.82f, 0.74f, 0.58f, 1f);
            colors.selectedColor = ParchmentHover;
            colors.disabledColor = new Color(0.75f, 0.72f, 0.65f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = image;

            var outline = button.GetComponent<Outline>();
            if (outline == null)
            {
                outline = button.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = Border;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;

            var tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.transform.SetAsLastSibling();
                tmp.text = label;
                tmp.color = Ink;
                tmp.fontSize = fontSize;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                tmp.enableAutoSizing = false;
                // Force opaque black ink (no theme leftover alpha).
                tmp.faceColor = Ink;
            }

            var le = button.GetComponent<LayoutElement>();
            if (le != null)
            {
                Object.Destroy(le);
            }
        }

        private static void Place(Button button, float anchorX, float anchorY, float width, float height)
        {
            if (button == null)
            {
                return;
            }

            var rt = button.transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void DetachFromLayout(Button button)
        {
            if (button == null)
            {
                return;
            }

            var parent = button.transform.parent;
            if (parent != null && parent.name == "Buttons")
            {
                var canvas = button.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    button.transform.SetParent(canvas.transform, true);
                }
            }
        }

        private static void Stretch(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TMP_Text FindTmp(MainMenuController host, Transform root, string name)
        {
            var t = root.Find("Background/Panel/" + name) ?? root.Find("Panel/" + name) ?? root.Find(name);
            if (t != null)
            {
                return t.GetComponent<TMP_Text>();
            }

            return null;
        }
    }
}
