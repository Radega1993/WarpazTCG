using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Expanded = reading / confirm. Compact = top strip so hand and board stay clickable.
    /// Abort lives as a discreet top-corner control on the canvas, not inside the coach panel.
    /// </summary>
    public sealed class TutorialCoachView : MonoBehaviour
    {
        private RectTransform _root;
        private Image _bg;
        private TMP_Text _stepLabel;
        private TMP_Text _title;
        private TMP_Text _body;
        private TMP_Text _compactHint;
        private Button _primaryButton;
        private TMP_Text _primaryLabel;
        private Button _helpButton;
        private TMP_Text _helpLabel;
        private Button _abortButton;
        private TMP_Text _abortLabel;
        private bool _expanded = true;

        public static TutorialCoachView Ensure(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var existing = canvasRoot.GetComponentInChildren<TutorialCoachView>(true);
            if (existing != null)
            {
                existing.EnsureAbortCorner(canvasRoot);
                return existing;
            }

            var go = new GameObject("TutorialCoach", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasRoot, false);
            var view = go.AddComponent<TutorialCoachView>();
            view.Build();
            view.EnsureAbortCorner(canvasRoot);
            return view;
        }

        public void Bind(System.Action onPrimary, System.Action onHelp, System.Action onAbort)
        {
            EnsureBuilt();
            if (_primaryButton != null)
            {
                _primaryButton.onClick.RemoveAllListeners();
                if (onPrimary != null)
                {
                    _primaryButton.onClick.AddListener(() => onPrimary());
                }
            }

            if (_helpButton != null)
            {
                _helpButton.onClick.RemoveAllListeners();
                if (onHelp != null)
                {
                    _helpButton.onClick.AddListener(() => onHelp());
                }
            }

            if (_abortButton != null)
            {
                _abortButton.onClick.RemoveAllListeners();
                if (onAbort != null)
                {
                    _abortButton.onClick.AddListener(() => onAbort());
                }
            }
        }

        public void Render(
            int stepIndex,
            int stepCount,
            string title,
            string body,
            string compactHint,
            string primaryLabel,
            bool primaryVisible,
            bool expanded)
        {
            EnsureBuilt();
            gameObject.SetActive(true);
            _expanded = expanded;
            ApplyLayoutMode(expanded);

            if (_stepLabel != null)
            {
                _stepLabel.text = "Paso " + (stepIndex + 1) + " / " + stepCount;
            }

            if (_title != null)
            {
                _title.text = title ?? string.Empty;
                _title.gameObject.SetActive(expanded);
                _title.color = Color.white;
            }

            if (_stepLabel != null)
            {
                _stepLabel.gameObject.SetActive(expanded);
                _stepLabel.color = new Color(0.85f, 0.80f, 0.62f, 1f);
            }

            if (_body != null)
            {
                _body.text = body ?? string.Empty;
                _body.gameObject.SetActive(expanded);
                _body.color = new Color(0.92f, 0.91f, 0.88f, 1f);
            }

            if (_compactHint != null)
            {
                _compactHint.text = compactHint ?? string.Empty;
                _compactHint.gameObject.SetActive(!expanded);
                _compactHint.color = BattleUiColors.HudText;
                _compactHint.fontStyle = FontStyles.Bold;
            }

            if (_bg != null)
            {
                _bg.color = expanded
                    ? new Color(0.08f, 0.09f, 0.11f, 0.96f)
                    : new Color(0.12f, 0.11f, 0.10f, 0.92f);
                _bg.raycastTarget = expanded;
            }

            if (_primaryLabel != null)
            {
                _primaryLabel.text = string.IsNullOrEmpty(primaryLabel) ? "Continuar" : primaryLabel;
            }

            if (_primaryButton != null)
            {
                _primaryButton.gameObject.SetActive(expanded && primaryVisible);
            }

            if (_helpButton != null)
            {
                _helpButton.gameObject.SetActive(!expanded);
            }

            if (_abortButton != null)
            {
                _abortButton.gameObject.SetActive(true);
                _abortButton.transform.SetAsLastSibling();
            }
        }

        public void SetExpanded(bool expanded)
        {
            _expanded = expanded;
        }

        public bool IsExpanded => _expanded;

        public void Hide()
        {
            gameObject.SetActive(false);
            if (_abortButton != null)
            {
                _abortButton.gameObject.SetActive(false);
            }
        }

        private void ApplyLayoutMode(bool expanded)
        {
            if (_root == null)
            {
                return;
            }

            if (expanded)
            {
                _root.anchorMin = new Vector2(0.10f, 0.22f);
                _root.anchorMax = new Vector2(0.90f, 0.78f);
            }
            else
            {
                // Narrow center strip under chrome — leave piles (right) and Tutor HUD (left) free.
                var top = AppWindowChrome.ContentTop;
                _root.anchorMin = new Vector2(0.32f, top - 0.032f);
                _root.anchorMax = new Vector2(0.68f, top);
            }

            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            _root.SetAsLastSibling();
            if (_abortButton != null)
            {
                _abortButton.transform.SetAsLastSibling();
            }
        }

        private void Build()
        {
            _root = transform as RectTransform;
            _bg = GetComponent<Image>();
            _bg.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);
            _bg.raycastTarget = true;

            _stepLabel = CreateTmp("StepLabel", 15f, TextAlignmentOptions.Left);
            Layout(_stepLabel.rectTransform, 0.03f, 0.86f, 0.45f, 0.97f);
            _stepLabel.color = new Color(0.75f, 0.72f, 0.55f, 1f);

            _title = CreateTmp("Title", 24f, TextAlignmentOptions.Left);
            Layout(_title.rectTransform, 0.03f, 0.70f, 0.70f, 0.88f);
            _title.color = Color.white;
            _title.fontStyle = FontStyles.Bold;

            _body = CreateTmp("Body", 18f, TextAlignmentOptions.TopLeft);
            Layout(_body.rectTransform, 0.03f, 0.18f, 0.68f, 0.68f);
            _body.color = new Color(0.90f, 0.90f, 0.92f, 1f);
            _body.textWrappingMode = TextWrappingModes.Normal;
            _body.overflowMode = TextOverflowModes.Ellipsis;

            _compactHint = CreateTmp("CompactHint", 18f, TextAlignmentOptions.Left);
            Layout(_compactHint.rectTransform, 0.03f, 0.15f, 0.96f, 0.85f);
            _compactHint.color = new Color(0.95f, 0.93f, 0.85f, 1f);
            _compactHint.gameObject.SetActive(false);

            var btnGo = new GameObject("PrimaryButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(transform, false);
            _primaryButton = btnGo.GetComponent<Button>();
            btnGo.GetComponent<Image>().color = new Color(0.45f, 0.32f, 0.18f, 1f);
            Layout(btnGo.transform as RectTransform, 0.72f, 0.28f, 0.97f, 0.62f);
            _primaryLabel = CreateTmp("PrimaryButton/Label", 20f, TextAlignmentOptions.Center);
            _primaryLabel.transform.SetParent(btnGo.transform, false);
            Layout(_primaryLabel.rectTransform, 0.05f, 0.1f, 0.95f, 0.9f);
            _primaryLabel.color = Color.white;
            _primaryLabel.raycastTarget = false;

            var helpGo = new GameObject("HelpButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            helpGo.transform.SetParent(transform, false);
            _helpButton = helpGo.GetComponent<Button>();
            helpGo.GetComponent<Image>().color = new Color(0.28f, 0.30f, 0.36f, 1f);
            Layout(helpGo.transform as RectTransform, 0.82f, 0.18f, 0.97f, 0.82f);
            _helpLabel = CreateTmp("HelpButton/Label", 16f, TextAlignmentOptions.Center);
            _helpLabel.transform.SetParent(helpGo.transform, false);
            Layout(_helpLabel.rectTransform, 0.05f, 0.1f, 0.95f, 0.9f);
            _helpLabel.text = "Ayuda";
            _helpLabel.color = Color.white;
            _helpLabel.raycastTarget = false;
            helpGo.SetActive(false);

            // Remove any legacy abort that lived inside the coach panel.
            var legacyAbort = transform.Find("AbortButton");
            if (legacyAbort != null)
            {
                Object.Destroy(legacyAbort.gameObject);
            }

            ApplyLayoutMode(true);
        }

        private void EnsureAbortCorner(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return;
            }

            var existing = canvasRoot.Find("TutorialAbortCorner");
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(
                    "TutorialAbortCorner",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                go.transform.SetParent(canvasRoot, false);
            }
            else
            {
                go = existing.gameObject;
            }

            var rt = (RectTransform)go.transform;
            // Just left of chrome "Salir X", clear of piles and hero HUD.
            var top = AppWindowChrome.ContentTop;
            rt.anchorMin = new Vector2(0.74f, top - 0.032f);
            rt.anchorMax = new Vector2(0.86f, top);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.transform.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.16f, 0.14f, 0.72f);
            img.raycastTarget = true;

            _abortButton = go.GetComponent<Button>();
            _abortButton.targetGraphic = img;
            _abortButton.transition = Selectable.Transition.ColorTint;

            var labelT = go.transform.Find("Label");
            if (labelT == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                labelT = labelGo.transform;
                _abortLabel = labelGo.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                _abortLabel = labelT.GetComponent<TextMeshProUGUI>() ?? labelT.gameObject.AddComponent<TextMeshProUGUI>();
            }

            Layout(_abortLabel.rectTransform, 0.04f, 0.08f, 0.96f, 0.92f);
            _abortLabel.text = "Salir tutorial";
            _abortLabel.fontSize = 14f;
            _abortLabel.fontSizeMin = 11f;
            _abortLabel.fontSizeMax = 15f;
            _abortLabel.enableAutoSizing = true;
            _abortLabel.alignment = TextAlignmentOptions.Center;
            _abortLabel.color = new Color(0.90f, 0.84f, 0.72f, 0.92f);
            _abortLabel.fontStyle = FontStyles.Normal;
            _abortLabel.raycastTarget = false;
            _abortLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _abortLabel.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void EnsureBuilt()
        {
            if (_title == null)
            {
                Build();
            }
        }

        private void OnDestroy()
        {
            if (_abortButton != null)
            {
                Object.Destroy(_abortButton.gameObject);
                _abortButton = null;
            }
        }

        private TMP_Text CreateTmp(string path, float size, TextAlignmentOptions align)
        {
            var slash = path.LastIndexOf('/');
            Transform parent = transform;
            var leaf = path;
            if (slash >= 0)
            {
                leaf = path.Substring(slash + 1);
                var parentPath = path.Substring(0, slash);
                var existing = transform.Find(parentPath);
                if (existing != null)
                {
                    parent = existing;
                }
            }

            var go = new GameObject(leaf, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static void Layout(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
