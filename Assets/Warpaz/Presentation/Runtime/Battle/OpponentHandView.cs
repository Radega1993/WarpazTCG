using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    /// <summary>Rival hand as face-down grey cards. Count only — no private card data.</summary>
    public sealed class OpponentHandView : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<CardPlaceholderView> _cards =
            new System.Collections.Generic.List<CardPlaceholderView>();

        public void Render(int handCount)
        {
            EnsureCapacity(Mathf.Clamp(handCount, 0, 10));
            for (var i = 0; i < _cards.Count; i++)
            {
                var active = i < handCount;
                _cards[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                _cards[i].SetFaceDown();
                _cards[i].SetInteractable(false);
                _cards[i].SetSelected(false);
                _cards[i].SetPlayable(false);

                var rt = _cards[i].transform as RectTransform;
                if (rt == null || handCount <= 0)
                {
                    continue;
                }

                var x0 = (float)i / handCount + 0.01f;
                var x1 = (float)(i + 1) / handCount - 0.01f;
                rt.anchorMin = new Vector2(x0, 0.05f);
                rt.anchorMax = new Vector2(x1, 0.95f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private void EnsureCapacity(int count)
        {
            while (_cards.Count < count)
            {
                var go = new GameObject("OppHandCard" + _cards.Count, typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<CardPlaceholderView>();
                view.EnsureChrome();
                view.SetFaceDown();
                _cards.Add(view);
            }
        }
    }
}
