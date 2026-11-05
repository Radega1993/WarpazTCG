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
        public const int PageSize = 8;
        public int CurrentPage { get; private set; }
        private MatchView _view;
        private ICardCatalog _catalog;
        private bool _interactive;
        private CardInstanceId _selected;
        private IReadOnlyCollection<CardInstanceId> _playable;
        private GameObject _pager;
        private TMPro.TMP_Text _pageLabel;

        public void ChangePage(int delta)
        {
            if (_view == null) return;
            CurrentPage = Mathf.Clamp(CurrentPage + delta, 0, Mathf.Max(0, (_view.Viewer.VisibleHand.Count - 1) / PageSize));
            Render(_view, true, _catalog, _interactive, _selected, _playable);
        }

        private void EnsurePager()
        {
            if (_pager != null) return;
            _pager = new GameObject("HandPages", typeof(RectTransform)); _pager.transform.SetParent(transform, false);
            var rect = (RectTransform)_pager.transform; rect.anchorMin = new Vector2(.37f, .87f); rect.anchorMax = new Vector2(.63f, 1); rect.offsetMin = rect.offsetMax = Vector2.zero;
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("PageControl", typeof(RectTransform), typeof(Image)); go.transform.SetParent(_pager.transform, false);
                var r = (RectTransform)go.transform; r.anchorMin = new Vector2(i / 3f, 0); r.anchorMax = new Vector2((i + 1) / 3f, 1); r.offsetMin = r.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = new Color(.1f, .15f, .2f, 1);
                var textGo = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)); textGo.transform.SetParent(go.transform, false);
                var tr = (RectTransform)textGo.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
                var text = textGo.GetComponent<TMPro.TextMeshProUGUI>(); text.fontSize = 16; text.alignment = TMPro.TextAlignmentOptions.Center; text.raycastTarget = false;
                if (i == 1) _pageLabel = text;
                else { var delta = i == 0 ? -1 : 1; text.text = i == 0 ? "‹" : "›"; go.AddComponent<Button>().onClick.AddListener(() => ChangePage(delta)); }
            }
        }

        private void LateUpdate()
        {
            var visible = 0;
            foreach (var card in _placeholders) if (card.gameObject.activeSelf) visible++;
            LayoutVisible(visible);
        }

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
            EnsurePager();
            if (!showHand || view == null)
            {
                _view = null; _catalog = null; _playable = null; _selected = default; CurrentPage = 0;
                _pager.SetActive(false); _pageLabel.text = "";
                foreach (var placeholder in _placeholders) { placeholder.Bind(null); placeholder.SetFaceDown(); placeholder.gameObject.SetActive(false); }
                return;
            }
            if (_view != null && _view.Viewer.PlayerId != view.Viewer.PlayerId) CurrentPage = 0;
            _view = view; _catalog = catalog; _interactive = interactive; _selected = selectedHandId; _playable = playableIds;

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
            CurrentPage = Mathf.Clamp(CurrentPage, 0, Mathf.Max(0, (hand.Count - 1) / PageSize));
            var offset = CurrentPage * PageSize;
            var visible = Mathf.Min(hand.Count - offset, PageSize);
            _pager.SetActive(hand.Count > PageSize);
            _pageLabel.text = (CurrentPage + 1) + "/" + Mathf.Max(1, (hand.Count + PageSize - 1) / PageSize);
            for (var i = 0; i < visible; i++)
            {
                var placeholder = _placeholders[i];
                var card = hand[i + offset];
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

            var parent = transform as RectTransform;
            if (parent == null || parent.rect.width <= 0f || parent.rect.height <= 0f) return;
            var available = parent.rect.width * 0.98f;
            var size = CardLayout.Fit(available / count - 8f, parent.rect.height * 0.78f);
            var step = size.x + 8f;
            for (var i = 0; i < count; i++)
            {
                var rt = _placeholders[i].transform as RectTransform;
                if (rt == null)
                {
                    continue;
                }

                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.05f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = size;
                rt.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * step, 0f);
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
