using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Warpaz.Presentation;

namespace Warpaz.Presentation.Editor
{
    public static class WarpazShellSceneBuilder
    {
        private const string ScenesFolder = "Assets/Warpaz/Presentation/Scenes";
        private const string ThemePath = "Assets/Warpaz/Presentation/UI/UiTheme.asset";
        private const string FontPath = "Assets/Warpaz/Presentation/UI/Fonts/WarpazRuntimeFont.asset";

        [MenuItem("WARPAZ/Presentation/Build Shell Scenes")]
        public static void BuildShellScenes()
        {
            Directory.CreateDirectory(Path.Combine(UnityEngine.Application.dataPath, "Warpaz/Presentation/Scenes"));
            Directory.CreateDirectory(Path.Combine(UnityEngine.Application.dataPath, "Warpaz/Presentation/UI/Fonts"));

            var theme = LoadOrCreateTheme();
            EnsureTmpFont();

            BuildBootstrap(theme);
            BuildMainMenu(theme);
            BuildStub(SceneIds.DeckBuilder, "Constructor de mazos", theme);
            BuildStub(SceneIds.Battle, "Batalla", theme);
            BuildStub(SceneIds.Laboratory, "Laboratorio", theme);

            EditorBuildSettings.scenes = new[]
            {
                NewBuildScene(SceneIds.Bootstrap),
                NewBuildScene(SceneIds.MainMenu),
                NewBuildScene(SceneIds.DeckBuilder),
                NewBuildScene(SceneIds.Battle),
                NewBuildScene(SceneIds.Laboratory)
            };

            AssetDatabase.SaveAssets();
            Debug.Log("WARPAZ shell scenes built. Bootstrap is build index 0.");
        }

        private static EditorBuildSettingsScene NewBuildScene(string name)
        {
            return new EditorBuildSettingsScene(ScenePath(name), true);
        }

        private static string ScenePath(string name)
        {
            return ScenesFolder + "/" + name + ".unity";
        }

        private static UiTheme LoadOrCreateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme != null)
            {
                return theme;
            }

            theme = ScriptableObject.CreateInstance<UiTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            return theme;
        }

        private static void EnsureTmpFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null)
            {
                return;
            }

            var source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (source == null)
            {
                Debug.LogWarning("WARPAZ: TMP default font is missing and LegacyRuntime.ttf was not found.");
                return;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(source);
            AssetDatabase.CreateAsset(fontAsset, FontPath);
            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            if (fontAsset.atlasTexture != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }
        }

        private static TMP_FontAsset Font()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        }

        private static void BuildBootstrap(UiTheme theme)
        {
            var scene = NewEmpty(SceneIds.Bootstrap);
            CreateCamera(theme);
            CreateEventSystem();
            var canvas = CreateCanvas();
            var background = CreateImage("Background", canvas.transform, theme.Background, FullStretch());
            var panel = CreateImage("Panel", background.transform, theme.Panel, Centered(720, 420));

            var loading = CreatePanel("LoadingRoot", panel.transform, Color.clear, FullStretch());
            var status = CreateTmp("StatusText", loading.transform, "Cargando WARPAZ TCG…", 28, theme, FullStretch());
            status.alignment = TextAlignmentOptions.Center;

            var error = CreatePanel("ErrorRoot", panel.transform, Color.clear, FullStretch());
            error.SetActive(false);
            var errorText = CreateTmp("ErrorText", error.transform, string.Empty, 22, theme, PaddedStretch(24));
            errorText.alignment = TextAlignmentOptions.TopLeft;
            errorText.textWrappingMode = TextWrappingModes.Normal;

            var controller = canvas.AddComponent<BootstrapController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.FindProperty("loadingRoot").objectReferenceValue = loading;
            serialized.FindProperty("errorRoot").objectReferenceValue = error;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.FindProperty("errorText").objectReferenceValue = errorText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath(SceneIds.Bootstrap));
        }

        private static void BuildMainMenu(UiTheme theme)
        {
            var scene = NewEmpty(SceneIds.MainMenu);
            CreateCamera(theme);
            CreateEventSystem();
            var canvas = CreateCanvas();
            var background = CreateImage("Background", canvas.transform, theme.Background, FullStretch());
            var panel = CreateImage("Panel", background.transform, theme.Panel, Centered(640, 780));

            var title = CreateTmp("Title", panel.transform, "WARPAZ TCG", 56, theme, new Vector2(0.5f, 1f), new Vector2(0, -48), new Vector2(560, 72));
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;

            var version = CreateTmp("VersionText", panel.transform, "Build · Contenido · Reglas", 20, theme, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(560, 48));
            version.alignment = TextAlignmentOptions.Center;
            version.textWrappingMode = TextWrappingModes.Normal;

            var column = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            column.transform.SetParent(panel.transform, false);
            var columnRect = column.GetComponent<RectTransform>();
            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnRect.pivot = new Vector2(0.5f, 0.5f);
            columnRect.anchoredPosition = new Vector2(0, -40);
            columnRect.sizeDelta = new Vector2(420, 480);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var play = CreateMenuButton("PlayButton", column.transform, "Jugar", theme);
            var decks = CreateMenuButton("DecksButton", column.transform, "Mazos", theme);
            var cards = CreateMenuButton("CardsButton", column.transform, "Cartas", theme);
            var lab = CreateMenuButton("LaboratoryButton", column.transform, "Laboratorio", theme);
            var settings = CreateMenuButton("SettingsButton", column.transform, "Ajustes", theme);
            var quit = CreateMenuButton("QuitButton", column.transform, "Salir", theme);

            var comingSoon = CreateTmp("ComingSoonText", panel.transform, string.Empty, 22, theme, new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(560, 40));
            comingSoon.alignment = TextAlignmentOptions.Center;
            comingSoon.color = theme.Accent;

            var controller = canvas.AddComponent<MainMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("versionText").objectReferenceValue = version;
            serialized.FindProperty("comingSoonText").objectReferenceValue = comingSoon;
            serialized.FindProperty("playButton").objectReferenceValue = play;
            serialized.FindProperty("decksButton").objectReferenceValue = decks;
            serialized.FindProperty("cardsButton").objectReferenceValue = cards;
            serialized.FindProperty("laboratoryButton").objectReferenceValue = lab;
            serialized.FindProperty("settingsButton").objectReferenceValue = settings;
            serialized.FindProperty("quitButton").objectReferenceValue = quit;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath(SceneIds.MainMenu));
        }

        private static void BuildStub(string sceneId, string title, UiTheme theme)
        {
            var scene = NewEmpty(sceneId);
            CreateCamera(theme);
            CreateEventSystem();
            var canvas = CreateCanvas();
            var background = CreateImage("Background", canvas.transform, theme.Background, FullStretch());
            var panel = CreateImage("Panel", background.transform, theme.Panel, Centered(640, 360));

            var titleText = CreateTmp("Title", panel.transform, title, 40, theme, new Vector2(0.5f, 1f), new Vector2(0, -64), new Vector2(560, 64));
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            var hint = CreateTmp("Hint", panel.transform, "Pantalla provisional.", 22, theme, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 48));
            hint.alignment = TextAlignmentOptions.Center;

            var back = CreateMenuButton("BackButton", panel.transform, "Volver", theme);
            var backRect = back.GetComponent<RectTransform>();
            Object.DestroyImmediate(back.GetComponent<LayoutElement>());
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0, 36);
            backRect.sizeDelta = new Vector2(280, 56);

            var controller = canvas.AddComponent<StubSceneController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.FindProperty("titleText").objectReferenceValue = titleText;
            serialized.FindProperty("screenTitle").stringValue = title;
            serialized.FindProperty("backButton").objectReferenceValue = back;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath(sceneId));
        }

        private static Scene NewEmpty(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            return scene;
        }

        private static void CreateCamera(UiTheme theme)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = theme.Background;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            go.AddComponent<AudioListener>();
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100;
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color, RectSetup rect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            ApplyRect(go.GetComponent<RectTransform>(), rect);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = name == "Background" || name == "Panel";
            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color, RectSetup rect)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ApplyRect(go.GetComponent<RectTransform>(), rect);
            if (color.a > 0f)
            {
                var image = go.AddComponent<Image>();
                image.color = color;
            }

            return go;
        }

        private static TextMeshProUGUI CreateTmp(
            string name,
            Transform parent,
            string text,
            float size,
            UiTheme theme,
            RectSetup rect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            ApplyRect(go.GetComponent<RectTransform>(), rect);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = theme.Text;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            var font = Font();
            if (font != null)
            {
                tmp.font = font;
            }

            return tmp;
        }

        private static TextMeshProUGUI CreateTmp(
            string name,
            Transform parent,
            string text,
            float size,
            UiTheme theme,
            Vector2 anchor,
            Vector2 position,
            Vector2 sizeDelta)
        {
            return CreateTmp(name, parent, text, size, theme, Anchored(anchor, position, sizeDelta));
        }

        private static Button CreateMenuButton(string name, Transform parent, string label, UiTheme theme)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420, 56);
            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 56;
            layout.preferredHeight = 56;
            var image = go.GetComponent<Image>();
            image.color = Color.white;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            theme.ApplyButton(button, null);

            var tmp = CreateTmp("Label", go.transform, label, 26, theme, FullStretch());
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            theme.ApplyButton(button, tmp);
            return button;
        }

        private struct RectSetup
        {
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Vector2 OffsetMin;
            public Vector2 OffsetMax;
            public bool UseOffsets;
        }

        private static RectSetup FullStretch()
        {
            return new RectSetup
            {
                AnchorMin = Vector2.zero,
                AnchorMax = Vector2.one,
                Pivot = new Vector2(0.5f, 0.5f),
                UseOffsets = true,
                OffsetMin = Vector2.zero,
                OffsetMax = Vector2.zero
            };
        }

        private static RectSetup PaddedStretch(float pad)
        {
            return new RectSetup
            {
                AnchorMin = Vector2.zero,
                AnchorMax = Vector2.one,
                Pivot = new Vector2(0.5f, 0.5f),
                UseOffsets = true,
                OffsetMin = new Vector2(pad, pad),
                OffsetMax = new Vector2(-pad, -pad)
            };
        }

        private static RectSetup Centered(float width, float height)
        {
            return new RectSetup
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                AnchoredPosition = Vector2.zero,
                SizeDelta = new Vector2(width, height)
            };
        }

        private static RectSetup Anchored(Vector2 anchor, Vector2 position, Vector2 sizeDelta)
        {
            return new RectSetup
            {
                AnchorMin = anchor,
                AnchorMax = anchor,
                Pivot = new Vector2(0.5f, 0.5f),
                AnchoredPosition = position,
                SizeDelta = sizeDelta
            };
        }

        private static void ApplyRect(RectTransform rect, RectSetup setup)
        {
            rect.anchorMin = setup.AnchorMin;
            rect.anchorMax = setup.AnchorMax;
            rect.pivot = setup.Pivot;
            if (setup.UseOffsets)
            {
                rect.offsetMin = setup.OffsetMin;
                rect.offsetMax = setup.OffsetMax;
            }
            else
            {
                rect.anchoredPosition = setup.AnchoredPosition;
                rect.sizeDelta = setup.SizeDelta;
            }
        }
    }
}
