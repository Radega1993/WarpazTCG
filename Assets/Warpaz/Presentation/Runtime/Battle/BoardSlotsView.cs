using System.Collections.Generic;
using UnityEngine;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Huecos de tablero bloqueados para Sprint 1 (Sprint 2).
    /// </summary>
    public sealed class BoardSlotsView : MonoBehaviour
    {
        private const string BlockLabel = "Sprint 2";

        private CardPlaceholderView[] _slots;

        private void Awake()
        {
            // BoardSlot0..BoardSlot7
            var list = new List<CardPlaceholderView>();
            for (var i = 0; i < 16; i++)
            {
                var t = transform.Find("BoardSlot" + i);
                if (t == null)
                {
                    break;
                }

                var view = t.GetComponent<CardPlaceholderView>();
                if (view != null)
                {
                    list.Add(view);
                }
            }

            _slots = list.ToArray();

            // Disposición simple (2 filas x 4 columnas).
            // Usamos anchoredPosition para que funcione con el Canvas.
            var slotW = 210f;
            var slotH = 120f;
            var startX = -3 * slotW / 2f;
            var startY = slotH / 2f;

            for (var i = 0; i < _slots.Length; i++)
            {
                var row = i / 4;
                var col = i % 4;
                var rt = _slots[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(startX + col * slotW, startY - row * slotH);
                }
            }
        }

        public void Render(bool debugVisibility)
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i].SetBlocked(BlockLabel);
                _slots[i].gameObject.SetActive(true);
            }
        }
    }
}
