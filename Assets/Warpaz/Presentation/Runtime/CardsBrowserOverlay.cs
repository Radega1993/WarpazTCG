using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Full-screen browse of all catalog cards (keyed by cardId). Presentation only.
    /// Grid on the left; selected card enlarges into the open grey column on the right.
    /// </summary>
    public sealed class CardsBrowserOverlay : MonoBehaviour
    {
        private const float CardAspect = CardLayout.Aspect;

        private ICardCatalog _catalog;
        private System.Action _onClose;
        private RectTransform _content;
        private TMP_Text _title;
        private TMP_Text _detail;
        private CardPlaceholderView _preview;
        private RectTransform _previewRoot;
        private RectTransform _previewFrame;
        private readonly List<CardPlaceholderView> _cells = new List<CardPlaceholderView>();
        private string _selectedCardId;

        public static CardsBrowserOverlay Show(Transform canvasRoot, ICardCatalog catalog, System.Action onClose)
        {
            if (canvasRoot == null || catalog == null)
            {
                return null;
            }

            var existing = canvasRoot.Find("CardsBrowserOverlay");
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
            }

            var go = new GameObject("CardsBrowserOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasRoot, false);
            go.transform.SetAsLastSibling();
            var overlay = go.AddComponent<CardsBrowserOverlay>();
            overlay.Build(catalog, onClose);
            return overlay;
        }

        private void Build(ICardCatalog catalog, System.Action onClose)
        {
            _catalog = catalog;
            _onClose = onClose;

            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = GetComponent<Image>();
            bg.color = new Color(0.08f, 0.07f, 0.06f, 0.96f);
            bg.raycastTarget = true;

            // Stay below AppWindowChrome title bar.
            var top = AppWindowChrome.ContentTop;
            _title = CreateLabel(transform, "Title", "Cartas WZ1", 36f, TextAlignmentOptions.Center);
            Layout(_title.rectTransform, 0.14f, top - 0.07f, 0.55f, top);
            _title.color = new Color(0.96f, 0.90f, 0.76f, 1f);
            _title.fontStyle = FontStyles.Bold;

            var count = CreateLabel(
                transform,
                "Count",
                catalog.Count + " cartas",
                22f,
                TextAlignmentOptions.Right);
            Layout(count.rectTransform, 0.55f, top - 0.07f, 0.72f, top);
            count.color = new Color(0.90f, 0.84f, 0.72f, 0.9f);

            var back = CreateButton(transform, "BackButton", "Volver", OnClose);
            Layout(back.GetComponent<RectTransform>(), 0.02f, top - 0.07f, 0.12f, top);

            // Grid hugs 4 columns; remaining width is the preview column (red-box space).
            var scrollGo = new GameObject(
                "Scroll",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(ScrollRect));
            scrollGo.transform.SetParent(transform, false);
            Layout((RectTransform)scrollGo.transform, 0.015f, 0.04f, 0.455f, top - 0.085f);
            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = new Color(0.12f, 0.10f, 0.08f, 0.85f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch((RectTransform)viewport.transform);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Image>().raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewport.transform, false);
            _content = (RectTransform)contentGo.transform;
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, 0f);

            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(148f, 208f);
            grid.spacing = new Vector2(10f, 10f);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = _content;

            BuildPreviewPanel();
            Populate();
        }

        private void LateUpdate()
        {
            if (_content != null && _content.parent is RectTransform viewport)
            {
                var grid = _content.GetComponent<GridLayoutGroup>();
                var columns = Mathf.Clamp(Mathf.FloorToInt((viewport.rect.width - 24f) / 158f), 1, 4);
                grid.constraintCount = columns;
                var width = Mathf.Min(148f, Mathf.Max(40f, (viewport.rect.width - 24f - (columns - 1) * 10f) / columns));
                grid.cellSize = new Vector2(width, width / CardLayout.Aspect);
            }
            LayoutPreviewCard();
        }

        private void BuildPreviewPanel()
        {
            var panel = new GameObject(
                "PreviewPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(transform, false);
            // Occupies the open grey column between grid and screen edge.
            var top = AppWindowChrome.ContentTop;
            Layout((RectTransform)panel.transform, 0.465f, 0.04f, 0.985f, top - 0.085f);
            panel.transform.SetAsLastSibling();
            var panelImg = panel.GetComponent<Image>();
            panelImg.color = new Color(0.14f, 0.12f, 0.10f, 0.95f);
            panelImg.raycastTarget = true;

            var frameGo = new GameObject("PreviewFrame", typeof(RectTransform));
            frameGo.transform.SetParent(panel.transform, false);
            _previewFrame = (RectTransform)frameGo.transform;
            // Card uses almost the whole column; compact meta under it.
            Layout(_previewFrame, 0.04f, 0.14f, 0.96f, 0.97f);

            var cardGo = new GameObject("PreviewCard", typeof(RectTransform), typeof(CanvasRenderer));
            cardGo.transform.SetParent(frameGo.transform, false);
            _previewRoot = (RectTransform)cardGo.transform;
            _previewRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _previewRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _previewRoot.pivot = new Vector2(0.5f, 0.5f);
            _previewRoot.anchoredPosition = Vector2.zero;
            _previewRoot.sizeDelta = new Vector2(360f, 504f);

            _preview = cardGo.AddComponent<CardPlaceholderView>();
            _preview.EnsureChrome();
            _preview.SetInteractable(false);
            DisableRaycasts(cardGo);

            _detail = CreateLabel(panel.transform, "Detail", "Toca una carta", 17f, TextAlignmentOptions.TopLeft);
            Layout(_detail.rectTransform, 0.05f, 0.02f, 0.95f, 0.13f);
            _detail.color = new Color(0.95f, 0.90f, 0.78f, 1f);
            _detail.textWrappingMode = TextWrappingModes.Normal;
            _detail.overflowMode = TextOverflowModes.Ellipsis;
            _detail.enableAutoSizing = false;
            _detail.fontSize = 16f;

            LayoutPreviewCard();
        }

        private void LayoutPreviewCard()
        {
            if (_previewFrame == null || _previewRoot == null)
            {
                return;
            }

            var w = _previewFrame.rect.width;
            var h = _previewFrame.rect.height;
            if (w < 8f || h < 8f)
            {
                return;
            }

            float cardW;
            float cardH;
            if (w / h > CardAspect)
            {
                cardH = h;
                cardW = h * CardAspect;
            }
            else
            {
                cardW = w;
                cardH = w / CardAspect;
            }

            _previewRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _previewRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _previewRoot.pivot = new Vector2(0.5f, 0.5f);
            _previewRoot.anchoredPosition = Vector2.zero;
            _previewRoot.sizeDelta = new Vector2(cardW, cardH);
            _previewRoot.localScale = Vector3.one;
        }

        private void Populate()
        {
            _cells.Clear();
            var all = _catalog.All;
            for (var i = 0; i < all.Count; i++)
            {
                var def = all[i];
                var cell = new GameObject(
                    def.CardId.Value,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                cell.transform.SetParent(_content, false);
                var plate = cell.GetComponent<Image>();
                plate.color = new Color(0.18f, 0.16f, 0.13f, 1f);
                plate.raycastTarget = false;

                var cardGo = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer));
                cardGo.transform.SetParent(cell.transform, false);
                Stretch((RectTransform)cardGo.transform, 3f);
                var view = cardGo.AddComponent<CardPlaceholderView>();
                var cost = def.CostPa.HasValue ? "PA " + def.CostPa.Value : "—";
                var power = def.Power.HasValue ? "P" + def.Power.Value : "—";
                view.SetFaceUp(
                    def.Name,
                    CardTypeCatalog.ToLabel(def.Type),
                    cost,
                    power,
                    def.CardId.Value,
                    def.EffectText);

                var captured = def;
                // Click the card itself — nested chrome used to steal clicks from a parent Button.
                view.Bind(() => ShowDetail(captured));
                view.SetInteractable(true);
                _cells.Add(view);
            }

            if (all.Count > 0)
            {
                ShowDetail(all[0]);
            }
        }

        private void ShowDetail(CardDefinition def)
        {
            if (def == null)
            {
                return;
            }

            _selectedCardId = def.CardId.Value;
            SyncSelectionHighlight();

            var cost = def.CostPa.HasValue ? "PA " + def.CostPa.Value : "—";
            var power = def.Power.HasValue ? "P" + def.Power.Value : "—";
            if (_preview != null)
            {
                _preview.SetFaceUp(
                    def.Name,
                    CardTypeCatalog.ToLabel(def.Type),
                    cost,
                    power,
                    def.CardId.Value,
                    def.EffectText);
                _preview.SetSelected(true);
                _preview.SetInteractable(false);
                DisableRaycasts(_preview.gameObject);
            }

            if (_detail != null)
            {
                var pa = def.CostPa.HasValue ? def.CostPa.Value.ToString() : "—";
                var pow = def.Power.HasValue ? def.Power.Value.ToString() : "—";
                // Compact meta only — rules already live on the large card.
                _detail.text =
                    def.CardId.Value + "  ·  " + CardTypeCatalog.ToLabel(def.Type) +
                    "  ·  PA " + pa + "  ·  Poder " + pow + "\n" +
                    "Raza: " + (string.IsNullOrEmpty(def.Race) ? "—" : def.Race) +
                    "   Clase: " + (string.IsNullOrEmpty(def.ClassPath) ? "—" : def.ClassPath) +
                    "   Rareza: " + (string.IsNullOrEmpty(def.Rarity) ? "—" : def.Rarity);
            }
        }

        private void SyncSelectionHighlight()
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell == null)
                {
                    continue;
                }

                var selected = !string.IsNullOrEmpty(_selectedCardId) &&
                               cell.transform.parent != null &&
                               cell.transform.parent.name == _selectedCardId;
                cell.SetSelected(selected);
            }
        }

        private void OnClose()
        {
            var cb = _onClose;
            Destroy(gameObject);
            cb?.Invoke();
        }

        private static void DisableRaycasts(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var graphics = root.GetComponentsInChildren<Graphic>(true);
            for (var i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }

            var button = root.GetComponent<Button>();
            if (button != null)
            {
                button.enabled = false;
            }
        }

        private static Button CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.90f, 0.83f, 0.70f, 1f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var tmp = CreateLabel(go.transform, "Label", label, 22f, TextAlignmentOptions.Center);
            Stretch(tmp.rectTransform);
            tmp.color = Color.black;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            return btn;
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

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
