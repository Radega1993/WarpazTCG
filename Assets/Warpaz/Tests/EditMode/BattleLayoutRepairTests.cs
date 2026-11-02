using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Presentation;

namespace Warpaz.Tests.EditMode
{
    /// <summary>S2-UI-LAYOUT-REPAIR-01 geometry and grey-card contracts. Presentation only.</summary>
    public sealed class BattleLayoutRepairTests
    {
        private const string ScenePath = "Assets/Warpaz/Presentation/Scenes/Battle.unity";

        [Test]
        public void BattleCompositionRoot_AfterEnsureLayout_IsFullStretch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var root = Object.FindFirstObjectByType<BattleCompositionRoot>();
                Assert.IsNotNull(root);
                var safe = BattleLayoutBuilder.EnsureLayout(root);
                Assert.IsNotNull(root, "Root must survive layout build");
                Assert.IsNotNull(safe, "SafeAreaRoot required");
                Assert.AreEqual(Vector2.zero, safe.anchorMin);
                Assert.AreEqual(Vector2.one, safe.anchorMax);
                Assert.AreEqual(Vector2.zero, safe.offsetMin);
                Assert.AreEqual(Vector2.zero, safe.offsetMax);

                var rootRt = root.GetComponent<RectTransform>();
                if (rootRt != null)
                {
                    Assert.AreEqual(Vector2.zero, rootRt.anchorMin);
                    Assert.AreEqual(Vector2.one, rootRt.anchorMax);
                }
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void LegacyBackgroundPanel_IsDisabled()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var root = Object.FindFirstObjectByType<BattleCompositionRoot>();
                BattleLayoutBuilder.EnsureLayout(root);
                var canvas = Object.FindFirstObjectByType<Canvas>();
                var panel = canvas.transform.Find("Background/Panel");
                if (panel != null)
                {
                    Assert.IsFalse(panel.gameObject.activeSelf, "Legacy Panel must be inactive");
                }
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void Background_DoesNotBlockRaycasts()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                var bg = canvas.transform.Find("Background");
                Assert.IsNotNull(bg);
                var image = bg.GetComponent<Image>();
                Assert.IsNotNull(image);
                Assert.IsFalse(image.raycastTarget);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void MainZones_Exist_AndDoNotFullyOverlap()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var root = Object.FindFirstObjectByType<BattleCompositionRoot>();
                BattleLayoutBuilder.EnsureLayout(root);

                var oppBoard = FindZone(root, BattleLayoutBuilder.OpponentBoardName);
                var locBoard = FindZone(root, BattleLayoutBuilder.LocalBoardName);
                var oppHand = FindZone(root, BattleLayoutBuilder.OpponentHandName);
                var locHand = FindZone(root, BattleLayoutBuilder.LocalHandName);
                var oppHud = FindZone(root, BattleLayoutBuilder.OpponentHudName);
                var locHud = FindZone(root, BattleLayoutBuilder.LocalHudName);
                var actions = FindZone(root, BattleLayoutBuilder.ActionPanelName);

                Assert.IsNotNull(oppBoard);
                Assert.IsNotNull(locBoard);
                Assert.IsNotNull(oppHand);
                Assert.IsNotNull(locHand);
                Assert.IsNotNull(oppHud);
                Assert.IsNotNull(locHud);
                Assert.IsNotNull(actions);

                // Rival above local: maxY of local < minY of opponent (normalized anchors).
                Assert.Greater(oppBoard.anchorMin.y, locBoard.anchorMax.y - 0.001f);
                Assert.Greater(oppHand.anchorMin.y, locHand.anchorMax.y - 0.001f);
                Assert.Greater(oppHud.anchorMin.y, locHud.anchorMax.y - 0.001f);

                // Action panel outside local hand horizontally or below hand band.
                Assert.Greater(actions.anchorMin.x, locHand.anchorMax.x - 0.05f);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void BoardSlots_AreInsideBoardZones()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var root = Object.FindFirstObjectByType<BattleCompositionRoot>();
                BattleLayoutBuilder.EnsureLayout(root);

                var oppBoard = FindZone(root, BattleLayoutBuilder.OpponentBoardName);
                var locBoard = FindZone(root, BattleLayoutBuilder.LocalBoardName);

                for (var i = 0; i < 4; i++)
                {
                    var slot = FindNamed(root, "BoardSlot" + i) ?? FindNamed(Object.FindFirstObjectByType<Canvas>().transform, "BoardSlot" + i);
                    Assert.IsNotNull(slot, "BoardSlot" + i);
                    Assert.AreSame(locBoard, slot.parent, "Local slots under LocalBoard");
                }

                for (var i = 4; i < 8; i++)
                {
                    var slot = FindNamed(root, "BoardSlot" + i) ?? FindNamed(Object.FindFirstObjectByType<Canvas>().transform, "BoardSlot" + i);
                    Assert.IsNotNull(slot, "BoardSlot" + i);
                    Assert.AreSame(oppBoard, slot.parent, "Rival slots under OpponentBoard");
                }
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void FaceUpCard_HasGreyImage_NoIllustrationSprite()
        {
            var go = new GameObject("TestCard", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var view = go.AddComponent<CardPlaceholderView>();
                view.EnsureChrome();
                view.SetFaceUp("Alpha", "Character", "PA 2", "P3");

                Assert.IsTrue(view.HasGreyBackgroundImage());
                var bg = go.transform.Find("GrayBackground");
                Assert.IsNotNull(bg);
                var image = bg.GetComponent<Image>();
                Assert.IsNotNull(image);
                Assert.IsNotNull(image.sprite, "Chrome uses sliced rounded plate");
                Assert.AreEqual(BattleUiColors.FaceUpBackground, image.color);
                Assert.IsNotNull(go.transform.Find("CostGem"));
                Assert.IsNotNull(go.transform.Find("PowerGem"));
                Assert.IsNotNull(go.transform.Find("TypeBar"));

                var snapshot = view.GetVisibleTextSnapshot();
                Assert.IsTrue(snapshot.Contains("Alpha"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FaceDownCard_HasNoPrivateData()
        {
            var go = new GameObject("TestBack", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var view = go.AddComponent<CardPlaceholderView>();
                view.EnsureChrome();
                view.SetFaceDown();
                var snap = view.GetVisibleTextSnapshot();
                Assert.AreEqual("|||", snap);
                var cardBack = go.transform.Find("CardBack");
                Assert.IsNotNull(cardBack);
                var backImg = cardBack.GetComponent<Image>();
                Assert.IsNotNull(backImg);
                Assert.AreEqual(backImg.sprite != null, cardBack.gameObject.activeSelf);
                var bg = go.transform.Find("GrayBackground").GetComponent<Image>();
                if (backImg.sprite == null)
                {
                    Assert.AreEqual(BattleUiColors.FaceDownBackground, bg.color);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HandSelection_RaisesScaleAndOverlay()
        {
            var go = new GameObject("SelCard", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var view = go.AddComponent<CardPlaceholderView>();
                view.EnsureChrome();
                view.SetFaceUp("Beta", "Character", "PA 1", "P1");
                view.SetSelected(true);
                Assert.Greater(go.transform.localScale.x, 1.01f);
                Assert.IsTrue(go.transform.Find("SelectedOverlay").gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Hover_OutsideHand_DoesNotEnlarge()
        {
            var go = new GameObject("HoverCard", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var view = go.AddComponent<CardPlaceholderView>();
                view.EnsureChrome();
                view.SetFaceUp("Gamma", "Character", "PA 2", "P2");
                view.SetSelected(true);
                var selectedScale = go.transform.localScale.x;
                view.SetHovered(true);
                // Not under LocalHand → hover ignored so board slots keep receiving clicks.
                Assert.AreEqual(selectedScale, go.transform.localScale.x, 0.001f);
                Assert.IsFalse(view.IsHoveredForTests);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterSlot_UsesCardContainerGreyPlate()
        {
            var go = new GameObject("Slot", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var slot = go.AddComponent<CharacterSlotView>();
                slot.EnsureWired();
                slot.SetCard("Gamma", "Character", "P4", "Ready");
                Assert.IsNotNull(slot.CardContainer);
                Assert.IsNotNull(slot.CardView);
                Assert.IsTrue(slot.CardView.HasGreyBackgroundImage());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FindContracts_SurviveLayout(
            [Values("KeepHandButton", "ReplaceHandButton", "EndTurnButton", "ConfirmButton")] string name)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var root = Object.FindFirstObjectByType<BattleCompositionRoot>();
                BattleLayoutBuilder.EnsureLayout(root);
                var found = FindNamed(root, name);
                if (found == null)
                {
                    var canvas = Object.FindFirstObjectByType<Canvas>();
                    found = FindNamed(canvas.transform, name);
                }

                Assert.IsNotNull(found, name + " must remain findable");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static RectTransform FindZone(BattleCompositionRoot root, string name)
        {
            var t = FindNamed(root, name);
            if (t == null)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    t = FindNamed(canvas.transform, name);
                }
            }

            return t as RectTransform;
        }

        private static Transform FindNamed(Component root, string name)
        {
            return FindNamed(root.transform, name);
        }

        private static Transform FindNamed(Transform root, string name)
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
                var found = FindNamed(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
