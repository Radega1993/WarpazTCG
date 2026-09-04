using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class HandView : MonoBehaviour
    {
        private readonly List<CardPlaceholderView> _placeholders = new List<CardPlaceholderView>();

        private void Awake()
        {
            // HandCard0..HandCardN
            for (var i = 0; i < 16; i++)
            {
                var t = transform.Find("HandCard" + i);
                if (t == null)
                {
                    break;
                }

                var view = t.GetComponent<CardPlaceholderView>();
                if (view != null)
                {
                    _placeholders.Add(view);
                }
            }

            // Colocación horizontal para evitar solapamientos.
            // También movemos el contenedor hacia la parte inferior.
            var container = transform as RectTransform;
            if (container != null)
            {
                container.anchoredPosition = new Vector2(0, -260f);
            }

            var cardW = 135f;
            var cardH = 80f;
            var count = _placeholders.Count;
            var startX = -(count - 1) * cardW / 2f;

            for (var i = 0; i < count; i++)
            {
                var rt = _placeholders[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(startX + i * cardW, 0);
                    rt.sizeDelta = new Vector2(cardW, cardH);
                }
            }
        }

        public void Render(MatchView view, bool showHand, ICardCatalog catalog)
        {
            if (view == null)
            {
                return;
            }

            for (var i = 0; i < _placeholders.Count; i++)
            {
                _placeholders[i].gameObject.SetActive(false);
            }

            if (!showHand)
            {
                return;
            }

            var hand = view.Viewer.VisibleHand;
            for (var i = 0; i < hand.Count && i < _placeholders.Count; i++)
            {
                var placeholder = _placeholders[i];
                var card = hand[i];

                if (catalog != null && catalog.TryGet(card.CardId, out var def))
                {
                    var cost = def.CostPa.HasValue ? def.CostPa.Value.ToString() : "—";
                    placeholder.SetCard(def.Name, def.Type.ToString(), cost);
                }
                else
                {
                    placeholder.SetCard(card.CardId.Value, "?", "—");
                }

                placeholder.gameObject.SetActive(true);
            }
        }
    }
}
