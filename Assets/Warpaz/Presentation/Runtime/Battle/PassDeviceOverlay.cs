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
            LayoutOverlay();

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

            LayoutOverlay();

            if (_text != null)
            {
                var name = nextSeat == PlayerId.Player0 ? player0Name : player1Name;
                _text.text = "Pasa el dispositivo a " + name;
                _text.textWrappingMode = TextWrappingModes.Normal;
                _text.overflowMode = TextOverflowModes.Ellipsis;
                _text.alignment = TextAlignmentOptions.Center;
                _text.color = BattleUiColors.HudText;
                _text.fontStyle = FontStyles.Bold;
                _text.raycastTarget = false;
            }
        }

        private void LayoutOverlay()
        {
            var host = transform as RectTransform;
            if (host != null)
            {
                host.anchorMin = new Vector2(0.25f, 0.35f);
                host.anchorMax = new Vector2(0.75f, 0.65f);
                host.offsetMin = Vector2.zero;
                host.offsetMax = Vector2.zero;
            }

            var bg = GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
            }

            bg.color = new Color(0.10f, 0.10f, 0.12f, 0.94f);

            if (_text != null)
            {
                var rt = _text.rectTransform;
                rt.anchorMin = new Vector2(0.08f, 0.45f);
                rt.anchorMax = new Vector2(0.92f, 0.88f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            if (_confirmButton != null)
            {
                var rt = _confirmButton.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.30f, 0.12f);
                    rt.anchorMax = new Vector2(0.70f, 0.38f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }

                var img = _confirmButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = BattleUiColors.ActionButton;
                }

                var confirmLabel = _confirmButton.GetComponentInChildren<TMP_Text>(true);
                if (confirmLabel != null)
                {
                    confirmLabel.color = BattleUiColors.ActionButtonText;
                    confirmLabel.fontStyle = FontStyles.Bold;
                }
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
