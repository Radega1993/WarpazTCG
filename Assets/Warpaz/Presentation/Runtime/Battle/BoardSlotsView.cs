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
            PlayerId localSeat,
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

            // Slots 0-3 = bottom (local), 4-7 = top (opponent). Never derive from priority.
            var local = localSeat == PlayerId.Player0 ? view.Player0 : view.Player1;
            var rival = localSeat == PlayerId.Player0 ? view.Player1 : view.Player0;

            for (var i = 0; i < _slots.Count; i++)
            {
                _slots[i].gameObject.SetActive(true);
            }

            RenderBoard(local, localSeat, 0, catalog, interactive, selectedBoardId, legalTargetIds);
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
                var cost = "—";
                var power = "—";
                var type = slot.Row.ToString();
                string rules = null;
                if (catalog != null && catalog.TryGet(slot.Occupant.CardId, out var def))
                {
                    name = def.Name;
                    cost = def.CostPa.HasValue ? "PA " + def.CostPa.Value : "—";
                    power = def.Power.HasValue ? "P" + System.Math.Max(0, def.Power.Value + slot.Occupant.PowerModifier) : "—";
                    type = CardTypeCatalog.ToLabel(def.Type);
                    rules = def.EffectText;
                }

                if (slot.HasRecent)
                {
                    power = power + " ·R";
                }

                if (slot.Occupant.EquipmentId.HasValue && catalog.TryGet(slot.Occupant.EquipmentId.Value, out var equipment))
                    rules = (rules ?? string.Empty) + "\nEquipo: " + equipment.Name + " — " + equipment.EffectText;

                ui.SetCard(name, type, cost, power, slot.Occupant.CardId.Value, rules);
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

                // Root-level CardPlaceholderView chrome (grey plate) used to cover FaceUpCard art.
                StripLegacyRootCardChrome(t);

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

        private static void StripLegacyRootCardChrome(Transform slotRoot)
        {
            if (slotRoot == null)
            {
                return;
            }

            var legacy = slotRoot.GetComponent<CardPlaceholderView>();
            if (legacy != null)
            {
                Object.Destroy(legacy);
            }

            // Only strip chrome that CardPlaceholderView parents directly under the slot root.
            // Keep CharacterSlotView children: Frame, CardContainer, Interaction, overlays.
            string[] chrome =
            {
                "GrayBackground", "CardBack", "Border", "InnerRim", "Header", "EmptyContentArea",
                "ArtLip", "RulesPanel", "StatsPanel", "TypeBar", "CostGem", "PowerGem",
                "SelectedOverlay", "LegalOverlay", "TypeLine", "Illustration", "Text"
            };
            for (var i = 0; i < chrome.Length; i++)
            {
                var child = slotRoot.Find(chrome[i]);
                if (child != null)
                {
                    Object.Destroy(child.gameObject);
                }
            }
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
