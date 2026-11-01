using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Grey face-up / face-down card chrome. No illustration sprites. Root Button owns clicks.
    /// </summary>
    public sealed class CardPlaceholderView : MonoBehaviour
    {
        private Image _background;
        private Image _border;
        private Image _header;
        private Image _emptyContent;
        private Image _rulesPanel;
        private Image _selectedOverlay;
        private Image _legalOverlay;
        private TMP_Text _name;
        private TMP_Text _cost;
        private TMP_Text _typeLine;
        private TMP_Text _rulesText;
        private TMP_Text _power;
        private Button _button;
        private System.Action _onClick;
        private bool _faceUp = true;

        private void Awake()
        {
            EnsureChrome();
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _onClick?.Invoke());
            }
        }

        public void Bind(System.Action onClick)
        {
            EnsureChrome();
            _onClick = onClick;
        }

        public void SetInteractable(bool interactable)
        {
            EnsureChrome();
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        public void SetBlocked(string label)
        {
            SetFaceUp(label, string.Empty, string.Empty, string.Empty);
            SetInteractable(false);
        }

        public void SetCard(string name, string type, string cost, string extra = null)
        {
            SetFaceUp(name, type, cost, extra);
        }

        public void SetFaceUp(string name, string type, string cost, string extra = null)
        {
            EnsureChrome();
            _faceUp = true;
            ApplyFaceMode(true);
            if (_name != null)
            {
                _name.text = name ?? string.Empty;
            }

            if (_typeLine != null)
            {
                _typeLine.text = type ?? string.Empty;
            }

            if (_cost != null)
            {
                _cost.text = cost ?? string.Empty;
            }

            if (_power != null)
            {
                _power.text = extra ?? string.Empty;
            }

            if (_rulesText != null)
            {
                _rulesText.text = string.Empty;
            }
        }

        public void SetFaceDown()
        {
            EnsureChrome();
            _faceUp = false;
            ApplyFaceMode(false);
            if (_name != null)
            {
                _name.text = string.Empty;
            }

            if (_typeLine != null)
            {
                _typeLine.text = string.Empty;
            }

            if (_cost != null)
            {
                _cost.text = string.Empty;
            }

            if (_power != null)
            {
                _power.text = string.Empty;
            }

            if (_rulesText != null)
            {
                _rulesText.text = string.Empty;
            }
        }

        public void SetSelected(bool selected)
        {
            EnsureChrome();
            if (_selectedOverlay != null)
            {
                _selectedOverlay.gameObject.SetActive(selected);
            }

            if (_border != null)
            {
                _border.color = selected ? BattleUiColors.SelectedBorder : BattleUiColors.CardBorder;
                _border.pixelsPerUnitMultiplier = selected ? 0.5f : 1f;
            }

            var rt = transform as RectTransform;
            if (rt != null)
            {
                rt.localScale = selected ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
            }
        }

        public void SetPlayable(bool playable)
        {
            EnsureChrome();
            if (_legalOverlay != null)
            {
                _legalOverlay.gameObject.SetActive(playable && _faceUp);
            }

            if (!playable || !_faceUp)
            {
                return;
            }

            if (_border != null && (_selectedOverlay == null || !_selectedOverlay.gameObject.activeSelf))
            {
                _border.color = BattleUiColors.PlayableBorder;
            }
        }

        public bool HasGreyBackgroundImage()
        {
            EnsureChrome();
            return _background != null && _background.sprite == null;
        }

        public string GetVisibleTextSnapshot()
        {
            EnsureChrome();
            return (_name != null ? _name.text : string.Empty) + "|" +
                   (_cost != null ? _cost.text : string.Empty) + "|" +
                   (_typeLine != null ? _typeLine.text : string.Empty) + "|" +
                   (_power != null ? _power.text : string.Empty);
        }

        public void EnsureChrome()
        {
            var rootRt = GetComponent<RectTransform>();
            if (rootRt == null)
            {
                return;
            }

            if (GetComponent<RectMask2D>() == null)
            {
                gameObject.AddComponent<RectMask2D>();
            }

            _background = EnsureImage("GrayBackground", BattleUiColors.FaceUpBackground, false, 0);
            _border = EnsureImage("Border", BattleUiColors.CardBorder, false, 1);
            _header = EnsureImage("Header", BattleUiColors.FaceUpHeader, false, 2);
            _emptyContent = EnsureImage("EmptyContentArea", BattleUiColors.FaceUpEmptyContent, false, 3);
            _rulesPanel = EnsureImage("RulesPanel", BattleUiColors.FaceUpRules, false, 4);

            LayoutRegion(_background.rectTransform, 0f, 0f, 1f, 1f, 2f);
            LayoutRegion(_border.rectTransform, 0f, 0f, 1f, 1f, 0f);
            _border.color = new Color(
                BattleUiColors.CardBorder.r,
                BattleUiColors.CardBorder.g,
                BattleUiColors.CardBorder.b,
                1f);
            // Border as rim: use outline via slightly inset background; keep full plate under border tint.
            LayoutRegion(_header.rectTransform, 0.04f, 0.78f, 0.96f, 0.96f, 0f);
            LayoutRegion(_emptyContent.rectTransform, 0.06f, 0.36f, 0.94f, 0.76f, 0f);
            LayoutRegion(_rulesPanel.rectTransform, 0.06f, 0.06f, 0.94f, 0.34f, 0f);

            _name = EnsureTmp("Header/Name", 18f, TextAlignmentOptions.Left);
            _cost = EnsureTmp("Header/Cost", 18f, TextAlignmentOptions.Right);
            LayoutRegion(_name.rectTransform, 0.05f, 0.1f, 0.72f, 0.9f, 0f);
            LayoutRegion(_cost.rectTransform, 0.72f, 0.1f, 0.95f, 0.9f, 0f);
            _name.transform.SetParent(_header.transform, false);
            _cost.transform.SetParent(_header.transform, false);

            _typeLine = EnsureTmp("TypeLine", 14f, TextAlignmentOptions.Center);
            LayoutRegion(_typeLine.rectTransform, 0.06f, 0.70f, 0.94f, 0.78f, 0f);

            _rulesText = EnsureTmp("RulesPanel/RulesText", 12f, TextAlignmentOptions.TopLeft);
            _rulesText.textWrappingMode = TextWrappingModes.Normal;
            _rulesText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutRegion(_rulesText.rectTransform, 0.05f, 0.05f, 0.95f, 0.95f, 0f);
            _rulesText.transform.SetParent(_rulesPanel.transform, false);

            _power = EnsureTmp("StatsPanel/Power", 16f, TextAlignmentOptions.Center);
            var stats = EnsureImage("StatsPanel", BattleUiColors.FaceUpRules, false, 5);
            LayoutRegion(stats.rectTransform, 0.62f, 0.08f, 0.92f, 0.22f, 0f);
            _power.transform.SetParent(stats.transform, false);
            LayoutRegion(_power.rectTransform, 0f, 0f, 1f, 1f, 2f);

            _selectedOverlay = EnsureImage("SelectedOverlay", BattleUiColors.SelectionOverlay, false, 8);
            _selectedOverlay.gameObject.SetActive(false);
            LayoutRegion(_selectedOverlay.rectTransform, 0f, 0f, 1f, 1f, 0f);

            _legalOverlay = EnsureImage("LegalOverlay", BattleUiColors.LegalOverlay, false, 7);
            _legalOverlay.gameObject.SetActive(false);
            LayoutRegion(_legalOverlay.rectTransform, 0f, 0f, 1f, 1f, 0f);

            _button = GetComponent<Button>();
            if (_button == null)
            {
                _button = gameObject.AddComponent<Button>();
            }

            _button.targetGraphic = _background;
            _button.transition = Selectable.Transition.None;

            // Hide legacy single Text node if present.
            var legacy = transform.Find("Text");
            if (legacy != null)
            {
                legacy.gameObject.SetActive(false);
            }

            ApplyFaceMode(_faceUp);
            EnforceAspect(rootRt);
        }

        private void ApplyFaceMode(bool faceUp)
        {
            if (_background != null)
            {
                _background.color = faceUp ? BattleUiColors.FaceUpBackground : BattleUiColors.FaceDownBackground;
            }

            if (_header != null)
            {
                _header.gameObject.SetActive(faceUp);
            }

            if (_emptyContent != null)
            {
                _emptyContent.gameObject.SetActive(faceUp);
            }

            if (_rulesPanel != null)
            {
                _rulesPanel.gameObject.SetActive(faceUp);
            }

            if (_typeLine != null)
            {
                _typeLine.gameObject.SetActive(faceUp);
            }

            if (_power != null)
            {
                _power.transform.parent.gameObject.SetActive(faceUp);
            }
        }

        private static void EnforceAspect(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            // Prefer parent-driven cell size; AspectRatioFitter fights board/hand anchors.
            var fitter = rt.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                Object.Destroy(fitter);
            }
        }

        private Image EnsureImage(string path, Color color, bool raycast, int sibling)
        {
            var t = transform.Find(path);
            GameObject go;
            if (t == null)
            {
                var leaf = path;
                var slash = path.LastIndexOf('/');
                Transform parent = transform;
                if (slash >= 0)
                {
                    var parentPath = path.Substring(0, slash);
                    leaf = path.Substring(slash + 1);
                    var pt = transform.Find(parentPath);
                    if (pt == null)
                    {
                        go = new GameObject(parentPath, typeof(RectTransform));
                        go.transform.SetParent(transform, false);
                        pt = go.transform;
                    }

                    parent = pt;
                }

                go = new GameObject(leaf, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            else
            {
                go = t.gameObject;
            }

            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            image.sprite = null;
            image.color = color;
            image.raycastTarget = raycast;
            t.SetSiblingIndex(Mathf.Min(sibling, t.parent.childCount - 1));
            return image;
        }

        private TMP_Text EnsureTmp(string path, float fontSize, TextAlignmentOptions align)
        {
            var t = transform.Find(path);
            GameObject go;
            if (t == null)
            {
                var leaf = path;
                Transform parent = transform;
                var slash = path.LastIndexOf('/');
                if (slash >= 0)
                {
                    var parentPath = path.Substring(0, slash);
                    leaf = path.Substring(slash + 1);
                    var pt = transform.Find(parentPath);
                    if (pt == null)
                    {
                        var pgo = new GameObject(parentPath.Contains("/")
                            ? parentPath.Substring(parentPath.LastIndexOf('/') + 1)
                            : parentPath, typeof(RectTransform));
                        // Prefer existing Header/RulesPanel/StatsPanel parents created by EnsureImage.
                        var direct = transform.Find(parentPath);
                        if (direct != null)
                        {
                            parent = direct;
                        }
                        else
                        {
                            pgo.transform.SetParent(transform, false);
                            parent = pgo.transform;
                        }
                    }
                    else
                    {
                        parent = pt;
                    }
                }

                go = new GameObject(leaf, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            else
            {
                go = t.gameObject;
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            tmp.fontSize = fontSize;
            tmp.fontSizeMin = 10f;
            tmp.fontSizeMax = fontSize;
            tmp.enableAutoSizing = true;
            tmp.alignment = align;
            tmp.color = BattleUiColors.CardText;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private static void LayoutRegion(RectTransform rt, float xMin, float yMin, float xMax, float yMax, float inset)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            rt.localScale = Vector3.one;
        }
    }
}
