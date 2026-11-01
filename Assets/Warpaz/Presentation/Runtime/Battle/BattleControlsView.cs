using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    public sealed class BattleControlsView : MonoBehaviour
    {
        private Button _endTurnButton;
        private TMP_Text _endTurnText;

        private System.Action _onEndTurn;
        private System.Action _onDecline;

        private void Awake()
        {
            _endTurnButton = FindButton("EndTurnButton");
            _endTurnText = FindText("EndTurnButton/Text");
            LayoutAction();
        }

        public void Bind(System.Action onEndTurn, System.Action onDeclineInterception = null)
        {
            _onEndTurn = onEndTurn;
            _onDecline = onDeclineInterception;
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.RemoveAllListeners();
                _endTurnButton.onClick.AddListener(OnClicked);
            }
        }

        public void Render(bool canEndTurn, bool busy, bool passOverlayVisible)
        {
            gameObject.SetActive(true);
            LayoutAction();
            if (_endTurnButton != null)
            {
                _endTurnButton.interactable = canEndTurn && !busy && !passOverlayVisible;
            }

            if (_endTurnText != null)
            {
                _endTurnText.text = "Finalizar turno";
            }
        }

        public void RenderIntercept(bool canDecline, bool busy, bool passOverlayVisible)
        {
            gameObject.SetActive(true);
            LayoutAction();
            if (_endTurnButton != null)
            {
                _endTurnButton.interactable = canDecline && !busy && !passOverlayVisible;
            }

            if (_endTurnText != null)
            {
                _endTurnText.text = "Declinar intercepción";
            }
        }

        internal bool EndTurnInteractable => _endTurnButton != null && _endTurnButton.interactable;

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void LayoutAction()
        {
            var host = transform as RectTransform;
            if (host != null)
            {
                BattleLayoutBuilder.StretchFull(host);
            }

            if (_endTurnButton != null)
            {
                var rt = _endTurnButton.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.05f, 0.1f);
                    rt.anchorMax = new Vector2(0.95f, 0.9f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }

                var img = _endTurnButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.28f, 0.26f, 0.22f, 1f);
                }
            }

            if (_endTurnText != null)
            {
                _endTurnText.raycastTarget = false;
                _endTurnText.textWrappingMode = TextWrappingModes.NoWrap;
                _endTurnText.overflowMode = TextOverflowModes.Ellipsis;
                _endTurnText.alignment = TextAlignmentOptions.Center;
                _endTurnText.color = BattleUiColors.CardText;
                _endTurnText.enableAutoSizing = true;
                _endTurnText.fontSizeMin = 12f;
                _endTurnText.fontSizeMax = 22f;
            }
        }

        private void OnClicked()
        {
            if (_endTurnText != null && _endTurnText.text != null &&
                _endTurnText.text.IndexOf("Declinar", System.StringComparison.Ordinal) >= 0)
            {
                _onDecline?.Invoke();
                return;
            }

            _onEndTurn?.Invoke();
        }

        private Button FindButton(string relativePath)
        {
            var t = transform.Find(relativePath);
            return t != null ? t.GetComponent<Button>() : null;
        }

        private TMP_Text FindText(string relativePath)
        {
            var t = transform.Find(relativePath);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }
    }
}
