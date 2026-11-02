using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class HandView : MonoBehaviour
    {
        private readonly List<CardPlaceholderView> _placeholders = new List<CardPlaceholderView>();
        private System.Action<CardInstanceId> _onCardClicked;

        private void Awake()
        {
            CollectCards();
            LayoutInZone();
        }

        public void Bind(System.Action<CardInstanceId> onCardClicked)
        {
            _onCardClicked = onCardClicked;
        }

        public void Render(
            MatchView view,
            bool showHand,
            ICardCatalog catalog,
            bool interactive,
            CardInstanceId selectedHandId = default,
            IReadOnlyCollection<CardInstanceId> playableIds = null)
        {
            if (view == null)
            {
                return;
            }

            if (_placeholders.Count == 0)
            {
                CollectCards();
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
            var visible = Mathf.Min(hand.Count, _placeholders.Count);
            for (var i = 0; i < visible; i++)
            {
                var placeholder = _placeholders[i];
                var card = hand[i];
                var instanceId = card.InstanceId;

                if (catalog != null && catalog.TryGet(card.CardId, out var def))
                {
                    var cost = def.CostPa.HasValue ? "PA " + def.CostPa.Value : "—";
                    var power = def.Power.HasValue ? "P" + def.Power.Value : "—";
                    placeholder.SetFaceUp(
                        def.Name,
                        CardTypeCatalog.ToLabel(def.Type),
                        cost,
                        power,
                        card.CardId.Value,
                        def.EffectText);
                }
                else
                {
                    placeholder.SetFaceUp(card.CardId.Value, "?", "—", null, card.CardId.Value);
                }

                var selected = !selectedHandId.IsNone && selectedHandId == instanceId;
                var playable = Contains(playableIds, instanceId);
                placeholder.SetSelected(selected);
                placeholder.SetPlayable(playable && interactive);
                placeholder.Bind(() => _onCardClicked?.Invoke(instanceId));
                placeholder.SetInteractable(interactive);
                placeholder.gameObject.SetActive(true);
            }

            LayoutVisible(visible);
        }

        private void CollectCards()
        {
            _placeholders.Clear();
            for (var i = 0; i < 12; i++)
            {
                var t = transform.Find("HandCard" + i);
                if (t == null)
                {
                    var go = new GameObject("HandCard" + i, typeof(RectTransform), typeof(CanvasRenderer));
                    go.transform.SetParent(transform, false);
                    go.AddComponent<CardPlaceholderView>();
                    t = go.transform;
                }

                var rt = t as RectTransform;
                if (rt == null)
                {
                    rt = BattleLayoutBuilder.EnsureRectTransformOnUiObject(t.gameObject, transform);
                    t = rt;
                }

                var view = t.GetComponent<CardPlaceholderView>();
                if (view == null)
                {
                    view = t.gameObject.AddComponent<CardPlaceholderView>();
                }

                view.EnsureChrome();
                _placeholders.Add(view);
            }
        }

        private void LayoutInZone()
        {
            var container = transform as RectTransform;
            if (container != null)
            {
                BattleLayoutBuilder.StretchFull(container);
            }
        }

        private void LayoutVisible(int count)
        {
            if (count <= 0)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var rt = _placeholders[i].transform as RectTransform;
                if (rt == null)
                {
                    continue;
                }

                var x0 = (float)i / count + 0.005f;
                var x1 = (float)(i + 1) / count - 0.005f;
                rt.anchorMin = new Vector2(x0, 0.05f);
                rt.anchorMax = new Vector2(x1, 0.95f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                // Pivot/scale owned by CardPlaceholderView hover + selection.
            }
        }

        private static bool Contains(IReadOnlyCollection<CardInstanceId> ids, CardInstanceId id)
        {
            if (ids == null || id.IsNone)
            {
                return false;
            }

            foreach (var candidate in ids)
            {
                if (candidate == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
