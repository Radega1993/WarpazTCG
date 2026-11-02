using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Presentation-only character board slot. Receives visual state from BoardSlotsView / presenter.
    /// Does not read GameState or decide legality.
    /// </summary>
    public sealed class CharacterSlotView : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private RectTransform _cardContainer;
        [SerializeField] private Image _interaction;
        [SerializeField] private Button _button;
        [SerializeField] private Image _selectionOverlay;
        [SerializeField] private Image _legalTargetOverlay;
        [SerializeField] private Image _statusOverlay;
        [SerializeField] private CardPlaceholderView _cardView;
        [SerializeField] private TMP_Text _roleLabel;

        private System.Action _onClick;

        public Image Frame => _frame;
        public RectTransform CardContainer => _cardContainer;
        public CardPlaceholderView CardView => _cardView;

        private void Awake()
        {
            EnsureWired();
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _onClick?.Invoke());
            }
        }

        public void Bind(System.Action onClick)
        {
            _onClick = onClick;
        }

        public void SetInteractable(bool interactable)
        {
            EnsureWired();
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        public void SetEmpty(string row, int index)
        {
            EnsureWired();
            if (_cardView != null)
            {
                // Empty slot = role label inside ornate frame (never a blank card plate).
                _cardView.gameObject.SetActive(false);
                _cardView.SetSelected(false);
                _cardView.SetPlayable(false);
            }

            if (_cardContainer != null)
            {
                _cardContainer.gameObject.SetActive(false);
            }

            if (_frame != null)
            {
                _frame.gameObject.SetActive(true);
                _frame.color = Color.white;
            }

            if (_roleLabel != null)
            {
                var role = string.IsNullOrEmpty(row) ? "Casilla" : row;
                // index 0 = left-most of that row band; keep label as row type only.
                _roleLabel.text = role;
                _roleLabel.gameObject.SetActive(true);
            }
        }

        public void SetCard(
            string name,
            string type,
            string cost,
            string extra = null,
            string cardId = null,
            string rules = null)
        {
            EnsureWired();
            if (_roleLabel != null)
            {
                _roleLabel.gameObject.SetActive(false);
            }

            if (_cardContainer != null)
            {
                _cardContainer.gameObject.SetActive(true);
            }

            if (_cardView != null)
            {
                _cardView.gameObject.SetActive(true);
                _cardView.SetFaceUp(name, type, cost, extra, cardId, rules);
            }

            if (_frame != null)
            {
                // Keep frame behind the card so illustrations are visible.
                _frame.gameObject.SetActive(true);
                _frame.transform.SetAsFirstSibling();
                if (_cardContainer != null)
                {
                    _cardContainer.SetSiblingIndex(1);
                }
            }
        }

        public void SetSelected(bool selected)
        {
            EnsureWired();
            if (_selectionOverlay != null)
            {
                _selectionOverlay.gameObject.SetActive(selected);
            }

            if (_cardView != null)
            {
                _cardView.SetSelected(selected);
            }
        }

        public void SetLegalTarget(bool legal)
        {
            EnsureWired();
            if (_legalTargetOverlay != null)
            {
                _legalTargetOverlay.gameObject.SetActive(legal);
            }

            if (_cardView != null)
            {
                _cardView.SetPlayable(legal);
            }
        }

        public void SetExhausted(bool exhausted)
        {
            EnsureWired();
            if (_statusOverlay != null)
            {
                _statusOverlay.gameObject.SetActive(exhausted);
            }

            if (_cardContainer != null)
            {
                _cardContainer.localEulerAngles = exhausted
                    ? new Vector3(0f, 0f, -12f)
                    : Vector3.zero;
            }

            if (_frame != null)
            {
                _frame.color = exhausted
                    ? new Color(0.65f, 0.65f, 0.7f, 1f)
                    : Color.white;
            }
        }

        public void ClearVisualState()
        {
            SetSelected(false);
            SetLegalTarget(false);
            SetExhausted(false);
        }

        public void ApplyFrameSprite(Sprite sprite)
        {
            EnsureWired();
            if (_frame != null && sprite != null)
            {
                _frame.sprite = sprite;
                _frame.type = Image.Type.Simple;
                _frame.preserveAspect = true;
                _frame.raycastTarget = false;
            }
        }

        public void EnsureWired()
        {
            if (_frame == null)
            {
                _frame = EnsureChildImage("Frame", new Color(1f, 1f, 1f, 1f), false);
            }

            if (_cardContainer == null)
            {
                var t = transform.Find("CardContainer");
                if (t == null)
                {
                    var go = new GameObject("CardContainer", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    Stretch(go.GetComponent<RectTransform>(), 10f);
                    t = go.transform;
                }

                _cardContainer = t as RectTransform;
            }

            if (_cardView == null)
            {
                var cardT = _cardContainer.Find("FaceUpCard");
                if (cardT == null)
                {
                    var go = new GameObject("FaceUpCard", typeof(RectTransform), typeof(CanvasRenderer));
                    go.transform.SetParent(_cardContainer, false);
                    Stretch(go.GetComponent<RectTransform>(), 0f);
                    cardT = go.transform;
                }

                _cardView = cardT.GetComponent<CardPlaceholderView>();
                if (_cardView == null)
                {
                    _cardView = cardT.gameObject.AddComponent<CardPlaceholderView>();
                }

                _cardView.EnsureChrome();
            }

            if (_interaction == null)
            {
                _interaction = EnsureChildImage("Interaction", new Color(1f, 1f, 1f, 0.01f), true);
            }

            if (_button == null)
            {
                _button = GetComponent<Button>();
                if (_button == null)
                {
                    _button = gameObject.AddComponent<Button>();
                }

                _button.targetGraphic = _interaction;
                _button.transition = Selectable.Transition.None;
            }

            if (_selectionOverlay == null)
            {
                _selectionOverlay = EnsureChildImage(
                    "SelectionOverlay",
                    BattleUiColors.SelectionOverlay,
                    false);
                _selectionOverlay.gameObject.SetActive(false);
            }

            if (_legalTargetOverlay == null)
            {
                _legalTargetOverlay = EnsureChildImage(
                    "LegalTargetOverlay",
                    BattleUiColors.LegalOverlay,
                    false);
                _legalTargetOverlay.gameObject.SetActive(false);
            }

            if (_statusOverlay == null)
            {
                _statusOverlay = EnsureChildImage(
                    "StatusOverlay",
                    new Color(0.15f, 0.15f, 0.2f, 0.35f),
                    false);
                _statusOverlay.gameObject.SetActive(false);
            }

            if (_roleLabel == null)
            {
                var labelT = transform.Find("RoleLabel");
                if (labelT == null)
                {
                    var go = new GameObject("RoleLabel", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    labelT = go.transform;
                }

                _roleLabel = labelT.GetComponent<TextMeshProUGUI>();
                if (_roleLabel == null)
                {
                    _roleLabel = labelT.gameObject.AddComponent<TextMeshProUGUI>();
                }

                var labelRt = labelT as RectTransform;
                labelRt.anchorMin = new Vector2(0.08f, 0.35f);
                labelRt.anchorMax = new Vector2(0.92f, 0.65f);
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                _roleLabel.alignment = TextAlignmentOptions.Center;
                _roleLabel.fontStyle = FontStyles.Bold;
                _roleLabel.enableAutoSizing = true;
                _roleLabel.fontSizeMin = 11f;
                _roleLabel.fontSizeMax = 22f;
                _roleLabel.color = BattleUiColors.HudText;
                _roleLabel.raycastTarget = false;
                _roleLabel.textWrappingMode = TextWrappingModes.Normal;
                _roleLabel.overflowMode = TextOverflowModes.Ellipsis;
                _roleLabel.gameObject.SetActive(false);
            }

            // Hide legacy floating SlotLabel if present.
            var legacyLabel = transform.Find("SlotLabel");
            if (legacyLabel != null)
            {
                legacyLabel.gameObject.SetActive(false);
            }

            // Frame behind card: the frame asset has an opaque black center.
            if (_frame != null)
            {
                _frame.transform.SetAsFirstSibling();
            }

            if (_cardContainer != null)
            {
                _cardContainer.SetSiblingIndex(_frame != null ? 1 : 0);
            }

            if (_roleLabel != null)
            {
                _roleLabel.transform.SetSiblingIndex(
                    _cardContainer != null ? _cardContainer.GetSiblingIndex() + 1 : 1);
            }

            if (_selectionOverlay != null)
            {
                _selectionOverlay.transform.SetAsLastSibling();
            }

            if (_legalTargetOverlay != null)
            {
                _legalTargetOverlay.transform.SetAsLastSibling();
            }

            if (_statusOverlay != null)
            {
                _statusOverlay.transform.SetAsLastSibling();
            }

            if (_interaction != null)
            {
                _interaction.transform.SetAsLastSibling();
            }
        }

        private Image EnsureChildImage(string name, Color color, bool raycast)
        {
            var t = transform.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(transform, false);
            }
            else
            {
                go = t.gameObject;
            }

            Stretch(go.GetComponent<RectTransform>(), 0f);
            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            rt.localScale = Vector3.one;
        }
    }
}
