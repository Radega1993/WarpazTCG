using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Large non-blocking card preview for hand hover. Raycasts disabled so board clicks still work.
    /// </summary>
    public sealed class CardInspectOverlay : MonoBehaviour
    {
        private static CardInspectOverlay _instance;
        private CardPlaceholderView _preview;
        private Image _veil;
        private object _sourceKey;
        private bool _visible;

        public static bool IsVisibleForTests => _instance != null && _instance._visible;

        public static CardInspectOverlay Ensure(Transform canvasRoot)
        {
            if (_instance != null)
            {
                return _instance;
            }

            if (canvasRoot == null)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                canvasRoot = canvas != null ? canvas.transform : null;
            }

            if (canvasRoot == null)
            {
                return null;
            }

            var go = new GameObject("CardInspectOverlay", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            go.transform.SetAsLastSibling();
            _instance = go.AddComponent<CardInspectOverlay>();
            _instance.Build();
            return _instance;
        }

        public static void Show(
            object sourceKey,
            string name,
            string type,
            string cost,
            string extra,
            string cardId,
            string rules = null)
        {
            var overlay = Ensure(null);
            if (overlay == null)
            {
                return;
            }

            overlay.ShowInternal(sourceKey, name, type, cost, extra, cardId, rules);
        }

        public static void Hide(object sourceKey)
        {
            if (_instance == null)
            {
                return;
            }

            if (_instance._sourceKey != null && !ReferenceEquals(_instance._sourceKey, sourceKey))
            {
                return;
            }

            _instance.HideInternal();
        }

        public static void HideAll()
        {
            if (_instance != null)
            {
                _instance.HideInternal();
            }
        }

        private void Build()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var veilGo = new GameObject("Veil", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            veilGo.transform.SetParent(transform, false);
            var veilRt = (RectTransform)veilGo.transform;
            veilRt.anchorMin = Vector2.zero;
            veilRt.anchorMax = Vector2.one;
            veilRt.offsetMin = Vector2.zero;
            veilRt.offsetMax = Vector2.zero;
            _veil = veilGo.GetComponent<Image>();
            _veil.color = new Color(0f, 0f, 0f, 0.15f);
            _veil.raycastTarget = false;

            var cardGo = new GameObject("PreviewCard", typeof(RectTransform), typeof(CanvasRenderer));
            cardGo.transform.SetParent(transform, false);
            var cardRt = (RectTransform)cardGo.transform;
            cardRt.anchorMin = new Vector2(0.78f, 0.38f);
            cardRt.anchorMax = new Vector2(0.78f, 0.38f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(260f, 364f);
            cardRt.anchoredPosition = Vector2.zero;
            _preview = cardGo.AddComponent<CardPlaceholderView>();
            _preview.EnsureChrome();
            _preview.SetInteractable(false);

            // Preview must never steal clicks from the board / hand.
            var graphics = cardGo.GetComponentsInChildren<Graphic>(true);
            for (var i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }

            var button = cardGo.GetComponent<Button>();
            if (button != null)
            {
                button.enabled = false;
            }

            gameObject.SetActive(false);
            _visible = false;
        }

        private void ShowInternal(
            object sourceKey,
            string name,
            string type,
            string cost,
            string extra,
            string cardId,
            string rules)
        {
            _sourceKey = sourceKey;
            if (_preview != null)
            {
                _preview.SetFaceUp(name, type, cost, extra, cardId, rules);
                _preview.SetHovered(false);
                _preview.SetSelected(false);
                var graphics = _preview.GetComponentsInChildren<Graphic>(true);
                for (var i = 0; i < graphics.Length; i++)
                {
                    graphics[i].raycastTarget = false;
                }
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            _visible = true;
        }

        private void HideInternal()
        {
            _sourceKey = null;
            _visible = false;
            gameObject.SetActive(false);
        }
    }
}
