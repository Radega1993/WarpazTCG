using TMPro;
using UnityEngine;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class MatchStatusView : MonoBehaviour
    {
        private TMP_Text _turn;
        private TMP_Text _phase;
        private TMP_Text _active;
        private TMP_Text _hash;
        private TMP_Text _rejection;

        private void Awake()
        {
            var container = transform as RectTransform;
            if (container != null)
            {
                container.anchoredPosition = new Vector2(0, 140f);
            }

            _turn = Find("TurnText");
            _phase = Find("PhaseText");
            _active = Find("ActivePlayerText");
            _hash = Find("HashText");
            _rejection = Find("RejectionText");

            var startY = 0f;
            var stepY = -24f;
            Set(_turn, 0, startY + stepY * 0);
            Set(_phase, 0, startY + stepY * 1);
            Set(_active, 0, startY + stepY * 2);
            Set(_hash, 0, startY + stepY * 3);
            Set(_rejection, 0, startY + stepY * 4);
        }

        public void Render(MatchView view, PlayerId activePlayer, PlayerId prioritySeat, string rejectionReason)
        {
            if (view == null)
            {
                return;
            }

            if (_turn != null)
            {
                _turn.text = "Turn " + view.Turn;
            }

            if (_phase != null)
            {
                _phase.text = "Fase " + view.Phase;
            }

            if (_active != null)
            {
                _active.text = "Activo P" + (activePlayer.Value + 1);
            }

            if (_hash != null)
            {
                _hash.text = view.IsDebug ? "Hash " + Abbrev(view.Hash) : string.Empty;
            }

            if (_rejection != null)
            {
                _rejection.text = string.IsNullOrEmpty(rejectionReason) ? string.Empty : "Error: " + rejectionReason;
            }
        }

        // Testability: read-only getters for PlayMode tests (no logic, no side-effects).
        internal string GetPhaseText() => _phase != null ? _phase.text : string.Empty;
        internal string GetActivePlayerText() => _active != null ? _active.text : string.Empty;

        private static string Abbrev(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return string.Empty;
            }

            return hash.Substring(0, 8);
        }

        private TMP_Text Find(string relativePath)
        {
            var t = transform.Find(relativePath);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        private static void Set(TMP_Text text, float x, float y)
        {
            if (text == null)
            {
                return;
            }

            var rt = text.rectTransform;
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
