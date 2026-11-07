using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Warpaz.Presentation
{
    /// <summary>
    /// Persistent top bar with an obvious quit control for standalone builds.
    /// </summary>
    public sealed class AppWindowChrome : MonoBehaviour
    {
        /// <summary>Title bar occupies [BarBottom, 1]. Put battle UI strictly below ContentTop.</summary>
        public const float BarBottom = 0.962f;

        public const float ContentTop = 0.955f;

        private static AppWindowChrome _instance;

        public static void Ensure(Transform host)
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("AppWindowChrome");
            // Parent under the composition root (already DontDestroyOnLoad). Calling
            // DontDestroyOnLoad on a child is illegal and spam the console.
            if (host != null)
            {
                go.transform.SetParent(host, false);
            }
            else
            {
                DontDestroyOnLoad(go);
            }

            _instance = go.AddComponent<AppWindowChrome>();
            _instance.Build();
        }

        private void Build()
        {
            var canvasGo = new GameObject(
                "ChromeCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bar = new GameObject("TitleBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(canvasGo.transform, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, BarBottom);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0.18f, 0.12f, 0.08f, 0.96f);

            var title = CreateLabel(bar.transform, "Title", "WARPAZ TCG", 20f, TextAlignmentOptions.Left);
            Layout(title.rectTransform, 0.015f, 0.15f, 0.45f, 0.85f);
            title.color = new Color(0.95f, 0.88f, 0.72f, 1f);

            // Wide, labeled quit — ASCII only so TMP always shows it.
            var quit = new GameObject("QuitButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            quit.transform.SetParent(bar.transform, false);
            Layout(quit.transform as RectTransform, 0.86f, 0.12f, 0.99f, 0.88f);
            var quitImg = quit.GetComponent<Image>();
            quitImg.color = new Color(0.93f, 0.84f, 0.68f, 1f);
            var quitBtn = quit.GetComponent<Button>();
            quitBtn.transition = Selectable.Transition.ColorTint;
            quitBtn.targetGraphic = quitImg;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.78f, 1f);
            colors.pressedColor = new Color(0.85f, 0.55f, 0.40f, 1f);
            quitBtn.colors = colors;
            quitBtn.onClick.AddListener(QuitApp);

            var outline = quit.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);

            var quitLabel = CreateLabel(quit.transform, "Label", "Salir  X", 22f, TextAlignmentOptions.Center);
            Layout(quitLabel.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f);
            quitLabel.color = Color.black;
            quitLabel.fontStyle = FontStyles.Bold;
            quitLabel.faceColor = Color.black;
        }

        public static void QuitApp()
        {
            Debug.Log("WARPAZ TCG: Quit requested from window chrome.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string name,
            string text,
            float size,
            TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static void Layout(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
