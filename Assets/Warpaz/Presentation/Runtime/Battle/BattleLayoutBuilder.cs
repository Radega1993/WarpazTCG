using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Authoritative Battle UI layout. Builds a full-stretch SafeAreaRoot under Canvas
    /// without destroying BattleCompositionRoot (scene may host it as a plain Transform).
    /// </summary>
    public static class BattleLayoutBuilder
    {
        public const string SafeAreaName = "SafeAreaRoot";
        public const string OpponentAreaName = "OpponentArea";
        public const string CenterLaneName = "CenterLane";
        public const string LocalAreaName = "LocalArea";
        public const string ModalLayerName = "ModalLayer";
        public const string ValidationLayerName = "ValidationOrDebugLayer";

        public const string OpponentHudName = "OpponentHud";
        public const string OpponentHandName = "OpponentHand";
        public const string OpponentBoardName = "OpponentBoard";
        public const string OpponentPilesName = "OpponentPiles";
        public const string PhasePanelName = "PhasePanel";
        public const string PriorityPanelName = "PriorityPanelCenter";
        public const string MessagePanelName = "MessagePanel";
        public const string LocalBoardName = "LocalBoard";
        public const string LocalHudName = "LocalHud";
        public const string LocalHandName = "LocalHand";
        public const string LocalPilesName = "LocalPiles";
        public const string ActionPanelName = "ActionPanel";

        public static RectTransform EnsureLayout(BattleCompositionRoot root)
        {
            if (root == null)
            {
                return null;
            }

            var canvas = root.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = Object.FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return null;
            }

            DisableLegacyBackgroundPanel(canvas.transform);

            // Prefer converting root when possible; if it is a plain Transform, keep it and
            // host layout under Canvas so DestroyImmediate never drops BattleCompositionRoot.
            var rootRt = root.GetComponent<RectTransform>();
            RectTransform layoutParent;
            if (rootRt != null)
            {
                StretchFull(rootRt);
                layoutParent = rootRt;
            }
            else
            {
                layoutParent = canvas.GetComponent<RectTransform>();
            }

            var safe = EnsureChild(layoutParent, SafeAreaName);
            StretchFull(safe);
            if (layoutParent == canvas.transform || ReferenceEquals(layoutParent, canvas.GetComponent<RectTransform>()))
            {
                // Keep under canvas, after Background.
                safe.SetSiblingIndex(Mathf.Min(1, canvas.transform.childCount - 1));
            }

            var opponentArea = EnsureChild(safe, OpponentAreaName);
            StretchFull(opponentArea);
            var centerLane = EnsureChild(safe, CenterLaneName);
            StretchFull(centerLane);
            var localArea = EnsureChild(safe, LocalAreaName);
            StretchFull(localArea);

            var opponentHud = EnsureZone(opponentArea, OpponentHudName, new Vector2(0.025f, 0.82f), new Vector2(0.19f, 0.97f));
            var opponentHand = EnsureZone(opponentArea, OpponentHandName, new Vector2(0.28f, 0.84f), new Vector2(0.72f, 0.99f));
            var opponentBoard = EnsureZone(opponentArea, OpponentBoardName, new Vector2(0.22f, 0.52f), new Vector2(0.76f, 0.82f));
            var opponentPiles = EnsureZone(opponentArea, OpponentPilesName, new Vector2(0.80f, 0.69f), new Vector2(0.96f, 0.91f));

            var phasePanel = EnsureZone(centerLane, PhasePanelName, new Vector2(0.20f, 0.50f), new Vector2(0.80f, 0.55f));
            var priorityPanel = EnsureZone(centerLane, PriorityPanelName, new Vector2(0.20f, 0.47f), new Vector2(0.80f, 0.50f));
            var messagePanel = EnsureZone(centerLane, MessagePanelName, new Vector2(0.20f, 0.45f), new Vector2(0.80f, 0.47f));

            var localBoard = EnsureZone(localArea, LocalBoardName, new Vector2(0.22f, 0.19f), new Vector2(0.76f, 0.47f));
            var localHud = EnsureZone(localArea, LocalHudName, new Vector2(0.025f, 0.03f), new Vector2(0.19f, 0.19f));
            var localHand = EnsureZone(localArea, LocalHandName, new Vector2(0.25f, 0.00f), new Vector2(0.75f, 0.20f));
            var localPiles = EnsureZone(localArea, LocalPilesName, new Vector2(0.80f, 0.14f), new Vector2(0.96f, 0.38f));
            var actionPanel = EnsureZone(localArea, ActionPanelName, new Vector2(0.79f, 0.02f), new Vector2(0.97f, 0.12f));

            var modal = EnsureChild(canvas.transform, ModalLayerName);
            StretchFull(modal);
            modal.SetAsLastSibling();

            var validation = EnsureChild(canvas.transform, ValidationLayerName);
            StretchFull(validation);
            validation.SetAsLastSibling();

            ReparentViewInto(canvas, typeof(PlayerAreaView), safe);
            ReparentBoardSlots(canvas, opponentBoard, localBoard, safe);
            ReparentViewInto(canvas, typeof(HandView), localHand);
            ReparentViewInto(canvas, typeof(MatchStatusView), phasePanel);
            ReparentViewInto(canvas, typeof(EventLogView), validation);
            ReparentViewInto(canvas, typeof(MulliganPanel), modal);
            ReparentViewInto(canvas, typeof(PassDeviceOverlay), modal);
            ReparentViewInto(canvas, typeof(BattleControlsView), actionPanel);

            EnsureOpponentHandHost(opponentHand);
            EnsurePileHosts(opponentPiles, localPiles);

            var area = canvas.GetComponentInChildren<PlayerAreaView>(true);
            area?.AssignHudZones(opponentHud, localHud);

            var status = canvas.GetComponentInChildren<MatchStatusView>(true);
            status?.AssignCenterZones(phasePanel, priorityPanel, messagePanel);

            StretchHost(canvas.GetComponentInChildren<HandView>(true));
            StretchHost(canvas.GetComponentInChildren<MatchStatusView>(true));
            StretchHost(canvas.GetComponentInChildren<BattleControlsView>(true));
            StretchHost(canvas.GetComponentInChildren<PlayerAreaView>(true));

            // Zones use explicit child anchors; remove layout groups that fight cell anchors.
            StripLayoutGroup(opponentBoard);
            StripLayoutGroup(localBoard);
            StripLayoutGroup(opponentHand);
            StripLayoutGroup(localHand);
            StripLayoutGroup(opponentPiles);
            StripLayoutGroup(localPiles);

            return safe;
        }

        private static void StripLayoutGroup(RectTransform zone)
        {
            if (zone == null)
            {
                return;
            }

            var hlg = zone.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                Object.Destroy(hlg);
            }

            var vlg = zone.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                Object.Destroy(vlg);
            }
        }

        private static void DisableLegacyBackgroundPanel(Transform canvas)
        {
            var bg = canvas.Find("Background");
            if (bg == null)
            {
                return;
            }

            var panel = bg.Find("Panel");
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }
        }

        private static void ReparentBoardSlots(
            Canvas canvas,
            RectTransform opponentBoard,
            RectTransform localBoard,
            RectTransform safe)
        {
            var board = canvas.GetComponentInChildren<BoardSlotsView>(true);
            if (board == null)
            {
                return;
            }

            var slots = new Transform[8];
            for (var i = 0; i < 8; i++)
            {
                slots[i] = FindDeep(canvas.transform, "BoardSlot" + i);
            }

            for (var i = 0; i < 8; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                var parent = i < 4 ? localBoard : opponentBoard;
                var slotRt = EnsureRectTransformOnUiObject(slots[i].gameObject, parent);
                if (slotRt != null)
                {
                    slotRt.SetParent(parent, false);
                }
            }

            // Park BoardSlotsView host without covering zones (do not destroy if plain Transform).
            var boardRt = board.transform as RectTransform;
            if (boardRt != null)
            {
                boardRt.SetParent(safe, false);
                boardRt.anchorMin = new Vector2(0.5f, 0.5f);
                boardRt.anchorMax = new Vector2(0.5f, 0.5f);
                boardRt.sizeDelta = Vector2.zero;
                boardRt.anchoredPosition = Vector2.zero;
            }

            board.RefreshSlots();
        }

        private static void ReparentViewInto(Canvas canvas, System.Type viewType, RectTransform zone)
        {
            var view = canvas.GetComponentInChildren(viewType, true) as Component;
            if (view == null || zone == null)
            {
                return;
            }

            // Safe to rebuild non-root view hosts as RectTransform.
            var rt = EnsureRectTransformOnUiObject(view.gameObject, zone);
            if (rt == null)
            {
                return;
            }

            rt.SetParent(zone, false);

            // Modals size themselves; stretch everything else into its zone.
            if (viewType == typeof(MulliganPanel) || viewType == typeof(PassDeviceOverlay))
            {
                return;
            }

            StretchFull(rt);
        }

        private static void EnsureOpponentHandHost(RectTransform opponentHand)
        {
            if (opponentHand.GetComponent<OpponentHandView>() == null)
            {
                opponentHand.gameObject.AddComponent<OpponentHandView>();
            }
        }

        private static void EnsurePileHosts(RectTransform opponentPiles, RectTransform localPiles)
        {
            if (opponentPiles.GetComponent<PileStripView>() == null)
            {
                opponentPiles.gameObject.AddComponent<PileStripView>();
            }

            if (localPiles.GetComponent<PileStripView>() == null)
            {
                localPiles.gameObject.AddComponent<PileStripView>();
            }
        }

        private static void StretchHost(Component host)
        {
            if (host == null)
            {
                return;
            }

            var rt = host.transform as RectTransform;
            if (rt != null)
            {
                StretchFull(rt);
            }
        }

        private static void EnsureHorizontalLayout(RectTransform zone)
        {
            if (zone == null)
            {
                return;
            }

            var hlg = zone.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = zone.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 12f;
            hlg.padding = new RectOffset(8, 8, 8, 8);
        }

        private static RectTransform EnsureZone(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = EnsureChild(parent, name);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            return rt;
        }

        private static RectTransform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var existingRt = existing as RectTransform;
                if (existingRt != null)
                {
                    return existingRt;
                }

                return EnsureRectTransformOnUiObject(existing.gameObject, parent);
            }

            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            StretchFull(rt);
            return rt;
        }

        public static void StretchFull(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Converts a plain Transform UI object to RectTransform by recreating the GameObject.
        /// Never call this on BattleCompositionRoot.
        /// </summary>
        public static RectTransform EnsureRectTransformOnUiObject(GameObject go, Transform preferredParent)
        {
            if (go == null)
            {
                return null;
            }

            if (go.GetComponent<BattleCompositionRoot>() != null)
            {
                return go.GetComponent<RectTransform>();
            }

            var existing = go.GetComponent<RectTransform>();
            if (existing != null)
            {
                if (preferredParent != null && go.transform.parent != preferredParent)
                {
                    existing.SetParent(preferredParent, false);
                }

                return existing;
            }

            var parent = preferredParent != null ? preferredParent : go.transform.parent;
            var sibling = go.transform.GetSiblingIndex();
            var name = go.name;
            var active = go.activeSelf;

            var replacement = new GameObject(name, typeof(RectTransform));
            var rt = replacement.GetComponent<RectTransform>();
            if (parent != null)
            {
                rt.SetParent(parent, false);
                rt.SetSiblingIndex(sibling);
            }

            while (go.transform.childCount > 0)
            {
                go.transform.GetChild(0).SetParent(rt, false);
            }

            var components = go.GetComponents<Component>();
            for (var i = 0; i < components.Length; i++)
            {
                var comp = components[i];
                if (comp == null || comp is Transform)
                {
                    continue;
                }

                var type = comp.GetType();
                if (replacement.GetComponent(type) != null)
                {
                    continue;
                }

                var copy = replacement.AddComponent(type);
                if (copy != null)
                {
                    var json = JsonUtility.ToJson(comp);
                    if (!string.IsNullOrEmpty(json) && json != "{}")
                    {
                        JsonUtility.FromJsonOverwrite(json, copy);
                    }
                }
            }

            replacement.SetActive(active);
            Object.DestroyImmediate(go);
            return rt;
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
