using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Renders up to 8 character slots: first 4 = priority (local) board, next 4 = rival board.
    /// Visual zones: local bottom, rival top (reparented by BattleLayoutBuilder).
    /// </summary>
    public sealed class BoardSlotsView : MonoBehaviour
    {
        [SerializeField] private Sprite _slotFrameSprite;
        [SerializeField] private CharacterSlotView _slotPrefab;

        private readonly List<CharacterSlotView> _slots = new List<CharacterSlotView>();
        private System.Action<PlayerId, int> _onSlotClicked;

        public void RefreshSlots()
        {
            CollectOrUpgradeSlots();
            LayoutSlots();
        }

        private void Awake()
        {
            CollectOrUpgradeSlots();
            LayoutSlots();
        }

        public void Bind(System.Action<PlayerId, int> onSlotClicked)
        {
            _onSlotClicked = onSlotClicked;
        }

        public void ApplyArt(Sprite slotFrameSprite, CharacterSlotView prefab = null)
        {
            if (slotFrameSprite != null)
            {
                _slotFrameSprite = slotFrameSprite;
            }

            if (prefab != null)
            {
                _slotPrefab = prefab;
            }

            for (var i = 0; i < _slots.Count; i++)
            {
                _slots[i].ApplyFrameSprite(_slotFrameSprite);
            }
        }

        public void Render(
            MatchView view,
            PlayerId prioritySeat,
            ICardCatalog catalog,
            bool interactive,
            CardInstanceId selectedBoardId = default,
            IReadOnlyCollection<CardInstanceId> legalTargetIds = null)
        {
            if (view == null)
            {
                return;
            }

            if (_slots.Count == 0)
            {
                CollectOrUpgradeSlots();
                LayoutSlots();
            }

            var priority = prioritySeat == PlayerId.Player0 ? view.Player0 : view.Player1;
            var rival = prioritySeat == PlayerId.Player0 ? view.Player1 : view.Player0;

            for (var i = 0; i < _slots.Count; i++)
            {
                _slots[i].gameObject.SetActive(true);
            }

            RenderBoard(priority, prioritySeat, 0, catalog, interactive, selectedBoardId, legalTargetIds);
            RenderBoard(rival, rival.PlayerId, 4, catalog, interactive, selectedBoardId, legalTargetIds);
            LayoutSlots();
        }

        private void RenderBoard(
            PlayerMatchView player,
            PlayerId ownerId,
            int slotOffset,
            ICardCatalog catalog,
            bool interactive,
            CardInstanceId selectedBoardId,
            IReadOnlyCollection<CardInstanceId> legalTargetIds)
        {
            var board = player.BoardSlots;
            for (var i = 0; i < 4 && slotOffset + i < _slots.Count; i++)
            {
                var ui = _slots[slotOffset + i];
                var domainIndex = i;
                ui.Bind(() => _onSlotClicked?.Invoke(ownerId, domainIndex));
                ui.gameObject.SetActive(true);
                ui.ApplyFrameSprite(_slotFrameSprite);
                ui.ClearVisualState();

                if (i >= board.Count || !board[i].IsOccupied)
                {
                    var row = i == 0 ? "Vanguardia" : "Retaguardia";
                    var idx = i == 0 ? 0 : i;
                    ui.SetEmpty(row, idx);
                    ui.SetInteractable(interactive);
                    ui.SetSelected(false);
                    ui.SetLegalTarget(false);
                    ui.SetExhausted(false);
                    continue;
                }

                var slot = board[i];
                var name = slot.Occupant.CardId.Value;
                var power = "—";
                var type = slot.Row.ToString();
                if (catalog != null && catalog.TryGet(slot.Occupant.CardId, out var def))
                {
                    name = def.Name;
                    power = def.Power.HasValue ? "P" + def.Power.Value : "—";
                    type = def.Type.ToString();
                }

                var flags = slot.ReadyState + (slot.HasRecent ? " Reciente" : string.Empty);
                ui.SetCard(name, type, power, flags, slot.Occupant.CardId.Value);
                ui.SetInteractable(interactive);
                ui.SetSelected(!selectedBoardId.IsNone && selectedBoardId == slot.Occupant.InstanceId);
                ui.SetLegalTarget(Contains(legalTargetIds, slot.Occupant.InstanceId));
                ui.SetExhausted(slot.ReadyState == ReadyState.Exhausted);
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

        private void CollectOrUpgradeSlots()
        {
            _slots.Clear();
            for (var i = 0; i < 8; i++)
            {
                var t = FindSlotTransform("BoardSlot" + i);
                if (t == null)
                {
                    break;
                }

                var rt = t as RectTransform;
                if (rt == null)
                {
                    rt = BattleLayoutBuilder.EnsureRectTransformOnUiObject(t.gameObject, t.parent);
                    t = rt;
                }

                var slot = t.GetComponent<CharacterSlotView>();
                if (slot == null)
                {
                    slot = t.gameObject.AddComponent<CharacterSlotView>();
                }

                slot.EnsureWired();
                slot.ApplyFrameSprite(_slotFrameSprite);

                var legacy = t.GetComponent<CardPlaceholderView>();
                if (legacy != null)
                {
                    legacy.enabled = false;
                }

                _slots.Add(slot);
            }
        }

        private Transform FindSlotTransform(string name)
        {
            var direct = transform.Find(name);
            if (direct != null)
            {
                return direct;
            }

            var canvas = GetComponentInParent<Canvas>();
            var searchRoot = canvas != null ? canvas.transform : transform.root;
            var all = searchRoot.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == name)
                {
                    return all[i];
                }
            }

            return null;
        }

        private void LayoutSlots()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var rt = _slots[i].GetComponent<RectTransform>();
                if (rt == null)
                {
                    continue;
                }

                // Explicit column cells inside parent board zone (no HLG dependency).
                var col = i % 4;
                var x0 = col * 0.25f + 0.01f;
                var x1 = (col + 1) * 0.25f - 0.01f;
                var isVanguard = col == 0;
                var y0 = isVanguard ? 0.02f : 0.08f;
                var y1 = isVanguard ? 0.98f : 0.92f;
                rt.anchorMin = new Vector2(x0, y0);
                rt.anchorMax = new Vector2(x1, y1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;

                var le = rt.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.ignoreLayout = true;
                }
            }
        }
    }
}
