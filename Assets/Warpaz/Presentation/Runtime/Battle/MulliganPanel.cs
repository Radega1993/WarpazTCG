using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class MulliganPanel : MonoBehaviour
    {
        private Button _keepButton;
        private Button _replaceButton;
        private TMP_Text _onlyOnce;

        private System.Action<MulliganDecision> _onConfirm;

        private void Awake()
        {
            _keepButton = FindButton("KeepHandButton");
            _replaceButton = FindButton("ReplaceHandButton");
            _onlyOnce = FindText("OnlyOnceText");
            LayoutPanel();
        }

        public void Bind(System.Action<MulliganDecision> onConfirm)
        {
            _onConfirm = onConfirm;

            if (_keepButton != null)
            {
                _keepButton.onClick.RemoveAllListeners();
                _keepButton.onClick.AddListener(() => _onConfirm?.Invoke(MulliganDecision.KeepHand));
            }

            if (_replaceButton != null)
            {
                _replaceButton.onClick.RemoveAllListeners();
                _replaceButton.onClick.AddListener(() => _onConfirm?.Invoke(MulliganDecision.ReplaceWholeHand));
            }
        }

        public void Render(
            IReadOnlyList<LegalAction> legalActions,
            PlayerId prioritySeat,
            bool busy,
            bool passOverlayVisible,
            bool allowReplace = true)
        {
            gameObject.SetActive(true);
            LayoutPanel();

            if (_onlyOnce != null)
            {
                _onlyOnce.text = allowReplace
                    ? "Solo puede hacerse una vez."
                    : "Tutorial: Conservar la mano.";
                ConfigureLabel(_onlyOnce);
            }

            var canKeep = false;
            var canReplace = false;

            for (var i = 0; i < legalActions.Count; i++)
            {
                var a = legalActions[i];
                if (a.ActorId != prioritySeat || a.Type != CommandType.ConfirmMulligan)
                {
                    continue;
                }

                if (a.MulliganDecision == MulliganDecision.KeepHand)
                {
                    canKeep = true;
                }
                else if (a.MulliganDecision == MulliganDecision.ReplaceWholeHand)
                {
                    canReplace = true;
                }
            }

            if (!allowReplace)
            {
                canReplace = false;
            }

            if (_keepButton != null)
            {
                _keepButton.interactable = canKeep && !busy && !passOverlayVisible;
            }

            if (_replaceButton != null)
            {
                _replaceButton.interactable = canReplace && !busy && !passOverlayVisible;
            }
        }

        internal bool KeepHandInteractable => _keepButton != null && _keepButton.interactable;

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void LayoutPanel()
        {
            var host = transform as RectTransform;
            if (host != null)
            {
                // Centered modal card, not full-screen white.
                host.anchorMin = new Vector2(0.32f, 0.35f);
                host.anchorMax = new Vector2(0.68f, 0.65f);
                host.offsetMin = Vector2.zero;
                host.offsetMax = Vector2.zero;
            }

            var bg = GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
            }

            bg.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
            bg.raycastTarget = true;

            PlaceButton(_keepButton, 0.08f, 0.38f, 0.48f, 0.72f, "Keep");
            PlaceButton(_replaceButton, 0.52f, 0.38f, 0.92f, 0.72f, "Replace");
            if (_onlyOnce != null)
            {
                var rt = _onlyOnce.rectTransform;
                rt.anchorMin = new Vector2(0.08f, 0.08f);
                rt.anchorMax = new Vector2(0.92f, 0.32f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                ConfigureLabel(_onlyOnce);
            }
        }

        private static void PlaceButton(Button button, float x0, float y0, float x1, float y1, string labelText)
        {
            if (button == null)
            {
                return;
            }

            var rt = button.transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = button.GetComponent<Image>();
            if (img != null)
            {
                img.color = BattleUiColors.ActionButton;
            }

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (string.IsNullOrEmpty(label.text) || label.text.Length < 2)
                {
                    label.text = labelText;
                }

                ConfigureLabel(label);
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void ConfigureLabel(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 28f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.color = BattleUiColors.ActionButtonText;
            text.fontStyle = FontStyles.Bold;
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
