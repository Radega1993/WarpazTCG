using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Face-up / face-down card chrome in a modern TCG layout (name plate, art window,
    /// type bar, rules box, cost/power gems). Hover enlarges hand cards; root Button owns clicks.
    /// </summary>
    public sealed class CardPlaceholderView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Regex Digits = new Regex(@"\d+", RegexOptions.Compiled);

        private const float SelectedScale = 1.08f;
        private const float HoverScaleHand = 1.12f;

        private Image _background;
        private Image _cardBack;
        private Image _border;
        private Image _innerRim;
        private Image _header;
        private Image _emptyContent;
        private Image _artLip;
        private Image _illustration;
        private Image _rulesPanel;
        private Image _typeBar;
        private Image _costGem;
        private Image _powerGem;
        private Image _selectedOverlay;
        private Image _legalOverlay;
        private TMP_Text _name;
        private TMP_Text _cost;
        private TMP_Text _costCaption;
        private TMP_Text _typeLine;
        private TMP_Text _rulesText;
        private TMP_Text _power;
        private Button _button;
        private System.Action _onClick;
        private bool _faceUp = true;
        private bool _selected;
        private bool _hovered;
        private string _cachedName = string.Empty;
        private string _cachedType = string.Empty;
        private string _cachedCost = string.Empty;
        private string _cachedExtra = string.Empty;
        private string _cachedRules = string.Empty;
        private string _cachedCardId;
        private Sprite _illustrationSprite;

        // Prefer faces: shift cover-crop downward so heads aren't clipped by the art frame.
        private const float IllustrationTopBias = 0.36f;

        private void Awake()
        {
            EnsureChrome();
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() =>
                {
                    SetHovered(false);
                    CardInspectOverlay.HideAll();
                    _onClick?.Invoke();
                });
            }
        }

        private void OnDisable()
        {
            if (_hovered)
            {
                SetHovered(false);
            }

            CardInspectOverlay.Hide(this);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_illustrationSprite != null)
            {
                LayoutIllustrationCover(_illustrationSprite);
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
            SetFaceUp(label, string.Empty, string.Empty, string.Empty, null);
            SetInteractable(false);
        }

        public void SetCard(string name, string type, string cost, string extra = null, string cardId = null, string rules = null)
        {
            SetFaceUp(name, type, cost, extra, cardId, rules);
        }

        public void SetFaceUp(
            string name,
            string type,
            string cost,
            string extra = null,
            string cardId = null,
            string rules = null)
        {
            EnsureChrome();
            _faceUp = true;
            _cachedName = name ?? string.Empty;
            _cachedType = LocalizeType(type);
            _cachedCost = cost ?? string.Empty;
            _cachedExtra = extra ?? string.Empty;
            _cachedRules = rules ?? string.Empty;
            _cachedCardId = cardId;
            ApplyFaceMode(true);
            if (_name != null)
            {
                _name.text = _cachedName;
            }

            if (_typeLine != null)
            {
                _typeLine.text = _cachedType;
            }

            ApplyCostText(_cachedCost);
            ApplyPowerText(_cachedExtra);

            if (_rulesText != null)
            {
                _rulesText.text = string.IsNullOrWhiteSpace(_cachedRules) ? string.Empty : _cachedRules.Trim();
            }

            ApplyBodyLayout();
            ApplyIllustration(cardId);
            ApplyPresentationScale();
        }

        public void SetFaceDown()
        {
            EnsureChrome();
            _faceUp = false;
            ApplyFaceMode(false);
            ClearIllustration();
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

            if (_costCaption != null)
            {
                _costCaption.text = string.Empty;
            }

            if (_power != null)
            {
                _power.text = string.Empty;
            }

            if (_rulesText != null)
            {
                _rulesText.text = string.Empty;
            }

            ApplyPresentationScale();
        }

        public void SetSelected(bool selected)
        {
            EnsureChrome();
            _selected = selected;
            if (_selectedOverlay != null)
            {
                _selectedOverlay.gameObject.SetActive(selected);
            }

            if (_border != null)
            {
                _border.color = selected ? BattleUiColors.SelectedBorder : BattleUiColors.CardBorder;
            }

            ApplyPresentationScale();
        }

        public void SetHovered(bool hovered)
        {
            EnsureChrome();
            // Board / piles: never hover-zoom — nested scale + canvas stole slot clicks.
            if (hovered && !CanShowHandInspect())
            {
                hovered = false;
            }

            if (_hovered == hovered)
            {
                ApplyPresentationScale();
                SyncInspectOverlay();
                return;
            }

            _hovered = hovered;
            ApplyPresentationScale();
            SyncInspectOverlay();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHovered(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false);
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
            // Plate fill (solid or sliced chrome) — not card illustration art.
            EnsureChrome();
            return _background != null &&
                   _background.enabled &&
                   (_illustration == null ||
                    !_illustration.gameObject.activeSelf ||
                    _illustration.sprite == null);
        }

        public bool HasIllustrationSprite()
        {
            EnsureChrome();
            return _illustration != null &&
                   _illustration.sprite != null &&
                   _illustration.gameObject.activeSelf;
        }

        public bool IsHoveredForTests => _hovered;

        private void ApplyIllustration(string cardId)
        {
            EnsureChrome();
            if (_illustration == null)
            {
                return;
            }

            var sprite = CardIllustrationCatalog.TryGet(cardId);
            if (sprite == null)
            {
                ClearIllustration();
                return;
            }

            _illustrationSprite = sprite;
            _illustration.gameObject.SetActive(true);
            _illustration.enabled = true;
            _illustration.sprite = sprite;
            _illustration.color = Color.white;
            _illustration.type = Image.Type.Simple;
            _illustration.raycastTarget = false;
            LayoutIllustrationCover(sprite);
            if (_emptyContent != null)
            {
                _emptyContent.color = BattleUiColors.FaceUpEmptyContent;
            }

            // Keep rules box visible — modern TCGs always show a text window under art.
            if (_rulesPanel != null)
            {
                _rulesPanel.gameObject.SetActive(_faceUp);
            }
        }

        private void ClearIllustration()
        {
            _illustrationSprite = null;
            if (_illustration != null)
            {
                _illustration.sprite = null;
                _illustration.enabled = false;
                _illustration.gameObject.SetActive(false);
            }

            if (_emptyContent != null)
            {
                _emptyContent.color = BattleUiColors.FaceUpEmptyContent;
            }

            if (_faceUp && _rulesPanel != null)
            {
                _rulesPanel.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Portrait WZ1 art (≈2:3) must cover the landscape content window; RectMask2D crops overflow.
        /// </summary>
        private void LayoutIllustrationCover(Sprite sprite)
        {
            if (_illustration == null || _emptyContent == null || sprite == null)
            {
                return;
            }

            var window = _emptyContent.rectTransform;
            var rt = _illustration.rectTransform;
            if (rt.parent != window)
            {
                rt.SetParent(window, false);
            }

            if (_emptyContent.GetComponent<RectMask2D>() == null)
            {
                _emptyContent.gameObject.AddComponent<RectMask2D>();
            }

            Canvas.ForceUpdateCanvases();
            var w = window.rect.width;
            var h = window.rect.height;
            if (w < 1f || h < 1f)
            {
                LayoutRegion(rt, 0f, 0f, 1f, 1f, 0f);
                _illustration.preserveAspect = false;
                return;
            }

            var spriteAspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            var parentAspect = w / h;
            float drawW;
            float drawH;
            if (spriteAspect > parentAspect)
            {
                drawH = h;
                drawW = h * spriteAspect;
            }
            else
            {
                drawW = w;
                drawH = w / spriteAspect;
            }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // Negative Y moves the sprite down → more of the top (heads) stays in frame.
            var overflowY = Mathf.Max(0f, drawH - h);
            rt.anchoredPosition = new Vector2(0f, -overflowY * IllustrationTopBias);
            rt.sizeDelta = new Vector2(drawW, drawH);
            rt.localScale = Vector3.one;
            _illustration.preserveAspect = false;
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

            var rootMask = GetComponent<RectMask2D>();
            if (rootMask != null)
            {
                DestroyComponent(rootMask);
            }

            var rounded = CardChromeSprites.RoundedRect();
            var circle = CardChromeSprites.Circle();

            // Layer order (back → front): gold rim, inner lip, dark plate.
            // Keep node name GrayBackground for existing tests / scene wiring.
            _border = EnsureImage("Border", BattleUiColors.CardBorder, false, 0);
            ApplySliced(_border, rounded, BattleUiColors.CardBorder);
            LayoutRegion(_border.rectTransform, 0f, 0f, 1f, 1f, 0f);

            _innerRim = EnsureImage("InnerRim", BattleUiColors.CardBorderInner, false, 1);
            ApplySliced(_innerRim, rounded, BattleUiColors.CardBorderInner);
            LayoutRegion(_innerRim.rectTransform, 0f, 0f, 1f, 1f, 3f);
            _innerRim.raycastTarget = false;

            _background = EnsureImage("GrayBackground", BattleUiColors.FaceUpBackground, true, 2);
            ApplySliced(_background, rounded, BattleUiColors.FaceUpBackground);
            LayoutRegion(_background.rectTransform, 0f, 0f, 1f, 1f, 6f);
            _background.raycastTarget = true;

            _cardBack = EnsureImage("CardBack", Color.white, false, 3);
            LayoutRegion(_cardBack.rectTransform, 0f, 0f, 1f, 1f, 0f);
            ApplyCardBackSprite();

            _header = EnsureImage("Header", BattleUiColors.FaceUpHeader, false, 4);
            ApplySliced(_header, rounded, BattleUiColors.FaceUpHeader);

            _artLip = EnsureImage("ArtLip", BattleUiColors.CardBorder, false, 4);
            ApplySliced(_artLip, rounded, BattleUiColors.CardBorder);

            _emptyContent = EnsureImage("EmptyContentArea", BattleUiColors.FaceUpEmptyContent, false, 5);
            if (_emptyContent.GetComponent<RectMask2D>() == null)
            {
                _emptyContent.gameObject.AddComponent<RectMask2D>();
            }

            if (_emptyContent != null)
            {
                var nested = _emptyContent.transform.Find("Illustration");
                if (nested != null)
                {
                    _illustration = nested.GetComponent<Image>() ?? nested.gameObject.AddComponent<Image>();
                }
            }

            if (_illustration == null)
            {
                _illustration = EnsureImage("Illustration", Color.white, false, 5);
            }

            if (_illustration.transform.parent != _emptyContent.transform)
            {
                _illustration.transform.SetParent(_emptyContent.transform, false);
            }

            _illustration.raycastTarget = false;

            _typeBar = EnsureImage("TypeBar", BattleUiColors.TypeBar, false, 5);
            ApplySliced(_typeBar, rounded, BattleUiColors.TypeBar);

            _rulesPanel = EnsureImage("RulesPanel", BattleUiColors.FaceUpRules, false, 5);
            ApplySliced(_rulesPanel, rounded, BattleUiColors.FaceUpRules);

            _costGem = EnsureImage("CostGem", BattleUiColors.CostGem, false, 6);
            _costGem.sprite = circle;
            _costGem.type = Image.Type.Simple;
            _costGem.color = BattleUiColors.CostGem;
            _costGem.preserveAspect = true;
            EnsureOutline(_costGem.gameObject, BattleUiColors.CardBorder, 2f);

            _powerGem = EnsureImage("PowerGem", BattleUiColors.PowerGem, false, 6);
            _powerGem.sprite = circle;
            _powerGem.type = Image.Type.Simple;
            _powerGem.color = BattleUiColors.PowerGem;
            _powerGem.preserveAspect = true;
            EnsureOutline(_powerGem.gameObject, BattleUiColors.CardBorder, 2.5f);

            ApplyBodyLayout();
            if (_illustration.sprite != null)
            {
                LayoutIllustrationCover(_illustration.sprite);
            }
            else
            {
                LayoutRegion(_illustration.rectTransform, 0f, 0f, 1f, 1f, 0f);
                _illustration.enabled = false;
            }

            _name = EnsureTmp("Header/Name", 17f, TextAlignmentOptions.Left, BattleUiColors.CardText);
            LayoutRegion(_name.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f, 0f);
            _name.transform.SetParent(_header.transform, false);
            CardTypography.ApplyName(_name);

            _costCaption = EnsureTmp("CostGem/Caption", 9f, TextAlignmentOptions.Center, BattleUiColors.CardTextOnDark);
            LayoutRegion(_costCaption.rectTransform, 0.1f, 0.62f, 0.9f, 0.88f, 0f);
            _costCaption.transform.SetParent(_costGem.transform, false);
            CardTypography.ApplyCaption(_costCaption);

            _cost = EnsureTmp("CostGem/Value", 22f, TextAlignmentOptions.Center, BattleUiColors.CardTextOnDark);
            LayoutRegion(_cost.rectTransform, 0.08f, 0.08f, 0.92f, 0.68f, 0f);
            _cost.transform.SetParent(_costGem.transform, false);
            CardTypography.ApplyStat(_cost);

            // Legacy Header/Cost path — hide if present from older chrome.
            var legacyCost = transform.Find("Header/Cost");
            if (legacyCost != null && legacyCost != _cost.transform)
            {
                legacyCost.gameObject.SetActive(false);
            }

            _typeLine = EnsureTmp("TypeBar/TypeLine", 12f, TextAlignmentOptions.Center, BattleUiColors.CardTextOnDark);
            LayoutRegion(_typeLine.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f, 0f);
            _typeLine.transform.SetParent(_typeBar.transform, false);
            CardTypography.ApplyTypeLine(_typeLine);

            // Hide old root TypeLine if leftover.
            var legacyType = transform.Find("TypeLine");
            if (legacyType != null && legacyType != _typeLine.transform)
            {
                legacyType.gameObject.SetActive(false);
            }

            _rulesText = EnsureTmp("RulesPanel/RulesText", 11f, TextAlignmentOptions.TopLeft, BattleUiColors.CardText);
            LayoutRegion(_rulesText.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f, 0f);
            _rulesText.transform.SetParent(_rulesPanel.transform, false);
            CardTypography.ApplyRules(_rulesText);

            _power = EnsureTmp("PowerGem/Power", 22f, TextAlignmentOptions.Center, BattleUiColors.CardTextOnDark);
            LayoutRegion(_power.rectTransform, 0.05f, 0.05f, 0.95f, 0.95f, 0f);
            _power.transform.SetParent(_powerGem.transform, false);
            CardTypography.ApplyStat(_power);

            // Hide legacy StatsPanel from older chrome.
            var legacyStats = transform.Find("StatsPanel");
            if (legacyStats != null)
            {
                legacyStats.gameObject.SetActive(false);
            }

            _selectedOverlay = EnsureImage("SelectedOverlay", BattleUiColors.SelectionOverlay, false, 8);
            ApplySliced(_selectedOverlay, rounded, BattleUiColors.SelectionOverlay);
            _selectedOverlay.gameObject.SetActive(false);
            LayoutRegion(_selectedOverlay.rectTransform, 0f, 0f, 1f, 1f, 0f);

            _legalOverlay = EnsureImage("LegalOverlay", BattleUiColors.LegalOverlay, false, 7);
            ApplySliced(_legalOverlay, rounded, BattleUiColors.LegalOverlay);
            _legalOverlay.gameObject.SetActive(false);
            LayoutRegion(_legalOverlay.rectTransform, 0f, 0f, 1f, 1f, 0f);

            _button = GetComponent<Button>();
            if (_button == null)
            {
                _button = gameObject.AddComponent<Button>();
            }

            _button.targetGraphic = _background;
            _button.transition = Selectable.Transition.None;

            var legacy = transform.Find("Text");
            if (legacy != null)
            {
                legacy.gameObject.SetActive(false);
            }

            ApplyFaceMode(_faceUp);
            EnforceAspect(rootRt);
            ApplyPresentationScale();
        }

        private void ApplyPresentationScale()
        {
            var rt = transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            var leftoverCanvas = GetComponent<Canvas>();
            if (leftoverCanvas != null && leftoverCanvas.overrideSorting)
            {
                leftoverCanvas.overrideSorting = false;
            }

            var leftoverRaycaster = GetComponent<GraphicRaycaster>();
            if (leftoverRaycaster != null)
            {
                DestroyComponent(leftoverRaycaster);
            }

            if (leftoverCanvas != null)
            {
                DestroyComponent(leftoverCanvas);
            }

            float scale = 1f;
            if (_hovered && CanShowHandInspect())
            {
                scale = HoverScaleHand;
            }
            else if (_selected)
            {
                scale = SelectedScale;
            }

            rt.localScale = new Vector3(scale, scale, 1f);
        }

        private bool CanShowHandInspect()
        {
            return _faceUp && IsUnderName(BattleLayoutBuilder.LocalHandName);
        }

        private bool IsUnderName(string name)
        {
            var t = transform;
            while (t != null)
            {
                if (t.name == name)
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        private void SyncInspectOverlay()
        {
            if (_hovered && CanShowHandInspect())
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    CardInspectOverlay.Ensure(canvas.transform);
                }

                CardInspectOverlay.Show(
                    this,
                    _cachedName,
                    _cachedType,
                    _cachedCost,
                    _cachedExtra,
                    _cachedCardId,
                    _cachedRules);
                return;
            }

            CardInspectOverlay.Hide(this);
        }

        private void ApplyFaceMode(bool faceUp)
        {
            var backSprite = CardBackCatalog.TryGet();
            var hasBack = !faceUp && backSprite != null;

            if (_background != null)
            {
                _background.gameObject.SetActive(true);
                _background.raycastTarget = true;
                if (faceUp)
                {
                    ApplySliced(_background, CardChromeSprites.RoundedRect(), BattleUiColors.FaceUpBackground);
                }
                else if (hasBack)
                {
                    _background.color = new Color(0f, 0f, 0f, 0f);
                }
                else
                {
                    ApplySliced(_background, CardChromeSprites.RoundedRect(), BattleUiColors.FaceDownBackground);
                }
            }

            SetActive(_border, faceUp);
            SetActive(_innerRim, faceUp);
            SetActive(_header, faceUp);
            SetActive(_artLip, faceUp);
            SetActive(_emptyContent, faceUp);
            SetActive(_typeBar, faceUp);
            SetActive(_rulesPanel, faceUp);
            SetActive(_costGem, faceUp);
            SetActive(_powerGem, faceUp);

            if (_cardBack != null)
            {
                ApplyCardBackSprite();
                _cardBack.gameObject.SetActive(hasBack);
                _cardBack.enabled = hasBack;
                _cardBack.raycastTarget = false;
                if (hasBack)
                {
                    _cardBack.transform.SetSiblingIndex(Mathf.Max(0, transform.childCount - 3));
                }
            }

            if (_illustration != null && !faceUp)
            {
                _illustration.gameObject.SetActive(false);
            }

            if (_typeLine != null)
            {
                _typeLine.gameObject.SetActive(faceUp);
            }

            if (_legalOverlay != null)
            {
                _legalOverlay.transform.SetAsLastSibling();
            }

            if (_selectedOverlay != null)
            {
                _selectedOverlay.transform.SetAsLastSibling();
            }
        }

        private void ApplyCardBackSprite()
        {
            if (_cardBack == null)
            {
                return;
            }

            var sprite = CardBackCatalog.TryGet();
            _cardBack.sprite = sprite;
            _cardBack.type = Image.Type.Simple;
            _cardBack.preserveAspect = false;
            _cardBack.color = Color.white;
            _cardBack.raycastTarget = false;
            LayoutRegion(_cardBack.rectTransform, 0f, 0f, 1f, 1f, 0f);
        }

        /// <summary>
        /// Art / type / rules / gems sized to content — short rules shrink so art grows.
        /// </summary>
        private void ApplyBodyLayout()
        {
            if (_header == null ||
                _emptyContent == null ||
                _rulesPanel == null ||
                _typeBar == null ||
                _artLip == null ||
                _costGem == null ||
                _powerGem == null)
            {
                return;
            }

            var rules = string.IsNullOrWhiteSpace(_cachedRules) ? string.Empty : _cachedRules.Trim();
            float rulesH;
            if (rules.Length == 0)
            {
                rulesH = 0.07f;
            }
            else if (rules.Length < 50)
            {
                rulesH = 0.10f;
            }
            else if (rules.Length < 110)
            {
                rulesH = 0.13f;
            }
            else if (rules.Length < 180)
            {
                rulesH = 0.16f;
            }
            else
            {
                rulesH = 0.19f;
            }

            const float rulesBottom = 0.035f;
            var rulesTop = rulesBottom + rulesH;
            const float typeH = 0.038f;
            var typeBottom = rulesTop + 0.008f;
            var typeTop = typeBottom + typeH;
            var artBottom = typeTop + 0.012f;
            const float artTop = 0.855f;
            const float nameBottom = 0.875f;
            const float nameTop = 0.975f;

            LayoutRegion(_header.rectTransform, 0.05f, nameBottom, 0.78f, nameTop, 0f);
            LayoutRegion(_costGem.rectTransform, 0.78f, 0.86f, 0.97f, 0.985f, 0f);

            LayoutRegion(_artLip.rectTransform, 0.045f, artBottom - 0.012f, 0.955f, artTop + 0.01f, 0f);
            LayoutRegion(_emptyContent.rectTransform, 0.06f, artBottom, 0.94f, artTop, 0f);
            LayoutRegion(_typeBar.rectTransform, 0.06f, typeBottom, 0.94f, typeTop, 0f);
            LayoutRegion(_rulesPanel.rectTransform, 0.06f, rulesBottom, 0.72f, rulesTop, 0f);

            // Power gem sits on the bottom-right corner, sized to the rules band.
            var gemBottom = Mathf.Max(0.02f, rulesBottom - 0.01f);
            var gemTop = Mathf.Min(0.24f, rulesTop + 0.02f);
            LayoutRegion(_powerGem.rectTransform, 0.74f, gemBottom, 0.97f, gemTop, 0f);
        }

        private void ApplyCostText(string raw)
        {
            var value = ExtractPrimaryToken(raw);
            if (_cost != null)
            {
                _cost.text = value;
            }

            if (_costCaption != null)
            {
                _costCaption.text = string.IsNullOrEmpty(value) || value == "—" ? string.Empty : "PA";
            }
        }

        private void ApplyPowerText(string raw)
        {
            if (_power == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                _power.text = "—";
                return;
            }

            var match = Digits.Match(raw);
            if (match.Success)
            {
                var suffix = raw.IndexOf('R') >= 0 ? "R" : string.Empty;
                _power.text = match.Value + suffix;
            }
            else
            {
                _power.text = raw.Replace("P", string.Empty).Trim();
            }
        }

        private static string ExtractPrimaryToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "—";
            }

            var match = Digits.Match(raw);
            return match.Success ? match.Value : raw.Trim();
        }

        private static string LocalizeType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return string.Empty;
            }

            if (System.Enum.TryParse(type, true, out CardType parsed))
            {
                return CardTypeCatalog.ToLabel(parsed);
            }

            return type;
        }

        private static void ApplySliced(Image image, Sprite sprite, Color color)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.pixelsPerUnitMultiplier = 1.15f;
        }

        private static void EnsureOutline(GameObject go, Color color, float distance)
        {
            if (go == null)
            {
                return;
            }

            var outline = go.GetComponent<Outline>();
            if (outline == null)
            {
                outline = go.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static void SetActive(Component c, bool active)
        {
            if (c != null)
            {
                c.gameObject.SetActive(active);
            }
        }

        private static void EnforceAspect(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            var fitter = rt.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                DestroyComponent(fitter);
            }
        }

        private static void DestroyComponent(Component c)
        {
            if (c == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Object.Destroy(c);
            }
            else
            {
                Object.DestroyImmediate(c);
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

            // Keep assigned illustration / card-back / gem sprites; other chrome may be sliced.
            if (go.name != "Illustration" && go.name != "CardBack" && go.name != "CostGem" && go.name != "PowerGem")
            {
                if (image.sprite == null)
                {
                    image.color = color;
                }
            }
            else if (image.sprite == null)
            {
                image.color = color;
            }

            image.raycastTarget = raycast;
            t.SetSiblingIndex(Mathf.Min(sibling, t.parent.childCount - 1));
            return image;
        }

        private TMP_Text EnsureTmp(string path, float fontSize, TextAlignmentOptions align, Color color)
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
                        var direct = transform.Find(parentPath);
                        if (direct != null)
                        {
                            parent = direct;
                        }
                        else
                        {
                            var pgo = new GameObject(
                                parentPath.Contains("/")
                                    ? parentPath.Substring(parentPath.LastIndexOf('/') + 1)
                                    : parentPath,
                                typeof(RectTransform));
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
            tmp.fontSizeMin = 8f;
            tmp.fontSizeMax = fontSize;
            tmp.enableAutoSizing = true;
            tmp.alignment = align;
            tmp.color = color;
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
