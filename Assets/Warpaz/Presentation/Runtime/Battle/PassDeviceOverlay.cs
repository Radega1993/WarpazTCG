using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class PassDeviceOverlay : MonoBehaviour
    {
        private TMP_Text _text;
        private Button _confirmButton;

        private System.Action _onConfirm;

        private void Awake()
        {
            _text = FindText("OverlayText");
            _confirmButton = FindButton("ConfirmButton");

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(() => _onConfirm?.Invoke());
            }
        }

        public void Bind(System.Action onConfirm)
        {
            _onConfirm = onConfirm;
        }

        public void Render(bool visible, PlayerId nextSeat, string player0Name, string player1Name)
        {
            gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            if (_text != null)
            {
                var name = nextSeat == PlayerId.Player0 ? player0Name : player1Name;
                _text.text = "Pasa el dispositivo\nA continuación: " + name;
            }
        }

        private TMP_Text FindText(string relativePath)
        {
            var t = transform.Find(relativePath);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        private Button FindButton(string relativePath)
        {
            var t = transform.Find(relativePath);
            return t != null ? t.GetComponent<Button>() : null;
        }
    }
}
