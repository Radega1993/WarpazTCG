using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Presentation;

namespace Warpaz.Tests.EditMode
{
    /// <summary>PRE-SPRINT-2 presentation checks. Does not touch Domain rules.</summary>
    public sealed class BattleBoardUiIntegrationTests
    {
        private const string ArtRoot = "Assets/Warpaz/Presentation/Art/UI/Battle/";
        private const string ScenePath = "Assets/Warpaz/Presentation/Scenes/Battle.unity";
        private const string PrefabPath = "Assets/Warpaz/Presentation/Prefabs/Battle/CharacterSlotView.prefab";

        [Test]
        public void RequiredBattleSprites_ExistAsSingleSprites()
        {
            AssertSprite(ArtRoot + "warpaz_battleboard_background.png");
            AssertSprite(ArtRoot + "warpaz_character_slot_frame.png");
            AssertSprite(ArtRoot + "warpaz_pa_icon.png");
            AssertSprite(ArtRoot + "warpaz_resolution_icon.png");
            AssertSprite(ArtRoot + "warpaz_panel_frame_9slice.png");

            var panelImporter = AssetImporter.GetAtPath(ArtRoot + "warpaz_panel_frame_9slice.png") as TextureImporter;
            Assert.IsNotNull(panelImporter);
            Assert.Greater(panelImporter.spriteBorder.x, 0f);
            Assert.Greater(panelImporter.spriteBorder.y, 0f);
            Assert.Greater(panelImporter.spriteBorder.z, 0f);
            Assert.Greater(panelImporter.spriteBorder.w, 0f);
        }

        [Test]
        public void CharacterSlotPrefab_Exists_AndHasFrame()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "CharacterSlotView prefab missing");
            var view = prefab.GetComponent<CharacterSlotView>();
            Assert.IsNotNull(view);
            Assert.IsNotNull(view.Frame);
        }

        [Test]
        public void BattleScene_Background_DoesNotBlockRaycasts_AndBinderAssigned()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var canvas = GameObject.Find("Canvas");
                Assert.IsNotNull(canvas);
                var bg = canvas.transform.Find("Background");
                Assert.IsNotNull(bg);
                var image = bg.GetComponent<Image>();
                Assert.IsNotNull(image);
                Assert.IsFalse(image.raycastTarget, "Background must not block raycasts");
                Assert.IsNotNull(image.sprite, "Background sprite must be assigned");

                var root = GameObject.Find("BattleCompositionRoot");
                Assert.IsNotNull(root);
                var binder = root.GetComponent<BattleBoardArtBinder>();
                Assert.IsNotNull(binder, "BattleBoardArtBinder missing on root");

                var board = root.GetComponentInChildren<BoardSlotsView>(true);
                Assert.IsNotNull(board);
                for (var i = 0; i < 8; i++)
                {
                    var slot = board.transform.Find("BoardSlot" + i);
                    Assert.IsNotNull(slot, "BoardSlot" + i + " missing");
                    Assert.IsNotNull(slot.GetComponent<CharacterSlotView>());
                }

                var sliced = 0;
                var images = root.GetComponentsInChildren<Image>(true);
                for (var i = 0; i < images.Length; i++)
                {
                    if (images[i].type == Image.Type.Sliced && images[i].sprite != null)
                    {
                        sliced++;
                    }
                }

                Assert.Greater(sliced, 0, "Expected at least one sliced panel frame");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void AssertSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.IsNotNull(sprite, "Missing sprite at " + path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, path);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, path);
        }
    }
}
