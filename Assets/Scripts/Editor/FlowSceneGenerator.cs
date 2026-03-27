#if UNITY_EDITOR
using MakeItOut.Runtime.Dev;
using MakeItOut.Runtime.Flow;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.MazeGeneration;
using MakeItOut.Runtime.Player;
using MakeItOut.Runtime.Progression;
using MakeItOut.Runtime.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MakeItOut.EditorTools
{
    /// <summary>
    /// Generates the three production flow scenes (Bootstrap, MainMenu, Game) complete
    /// with their full UI panel hierarchies and all HudManager references pre-wired.
    ///
    /// Run via: Tools → Make It Out → Create Flow Scenes
    /// </summary>
    public static class FlowSceneGenerator
    {
        private const string SceneRoot          = "Assets/Scenes/Flow";
        private const string BootstrapScenePath = "Assets/Scenes/Flow/Bootstrap.unity";
        private const string MainMenuScenePath  = "Assets/Scenes/Flow/MainMenu.unity";
        private const string GameScenePath      = "Assets/Scenes/Flow/Game.unity";

        // ──────────────────────────────────────────────────────────────────────────
        // Entry points
        // ──────────────────────────────────────────────────────────────────────────

        public static void CreateFlowScenes()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(SceneRoot);

            CreateBootstrapScene();
            CreateMainMenuScene();
            CreateGameScene();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Flow scenes generated: Bootstrap, MainMenu, Game.");
        }

        [MenuItem("Tools/Make It Out/Create Flow Scenes")]
        public static void CreateFlowScenesMenu() => CreateFlowScenes();

        // ──────────────────────────────────────────────────────────────────────────
        // Scene builders
        // ──────────────────────────────────────────────────────────────────────────

        private static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var serviceLocatorGo = new GameObject("ServiceLocator");
            ServiceLocator locator = serviceLocatorGo.AddComponent<ServiceLocator>();
            locator.RegistryAsset = AssetDatabase.LoadAssetAtPath<LevelRegistryAsset>("Assets/Data/LevelRegistry.asset");

            new GameObject("GameManager").AddComponent<GameManager>();

            new GameObject("LevelLoader").AddComponent<LevelLoader>();

            var bootstrapLoaderGo = new GameObject("BootstrapSceneLoader");
            BootstrapSceneLoader loader = bootstrapLoaderGo.AddComponent<BootstrapSceneLoader>();
            loader.MainMenuSceneName = "MainMenu";

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera — required to silence Unity's "no camera" warning.
            // cullingMask = 0 so it renders nothing; UI lives on a Screen-Space Overlay canvas.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;
            camGo.AddComponent<AudioListener>();

            // Canvas
            GameObject canvasGo = CreateCanvas();

            // EventSystem
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // Panels — all start inactive; HudManager activates the correct one at runtime.
            Transform ct = canvasGo.transform;
            GameObject mainMenuPanel    = CreatePanel(ct, "MainMenuPanel");    BuildMainMenu(mainMenuPanel);     mainMenuPanel.SetActive(false);
            GameObject levelSelectPanel = CreatePanel(ct, "LevelSelectPanel"); BuildLevelSelect(levelSelectPanel); levelSelectPanel.SetActive(false);
            GameObject levelIntroPanel  = CreatePanel(ct, "LevelIntroPanel");  BuildLevelIntro(levelIntroPanel);   levelIntroPanel.SetActive(false);
            GameObject highScoresPanel  = CreatePanel(ct, "HighScoresPanel");  BuildHighScores(highScoresPanel);   highScoresPanel.SetActive(false);

            // HudManager lives outside the Canvas hierarchy.
            var hudRoot = new GameObject("HudManager");
            HudManager hudManager = hudRoot.AddComponent<HudManager>();

            // Wire all panel references so the inspector fields are pre-assigned.
            var hudSo = new SerializedObject(hudManager);
            hudSo.FindProperty("MainMenuPanel").objectReferenceValue            = mainMenuPanel;
            hudSo.FindProperty("LevelSelectPanel").objectReferenceValue         = levelSelectPanel;
            hudSo.FindProperty("LevelIntroPanel").objectReferenceValue          = levelIntroPanel;
            hudSo.FindProperty("HighScoresPanel").objectReferenceValue          = highScoresPanel;
            hudSo.FindProperty("_levelSelectController").objectReferenceValue   =
                levelSelectPanel.GetComponent<LevelSelectPanelController>();
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // DevSceneBootstrap is present but guarded — only auto-runs in Dev* scenes.
            new GameObject("DevSceneBootstrap").AddComponent<DevSceneBootstrap>();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Materials
            Material blockMaterial  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/DevEnv/Materials/DevBlock.mat");
            Material ladderMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/DevEnv/Materials/DevLadder.mat");
            Material stairMaterial  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/DevEnv/Materials/DevStair.mat");
            Material exitMaterial   = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/DevEnv/Materials/DevExit.mat");
            Mesh cubeMesh = GetPrimitiveMesh(PrimitiveType.Cube);

            // Systems
            var systems = new GameObject("Systems");
            systems.AddComponent<CameraOrientation>();
            MazeGenerator       mazeGenerator       = systems.AddComponent<MazeGenerator>();
            ChunkManager        chunkManager        = systems.AddComponent<ChunkManager>();
            FeaturePropRenderer featureRenderer     = systems.AddComponent<FeaturePropRenderer>();
            TransparencyManager transparencyManager = systems.AddComponent<TransparencyManager>();
            HudManager          hudManager          = systems.AddComponent<HudManager>();

            SetObjectReference(chunkManager, "_blockMaterial",      blockMaterial);
            SetObjectReference(chunkManager, "_featurePropRenderer", featureRenderer);
            SetObjectReference(featureRenderer, "_ladderMesh",      cubeMesh);
            SetObjectReference(featureRenderer, "_stairMesh",       cubeMesh);
            SetObjectReference(featureRenderer, "_exitMesh",        cubeMesh);
            SetObjectReference(featureRenderer, "_ladderMaterial",  ladderMaterial);
            SetObjectReference(featureRenderer, "_stairMaterial",   stairMaterial);
            SetObjectReference(featureRenderer, "_exitMaterial",    exitMaterial);

            // Player
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0.9f, 0f);
            PlayerController playerController = player.AddComponent<PlayerController>();

            // Camera
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cameraGo.AddComponent<AudioListener>();
            CameraController cameraController = cameraGo.AddComponent<CameraController>();
            SetObjectReference(cameraController, "_playerTransform",     player.transform);
            SetObjectReference(cameraController, "_cam",                 cam);
            SetObjectReference(cameraController, "_transparencyManager", transparencyManager);

            player.transform.position   = new Vector3(63f, 63f, 63f);
            cameraGo.transform.position = player.transform.position - Vector3.forward * 20f;

            // Canvas + EventSystem for game UI
            GameObject canvasGo = CreateCanvas();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // Game panels
            Transform ct = canvasGo.transform;
            GameObject loadingPanel = CreatePanel(ct, "LoadingPanel");      BuildLoadingPanel(loadingPanel); loadingPanel.SetActive(false);
            GameObject hudPanel     = CreatePanel(ct, "HudPanel");          BuildHudPanel(hudPanel);         hudPanel.SetActive(false);
            GameObject pausePanel   = CreatePanel(ct, "PausePanel");        BuildPausePanel(pausePanel);     pausePanel.SetActive(false);
            GameObject resultPanel  = CreatePanel(ct, "LevelResultPanel");  BuildResultPanel(resultPanel);   resultPanel.SetActive(false);

            // Screen-flash overlay
            CreateSwitchFlash(ct);

            // Wire HudManager references
            var hudSo = new SerializedObject(hudManager);
            hudSo.FindProperty("LoadingPanel").objectReferenceValue     = loadingPanel;
            hudSo.FindProperty("HudPanel").objectReferenceValue         = hudPanel;
            hudSo.FindProperty("PausePanel").objectReferenceValue       = pausePanel;
            hudSo.FindProperty("LevelResultPanel").objectReferenceValue = resultPanel;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Suppress unused-variable warnings
            _ = mazeGenerator;
            _ = playerController;

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Canvas helper
        // ──────────────────────────────────────────────────────────────────────────

        private static GameObject CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
            return go;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Primitive UI element factories
        // ──────────────────────────────────────────────────────────────────────────

        private static GameObject CreatePanel(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            Stretch(panel.GetComponent<RectTransform>());
            return panel;
        }

        /// <summary>Full-parent-stretch container. Every intermediate layout container must
        /// use this so that anchor-based children resolve against the full panel area.</summary>
        private static GameObject CreateContainer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        private static GameObject CreateBackground(Transform parent, string name = "Background")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            go.GetComponent<Image>().color = UiStyle.BackgroundDark;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, int size,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text      = text;
            label.fontSize  = size;
            label.alignment = alignment;
            label.color     = color ?? UiStyle.TextPrimary;
            return label;
        }

        private static UiButton CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UiButton));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            // UiButton.Awake() applies ButtonNormal colour at runtime; Image colour is set there.
            CreateText(go.transform, "Label", label, 30, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            return go.GetComponent<UiButton>();
        }

        private static StarRatingDisplay CreateStars(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(StarRatingDisplay));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.spacing               = 8f;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment         = TextAnchor.MiddleCenter;

            for (int i = 0; i < 5; i++)
            {
                var star = new GameObject($"Star_{i + 1}", typeof(RectTransform), typeof(Image));
                star.transform.SetParent(root.transform, false);
                star.GetComponent<Image>().color = UiStyle.StarEmpty;
                LayoutElement le = star.AddComponent<LayoutElement>();
                le.minWidth = 20f; le.preferredWidth  = 24f;
                le.minHeight = 20f; le.preferredHeight = 24f;
            }

            return root.GetComponent<StarRatingDisplay>();
        }

        private static void CreateSwitchFlash(Transform canvas)
        {
            if (canvas.Find("SwitchFlash") != null) return;

            var flash = new GameObject("SwitchFlash", typeof(RectTransform), typeof(Image), typeof(SwitchFlashController));
            flash.transform.SetParent(canvas, false);
            Stretch(flash.GetComponent<RectTransform>());
            Image img = flash.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = Color.clear;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Panel builders  (all UI content created here, saved into the scene file)
        // ──────────────────────────────────────────────────────────────────────────

        private static void BuildLoadingPanel(GameObject panel)
        {
            CreateBackground(panel.transform);
            CreateText(panel.transform, "Title", "MAKE IT OUT", 72, TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.75f), new Vector2(0.8f, 0.88f), UiStyle.AccentGold);

            var barBg = new GameObject("ProgressBarBackground", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(panel.transform, false);
            RectTransform barRt = barBg.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.25f, 0.45f); barRt.anchorMax = new Vector2(0.75f, 0.5f);
            barRt.offsetMin = barRt.offsetMax = Vector2.zero;
            barBg.GetComponent<Image>().color = UiStyle.ButtonNormal;

            var fill = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barBg.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = UiStyle.TextPrimary;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            CreateText(panel.transform, "StatusLabel", "Loading...", 28, TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.36f), new Vector2(0.8f, 0.43f), UiStyle.TextSecondary);
            panel.AddComponent<LoadingPanelController>();
        }

        private static void BuildMainMenu(GameObject panel)
        {
            CreateBackground(panel.transform);

            GameObject titleBlock = CreateContainer(panel.transform, "TitleBlock");
            CreateText(titleBlock.transform, "Title",    "MAKE IT OUT",  96, TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.65f), new Vector2(0.8f, 0.82f), UiStyle.AccentGold);
            CreateText(titleBlock.transform, "Subtitle", "find the exit", 28, TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.6f),  new Vector2(0.8f, 0.66f), UiStyle.TextSecondary);

            GameObject buttonStack = CreateContainer(panel.transform, "ButtonStack");
            CreateButton(buttonStack.transform, "PlayButton",       "PLAY",        new Vector2(0.38f, 0.42f), new Vector2(0.62f, 0.5f));
            CreateButton(buttonStack.transform, "HighScoresButton", "HIGH SCORES", new Vector2(0.38f, 0.31f), new Vector2(0.62f, 0.39f));

            CreateText(panel.transform, "VersionLabel", "v0.0.0", 20, TextAlignmentOptions.BottomRight,
                new Vector2(0.7f, 0f), new Vector2(0.98f, 0.06f), UiStyle.TextDim);
            panel.AddComponent<MainMenuPanelController>();
        }

        private static void BuildLevelSelect(GameObject panel)
        {
            CreateBackground(panel.transform);
            GameObject header = CreateContainer(panel.transform, "Header");
            CreateText(header.transform, "Title", "SELECT LEVEL", 60, TextAlignmentOptions.Center,
                new Vector2(0.3f, 0.88f), new Vector2(0.7f, 0.97f));
            CreateButton(header.transform, "BackButton", "<- BACK",
                new Vector2(0.03f, 0.9f), new Vector2(0.2f, 0.97f));

            var scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollView.transform.SetParent(panel.transform, false);
            RectTransform svRt = scrollView.GetComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.1f, 0.1f); svRt.anchorMax = new Vector2(0.9f, 0.85f);
            svRt.offsetMin = svRt.offsetMax = Vector2.zero;
            scrollView.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            Stretch(vpRt);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(10f, 0f); contentRt.offsetMax = new Vector2(-10f, 0f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;  vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollView.GetComponent<ScrollRect>();
            sr.viewport = vpRt; sr.content = contentRt;
            sr.horizontal = false; sr.vertical = true;

            LevelSelectPanelController controller = panel.AddComponent<LevelSelectPanelController>();

            // Wire inspector references so the controller doesn't rely on runtime path-finding.
            LevelButtonItem buttonPrefab = AssetDatabase.LoadAssetAtPath<LevelButtonItem>("Assets/Prefabs/LevelButton.prefab");
            var lsso = new SerializedObject(controller);
            lsso.FindProperty("_backButton").objectReferenceValue    = header.transform.Find("BackButton")?.GetComponent<UiButton>();
            lsso.FindProperty("_scrollContent").objectReferenceValue = content.transform;
            lsso.FindProperty("_scrollRect").objectReferenceValue    = sr;
            if (buttonPrefab != null)
                lsso.FindProperty("_levelButtonPrefab").objectReferenceValue = buttonPrefab;
            else
                Debug.LogWarning("[FlowSceneGenerator] LevelButton prefab not found at Assets/Prefabs/LevelButton.prefab — assign _levelButtonPrefab manually on LevelSelectPanelController.");
            lsso.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLevelButtonTemplate(Transform parent)
        {
            var root = new GameObject("LevelButtonTemplate",
                typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(UiButton), typeof(LayoutElement), typeof(LevelButtonItem));
            root.transform.SetParent(parent, false);
            root.SetActive(false);
            LayoutElement le = root.GetComponent<LayoutElement>();
            le.preferredHeight = 110f; le.minHeight = 110f;

            CreateText(root.transform, "LevelNumber",   "01",            32, TextAlignmentOptions.TopLeft,
                new Vector2(0.02f, 0.55f), new Vector2(0.14f, 0.95f), UiStyle.TextSecondary);
            CreateText(root.transform, "LevelName",     "Level Name",    30, TextAlignmentOptions.TopLeft,
                new Vector2(0.15f, 0.55f), new Vector2(0.75f, 0.95f), UiStyle.TextPrimary);
            CreateText(root.transform, "GridSizeLabel", "15 x 15 x 15", 22, TextAlignmentOptions.BottomLeft,
                new Vector2(0.15f, 0.05f), new Vector2(0.55f, 0.45f), UiStyle.TextDim);
            CreateStars(root.transform, "StarRatingDisplay",
                new Vector2(0.57f, 0.08f), new Vector2(0.9f, 0.42f));

            var lockIcon = new GameObject("LockIcon", typeof(RectTransform), typeof(Image));
            lockIcon.transform.SetParent(root.transform, false);
            RectTransform lrt = lockIcon.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.9f, 0.62f); lrt.anchorMax = new Vector2(0.98f, 0.94f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            lockIcon.GetComponent<Image>().color = UiStyle.TextDim;
        }

        private static void BuildLevelIntro(GameObject panel)
        {
            CreateBackground(panel.transform);
            CreateText(panel.transform, "LevelNumber",  "LEVEL 01",      44, TextAlignmentOptions.Center,
                new Vector2(0.35f, 0.85f), new Vector2(0.65f, 0.93f), UiStyle.TextSecondary);
            CreateText(panel.transform, "LevelName",    "Level Name",    72, TextAlignmentOptions.Center,
                new Vector2(0.2f,  0.73f), new Vector2(0.8f,  0.85f), UiStyle.AccentGold);
            CreateText(panel.transform, "GridSizeLabel","15 x 15 x 15", 32, TextAlignmentOptions.Center,
                new Vector2(0.3f,  0.66f), new Vector2(0.7f,  0.73f), UiStyle.TextSecondary);

            var targets = new GameObject("StarTargets", typeof(RectTransform), typeof(VerticalLayoutGroup));
            targets.transform.SetParent(panel.transform, false);
            RectTransform trt = targets.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.28f, 0.4f); trt.anchorMax = new Vector2(0.72f, 0.62f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            targets.GetComponent<VerticalLayoutGroup>().spacing = 4f;
            // Placeholder text — LevelIntroPanelController.OnEnable() overwrites these at runtime.
            string[] defaults = { "5 STARS  under 0:00", "4 STARS  under 0:00",
                                   "3 STARS  under 0:00", "2 STARS  under 0:00" };
            for (int i = 0; i < 4; i++)
                CreateText(targets.transform, $"StarTargetRow_{i}", defaults[i],
                    26, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

            GameObject pb = CreateContainer(panel.transform, "PersonalBestBlock");
            CreateText(pb.transform, "BestTimeLabel", "BEST  0:00", 30, TextAlignmentOptions.Center,
                new Vector2(0.32f, 0.3f), new Vector2(0.68f, 0.36f));
            CreateStars(pb.transform, "BestStarsDisplay",
                new Vector2(0.35f, 0.22f), new Vector2(0.65f, 0.28f));

            CreateText(panel.transform, "FirstTimeBadge", "FIRST ATTEMPT", 26, TextAlignmentOptions.Center,
                new Vector2(0.32f, 0.26f), new Vector2(0.68f, 0.34f), UiStyle.AccentGold);
            CreateButton(panel.transform, "StartButton", "START",
                new Vector2(0.38f, 0.13f), new Vector2(0.62f, 0.2f));
            CreateButton(panel.transform, "BackButton", "<- BACK",
                new Vector2(0.38f, 0.04f), new Vector2(0.62f, 0.11f));
            panel.AddComponent<LevelIntroPanelController>();
        }

        private static void BuildHudPanel(GameObject panel)
        {
            GameObject topLeft = CreateContainer(panel.transform, "TopLeft");
            CreateText(topLeft.transform, "RunTimer", "00:00", 36, TextAlignmentOptions.TopLeft,
                new Vector2(0.02f, 0.93f), new Vector2(0.22f, 0.99f))
                .gameObject.AddComponent<TimerDisplay>();

            GameObject topRight = CreateContainer(panel.transform, "TopRight");
            CreateText(topRight.transform, "OrientationLabel", "+Y", 32, TextAlignmentOptions.TopRight,
                new Vector2(0.78f, 0.93f), new Vector2(0.98f, 0.99f), UiStyle.TextSecondary);

            GameObject topCentre = CreateContainer(panel.transform, "TopCentre");
            CreateButton(topCentre.transform, "PauseButton", "II",
                new Vector2(0.47f, 0.93f), new Vector2(0.53f, 0.99f));
            panel.AddComponent<HudPanelController>();
        }

        private static void BuildPausePanel(GameObject panel)
        {
            CreateBackground(panel.transform);
            CreateText(panel.transform, "Title", "PAUSED", 70, TextAlignmentOptions.Center,
                new Vector2(0.3f, 0.72f), new Vector2(0.7f, 0.84f));
            CreateButton(panel.transform, "ResumeButton", "RESUME",
                new Vector2(0.36f, 0.5f),  new Vector2(0.64f, 0.58f));
            CreateButton(panel.transform, "RetryButton",  "RESTART LEVEL",
                new Vector2(0.36f, 0.39f), new Vector2(0.64f, 0.47f));
            CreateButton(panel.transform, "MenuButton",   "MAIN MENU",
                new Vector2(0.36f, 0.28f), new Vector2(0.64f, 0.36f));
            panel.AddComponent<PausePanelController>();
        }

        private static void BuildResultPanel(GameObject panel)
        {
            CreateBackground(panel.transform);

            GameObject header = CreateContainer(panel.transform, "ResultHeader");
            CreateText(header.transform, "CompleteLabel",     "YOU MADE IT OUT", 64, TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.8f), new Vector2(0.8f, 0.9f));
            CreateText(header.transform, "PersonalBestBadge", "NEW BEST!",        28, TextAlignmentOptions.Center,
                new Vector2(0.4f, 0.74f), new Vector2(0.6f, 0.8f), UiStyle.AccentGold);

            GameObject timeBlock = CreateContainer(panel.transform, "TimeBlock");
            CreateText(timeBlock.transform, "TimeLabel", "TIME",  26, TextAlignmentOptions.Center,
                new Vector2(0.45f, 0.62f), new Vector2(0.55f, 0.67f), UiStyle.TextSecondary);
            CreateText(timeBlock.transform, "TimeValue", "00:00", 54, TextAlignmentOptions.Center,
                new Vector2(0.35f, 0.54f), new Vector2(0.65f, 0.62f))
                .gameObject.AddComponent<TimerDisplay>();

            GameObject starBlock = CreateContainer(panel.transform, "StarBlock");
            CreateStars(starBlock.transform, "StarDisplay",
                new Vector2(0.33f, 0.43f), new Vector2(0.67f, 0.5f));

            GameObject statsBlock = CreateContainer(panel.transform, "StatsBlock");
            CreateText(statsBlock.transform, "SwitchCountLabel", "0 orientation switches", 24,
                TextAlignmentOptions.Center,
                new Vector2(0.3f, 0.36f), new Vector2(0.7f, 0.42f), UiStyle.TextSecondary);

            GameObject row = CreateContainer(panel.transform, "ButtonRow");
            CreateButton(row.transform, "RetryButton", "RETRY",
                new Vector2(0.23f, 0.19f), new Vector2(0.41f, 0.27f));
            CreateButton(row.transform, "NextButton",  "NEXT LEVEL",
                new Vector2(0.43f, 0.19f), new Vector2(0.61f, 0.27f));
            CreateButton(row.transform, "MenuButton",  "MENU",
                new Vector2(0.63f, 0.19f), new Vector2(0.81f, 0.27f));
            panel.AddComponent<LevelResultPanelController>();
        }

        private static void BuildHighScores(GameObject panel)
        {
            CreateBackground(panel.transform);
            GameObject header = CreateContainer(panel.transform, "Header");
            CreateText(header.transform, "Title", "HIGH SCORES", 60, TextAlignmentOptions.Center,
                new Vector2(0.3f, 0.88f), new Vector2(0.7f, 0.97f));
            CreateButton(header.transform, "BackButton", "<- BACK",
                new Vector2(0.03f, 0.9f), new Vector2(0.2f, 0.97f));

            var scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollView.transform.SetParent(panel.transform, false);
            RectTransform svRt = scrollView.GetComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.1f, 0.1f); svRt.anchorMax = new Vector2(0.9f, 0.85f);
            svRt.offsetMin = svRt.offsetMax = Vector2.zero;
            scrollView.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollView.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(10f, 0f); contentRt.offsetMax = new Vector2(-10f, 0f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;  vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            ScrollRect sr = scrollView.GetComponent<ScrollRect>();
            sr.viewport = vpRt; sr.content = contentRt;
            sr.horizontal = false; sr.vertical = true;

            CreateLeaderboardRowTemplate(content.transform);
            panel.AddComponent<HighScoresPanelController>();
        }

        private static void CreateLeaderboardRowTemplate(Transform parent)
        {
            var row = new GameObject("LeaderboardRowTemplate",
                typeof(RectTransform), typeof(LayoutElement), typeof(LeaderboardRow));
            row.transform.SetParent(parent, false);
            row.SetActive(false);
            LayoutElement le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 70f;

            CreateText(row.transform, "RankLabel",      "--",       26, TextAlignmentOptions.Left,
                new Vector2(0.02f, 0.15f), new Vector2(0.12f, 0.85f));
            CreateText(row.transform, "LevelNameLabel", "Level",    26, TextAlignmentOptions.Left,
                new Vector2(0.14f, 0.15f), new Vector2(0.45f, 0.85f));
            CreateText(row.transform, "TimeLabel",      "0:00",     26, TextAlignmentOptions.Center,
                new Vector2(0.47f, 0.15f), new Vector2(0.62f, 0.85f));
            CreateStars(row.transform, "StarDisplay",
                new Vector2(0.64f, 0.15f), new Vector2(0.96f, 0.85f));
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Utility
        // ──────────────────────────────────────────────────────────────────────────

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath,  true),
                new EditorBuildSettingsScene(GameScenePath,      true),
            };
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            int sep = folderPath.LastIndexOf('/');
            if (sep <= 0) return;
            EnsureFolder(folderPath.Substring(0, sep));
            AssetDatabase.CreateFolder(folderPath.Substring(0, sep), folderPath.Substring(sep + 1));
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }
    }
}
#endif
