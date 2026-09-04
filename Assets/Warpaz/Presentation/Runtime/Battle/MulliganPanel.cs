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

        public void Render(IReadOnlyList<LegalAction> legalActions, PlayerId prioritySeat, bool busy, bool passOverlayVisible)
        {
            gameObject.SetActive(true);

            if (_onlyOnce != null)
            {
                _onlyOnce.text = "Solo puede hacerse una vez.";
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

            if (_keepButton != null)
            {
                _keepButton.interactable = canKeep && !busy && !passOverlayVisible;
            }

            if (_replaceButton != null)
            {
                _replaceButton.interactable = canReplace && !busy && !passOverlayVisible;
            }
        }

        // Testability: read-only getter for PlayMode tests.
        internal bool KeepHandInteractable => _keepButton != null && _keepButton.interactable;

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
