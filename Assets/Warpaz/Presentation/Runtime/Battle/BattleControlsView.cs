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

        private void Awake()
        {
            _endTurnButton = FindButton("EndTurnButton");
            _endTurnText = FindText("EndTurnButton/Text");

            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.RemoveAllListeners();
                _endTurnButton.onClick.AddListener(() => _onEndTurn?.Invoke());
            }
        }

        public void Bind(System.Action onEndTurn)
        {
            _onEndTurn = onEndTurn;
        }

        public void Render(bool canEndTurn, bool busy, bool passOverlayVisible)
        {
            gameObject.SetActive(true);
            if (_endTurnButton != null)
            {
                _endTurnButton.interactable = canEndTurn && !busy && !passOverlayVisible;
            }

            if (_endTurnText != null)
            {
                _endTurnText.text = "Finalizar turno";
            }
        }

        // Testability: read-only getter for PlayMode tests.
        internal bool EndTurnInteractable => _endTurnButton != null && _endTurnButton.interactable;

        public void Hide()
        {
            gameObject.SetActive(false);
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
