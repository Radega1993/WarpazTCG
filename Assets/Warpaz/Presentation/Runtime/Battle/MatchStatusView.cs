using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        private RectTransform _phaseZone;
        private RectTransform _priorityZone;
        private RectTransform _messageZone;

        private void Awake()
        {
            Cache();
            LayoutCenter();
        }

        public void AssignCenterZones(RectTransform phase, RectTransform priority, RectTransform message)
        {
            _phaseZone = phase;
            _priorityZone = priority;
            _messageZone = message;
            Cache();
            LayoutCenter();
        }

        public void Render(MatchView view, PlayerId activePlayer, PlayerId prioritySeat, string rejectionReason)
        {
            if (view == null)
            {
                return;
            }

            Cache();

            if (_turn != null)
            {
                _turn.text = "Turno " + view.Turn + " · Prioridad P" + (prioritySeat.Value + 1);
            }

            if (_phase != null)
            {
                _phase.text = "Fase " + view.Phase;
            }

            if (_active != null)
            {
                // Keep compact contract used by PlayMode indicator tests.
                _active.text = "Activo P" + (activePlayer.Value + 1);
            }

            if (_hash != null)
            {
                _hash.gameObject.SetActive(false);
                _hash.text = string.Empty;
            }

            if (_rejection != null)
            {
                _rejection.text = string.IsNullOrEmpty(rejectionReason) ? string.Empty : rejectionReason;
            }
        }

        internal string GetPhaseText() => _phase != null ? _phase.text : string.Empty;
        internal string GetActivePlayerText() => _active != null ? _active.text : string.Empty;

        private void Cache()
        {
            _turn = Find("TurnText");
            _phase = Find("PhaseText");
            _active = Find("ActivePlayerText");
            _hash = Find("HashText");
            _rejection = Find("RejectionText");
        }

        private void LayoutCenter()
        {
            var host = transform as RectTransform;
            if (host != null)
            {
                BattleLayoutBuilder.StretchFull(host);
            }

            PlaceInZone(_phase, _phaseZone != null ? _phaseZone : host, 0);
            PlaceInZone(_turn, _priorityZone != null ? _priorityZone : host, 0);
            PlaceInZone(_active, _priorityZone != null ? _priorityZone : host, 1);
            PlaceInZone(_rejection, _messageZone != null ? _messageZone : host, 0);

            Configure(_phase);
            Configure(_turn);
            Configure(_active);
            Configure(_rejection);

            if (_hash != null)
            {
                _hash.gameObject.SetActive(false);
            }
        }

        private static void PlaceInZone(TMP_Text text, RectTransform zone, int row)
        {
            if (text == null || zone == null)
            {
                return;
            }

            text.transform.SetParent(zone, false);
            var rt = text.rectTransform;
            var yMax = 1f - row * 0.5f;
            var yMin = yMax - 0.5f;
            rt.anchorMin = new Vector2(0.02f, Mathf.Max(0f, yMin));
            rt.anchorMax = new Vector2(0.98f, Mathf.Min(1f, yMax));
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Configure(TMP_Text text)
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
            text.alignment = TextAlignmentOptions.Center;
            text.color = BattleUiColors.CardText;

            var img = text.GetComponentInParent<Image>();
            // Ensure zone has a subtle panel background once.
            if (text.transform.parent != null)
            {
                var parent = text.transform.parent.gameObject;
                var bg = parent.GetComponent<Image>();
                if (bg == null)
                {
                    bg = parent.AddComponent<Image>();
                    bg.color = new Color(0.12f, 0.12f, 0.14f, 0.55f);
                    bg.raycastTarget = false;
                }
            }
        }

        private TMP_Text Find(string relativePath)
        {
            var t = transform.Find(relativePath);
            if (t == null)
            {
                t = FindDeep(transform, relativePath);
            }

            if (t == null && _phaseZone != null)
            {
                t = FindDeep(_phaseZone, relativePath);
            }

            if (t == null && _priorityZone != null)
            {
                t = FindDeep(_priorityZone, relativePath);
            }

            if (t == null && _messageZone != null)
            {
                t = FindDeep(_messageZone, relativePath);
            }

            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
