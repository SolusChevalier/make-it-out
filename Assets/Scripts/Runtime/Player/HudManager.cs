using MakeItOut.Runtime.Flow;
using MakeItOut.Runtime.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MakeItOut.Runtime.Player
{
    public sealed class HudManager : MonoBehaviour
    {
        public static HudManager Instance { get; private set; }

        [Header("Panels")]
        public GameObject LoadingPanel;
        public GameObject HudPanel;
        public GameObject MainMenuPanel;
        public GameObject LevelSelectPanel;
        public GameObject LevelIntroPanel;
        public GameObject LevelResultPanel;
        public GameObject HighScoresPanel;
        public GameObject PausePanel;

        [Header("Controllers")]
        [SerializeField] private LevelSelectPanelController _levelSelectController;

        private void Awake()
        {
            Instance = this;
            EnsureCanvasAndPanels();
            if (_levelSelectController == null && LevelSelectPanel != null)
                _levelSelectController = LevelSelectPanel.GetComponent<LevelSelectPanelController>();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnStateChanged;
                GameManager.Instance.OnStateChanged += OnStateChanged;
                OnStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnStateChanged;

            if (Instance == this)
                Instance = null;
        }

        private void OnStateChanged(GameState state)
        {
            LoadingPanel?.SetActive(false);
            HudPanel?.SetActive(false);
            MainMenuPanel?.SetActive(false);
            LevelSelectPanel?.SetActive(false);
            LevelIntroPanel?.SetActive(false);
            LevelResultPanel?.SetActive(false);
            HighScoresPanel?.SetActive(false);
            PausePanel?.SetActive(false);

            switch (state)
            {
                case GameState.Boot:
                    LoadingPanel?.SetActive(true);
                    break;
                case GameState.MainMenu:
                    MainMenuPanel?.SetActive(true);
                    break;
                case GameState.LevelSelect:
                    LevelSelectPanel?.SetActive(true);
                    break;
                case GameState.LevelIntro:
                    LevelIntroPanel?.SetActive(true);
                    break;
                case GameState.LoadingLevel:
                    LoadingPanel?.SetActive(true);
                    break;
                case GameState.Playing:
                    HudPanel?.SetActive(true);
                    break;
                case GameState.Paused:
                    PausePanel?.SetActive(true);
                    break;
                case GameState.LevelResult:
                    LevelResultPanel?.SetActive(true);
                    break;
                case GameState.HighScores:
                    HighScoresPanel?.SetActive(true);
                    break;
            }

            switch (state)
            {
                case GameState.LevelSelect:
                    _levelSelectController?.RefreshButtons();
                    break;
            }
        }

        private void EnsureCanvasAndPanels()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (FindObjectOfType<EventSystem>() == null)
                _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            bool isMainMenu = SceneManager.GetActiveScene().name == "MainMenu";
            if (isMainMenu)
                EnsureMainMenuPanels(canvas.transform);
            else
                EnsureGamePanels(canvas.transform);
        }

        private void EnsureMainMenuPanels(Transform canvas)
        {
            MainMenuPanel = MainMenuPanel != null ? MainMenuPanel : CreatePanel(canvas, "MainMenuPanel");
            LevelSelectPanel = LevelSelectPanel != null ? LevelSelectPanel : CreatePanel(canvas, "LevelSelectPanel");
            LevelIntroPanel = LevelIntroPanel != null ? LevelIntroPanel : CreatePanel(canvas, "LevelIntroPanel");
            HighScoresPanel = HighScoresPanel != null ? HighScoresPanel : CreatePanel(canvas, "HighScoresPanel");

            if (MainMenuPanel.GetComponent<MainMenuPanelController>() == null)
                BuildMainMenu(MainMenuPanel);
            if (LevelSelectPanel.GetComponent<LevelSelectPanelController>() == null)
                BuildLevelSelect(LevelSelectPanel);
            if (LevelIntroPanel.GetComponent<LevelIntroPanelController>() == null)
                BuildLevelIntro(LevelIntroPanel);
            if (HighScoresPanel.GetComponent<HighScoresPanelController>() == null)
                BuildHighScores(HighScoresPanel);
        }

        private void EnsureGamePanels(Transform canvas)
        {
            LoadingPanel = LoadingPanel != null ? LoadingPanel : CreatePanel(canvas, "LoadingPanel");
            HudPanel = HudPanel != null ? HudPanel : CreatePanel(canvas, "HudPanel");
            PausePanel = PausePanel != null ? PausePanel : CreatePanel(canvas, "PausePanel");
            LevelResultPanel = LevelResultPanel != null ? LevelResultPanel : CreatePanel(canvas, "LevelResultPanel");

            if (LoadingPanel.GetComponent<LoadingPanelController>() == null)
                BuildLoadingPanel(LoadingPanel);
            if (HudPanel.GetComponent<HudPanelController>() == null)
                BuildHudPanel(HudPanel);
            if (PausePanel.GetComponent<PausePanelController>() == null)
                BuildPausePanel(PausePanel);
            if (LevelResultPanel.GetComponent<LevelResultPanelController>() == null)
                BuildResultPanel(LevelResultPanel);
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;

            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return panel;
        }

        private static GameObject CreateBackground(Transform parent, string name = "Background")
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = UiStyle.BackgroundDark;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, int size, TextAlignmentOptions alignment, Vector2 min, Vector2 max, Color? color = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color ?? UiStyle.TextPrimary;
            return label;
        }

        private static UiButton CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UiButton));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            CreateText(go.transform, "Label", label, 30, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            return go.GetComponent<UiButton>();
        }

        private static StarRatingDisplay CreateStars(Transform parent, string name, Vector2 min, Vector2 max)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(StarRatingDisplay));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < 5; i++)
            {
                GameObject star = new GameObject($"Star_{i + 1}", typeof(RectTransform), typeof(Image));
                star.transform.SetParent(root.transform, false);
                star.GetComponent<Image>().color = UiStyle.StarEmpty;
                LayoutElement le = star.AddComponent<LayoutElement>();
                le.minWidth = 20f;
                le.preferredWidth = 24f;
                le.minHeight = 20f;
                le.preferredHeight = 24f;
            }

            return root.GetComponent<StarRatingDisplay>();
        }

        private static void BuildLoadingPanel(GameObject panel)
        {
            CreateBackground(panel.transform);
            CreateText(panel.transform, "Title", "MAKE IT OUT", 72, TextAlignmentOptions.Center, new Vector2(0.2f, 0.75f), new Vector2(0.8f, 0.88f), UiStyle.AccentGold);
            GameObject barBg = new GameObject("ProgressBarBackground", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(panel.transform, false);
            RectTransform barRt = barBg.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.25f, 0.45f);
            barRt.anchorMax = new Vector2(0.75f, 0.5f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;
            barBg.GetComponent<Image>().color = UiStyle.ButtonNormal;

            GameObject fill = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barBg.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = UiStyle.TextPrimary;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0f;

            CreateText(panel.transform, "StatusLabel", "Loading...", 28, TextAlignmentOptions.Center, new Vector2(0.2f, 0.36f), new Vector2(0.8f, 0.43f), UiStyle.TextSecondary);
            panel.AddComponent<LoadingPanelController>();
        }

        private static void BuildMainMenu(GameObject panel)
        {
            CreateBackground(panel.transform);
            GameObject titleBlock = new GameObject("TitleBlock", typeof(RectTransform));
            titleBlock.transform.SetParent(panel.transform, false);
            CreateText(titleBlock.transform, "Title", "MAKE IT OUT", 96, TextAlignmentOptions.Center, new Vector2(0.2f, 0.65f), new Vector2(0.8f, 0.82f), UiStyle.AccentGold);
            CreateText(titleBlock.transform, "Subtitle", "find the exit", 28, TextAlignmentOptions.Center, new Vector2(0.2f, 0.6f), new Vector2(0.8f, 0.66f), UiStyle.TextSecondary);

            GameObject buttonStack = new GameObject("ButtonStack", typeof(RectTransform));
            buttonStack.transform.SetParent(panel.transform, false);
            CreateButton(buttonStack.transform, "PlayButton", "PLAY", new Vector2(0.38f, 0.42f), new Vector2(0.62f, 0.5f));
            CreateButton(buttonStack.transform, "HighScoresButton", "HIGH SCORES", new Vector2(0.38f, 0.31f), new Vector2(0.62f, 0.39f));

            CreateText(panel.transform, "VersionLabel", "v0.0.0", 20, TextAlignmentOptions.BottomRight, new Vector2(0.7f, 0f), new Vector2(0.98f, 0.06f), UiStyle.TextDim);
            panel.AddComponent<MainMenuPanelController>();
        }

        private static void BuildLevelSelect(GameObject panel)
        {
            CreateBackground(panel.transform);
            GameObject header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(panel.transform, false);
            CreateText(header.transform, "Title", "SELECT LEVEL", 60, TextAlignmentOptions.Center, new Vector2(0.3f, 0.88f), new Vector2(0.7f, 0.97f));
            CreateButton(header.transform, "BackButton", "<- BACK", new Vector2(0.03f, 0.9f), new Vector2(0.2f, 0.97f));

            GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollView.transform.SetParent(panel.transform, false);
            RectTransform svRt = scrollView.GetComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.1f, 0.1f);
            svRt.anchorMax = new Vector2(0.9f, 0.85f);
            svRt.offsetMin = Vector2.zero;
            svRt.offsetMax = Vector2.zero;
            scrollView.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(10f, 0f);
            contentRt.offsetMax = new Vector2(-10f, 0f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollView.GetComponent<ScrollRect>();
            sr.viewport = vpRt;
            sr.content = contentRt;
            sr.horizontal = false;
            sr.vertical = true;

            CreateLevelButtonTemplate(content.transform);
            panel.AddComponent<LevelSelectPanelController>();
        }

        private static void CreateLevelButtonTemplate(Transform parent)
        {
            GameObject root = new GameObject("LevelButtonTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UiButton), typeof(LayoutElement), typeof(LevelButtonItem));
            root.transform.SetParent(parent, false);
            root.SetActive(false);
            LayoutElement le = root.GetComponent<LayoutElement>();
            le.preferredHeight = 110f;
            le.minHeight = 110f;

            CreateText(root.transform, "LevelNumber", "01", 32, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.55f), new Vector2(0.14f, 0.95f), UiStyle.TextSecondary);
            CreateText(root.transform, "LevelName", "The Cube", 30, TextAlignmentOptions.TopLeft, new Vector2(0.15f, 0.55f), new Vector2(0.75f, 0.95f), UiStyle.TextPrimary);
            CreateText(root.transform, "GridSizeLabel", "15 x 15 x 15", 22, TextAlignmentOptions.BottomLeft, new Vector2(0.15f, 0.05f), new Vector2(0.55f, 0.45f), UiStyle.TextDim);
            CreateStars(root.transform, "StarRatingDisplay", new Vector2(0.57f, 0.08f), new Vector2(0.9f, 0.42f));

            GameObject lockIcon = new GameObject("LockIcon", typeof(RectTransform), typeof(Image));
            lockIcon.transform.SetParent(root.transform, false);
            RectTransform lrt = lockIcon.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.9f, 0.62f);
            lrt.anchorMax = new Vector2(0.98f, 0.94f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            lockIcon.GetComponent<Image>().color = UiStyle.TextDim;
        }

        private static void BuildLevelIntro(GameObject panel)
        {
            CreateBackground(panel.transform);
            CreateText(panel.transform, "LevelNumber", "LEVEL 01", 44, TextAlignmentOptions.Center, new Vector2(0.35f, 0.85f), new Vector2(0.65f, 0.93f), UiStyle.TextSecondary);
            CreateText(panel.transform, "LevelName", "The Cube", 72, TextAlignmentOptions.Center, new Vector2(0.2f, 0.73f), new Vector2(0.8f, 0.85f), UiStyle.AccentGold);
            CreateText(panel.transform, "GridSizeLabel", "15 x 15 x 15", 32, TextAlignmentOptions.Center, new Vector2(0.3f, 0.66f), new Vector2(0.7f, 0.73f), UiStyle.TextSecondary);

            GameObject targets = new GameObject("StarTargets", typeof(RectTransform), typeof(VerticalLayoutGroup));
            targets.transform.SetParent(panel.transform, false);
            RectTransform trt = targets.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.28f, 0.4f);
            trt.anchorMax = new Vector2(0.72f, 0.62f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlg = targets.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            for (int i = 0; i < 4; i++)
                CreateText(targets.transform, $"StarTargetRow_{i}", "★★★★★ under 00:00", 26, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

            GameObject pb = new GameObject("PersonalBestBlock", typeof(RectTransform));
            pb.transform.SetParent(panel.transform, false);
            CreateText(pb.transform, "BestTimeLabel", "BEST 00:00", 30, TextAlignmentOptions.Center, new Vector2(0.32f, 0.3f), new Vector2(0.68f, 0.36f));
            CreateStars(pb.transform, "BestStarsDisplay", new Vector2(0.35f, 0.22f), new Vector2(0.65f, 0.28f));
            CreateText(panel.transform, "FirstTimeBadge", "FIRST ATTEMPT", 26, TextAlignmentOptions.Center, new Vector2(0.32f, 0.26f), new Vector2(0.68f, 0.34f), UiStyle.AccentGold);
            CreateButton(panel.transform, "StartButton", "START", new Vector2(0.38f, 0.13f), new Vector2(0.62f, 0.2f));
            CreateButton(panel.transform, "BackButton", "<- BACK", new Vector2(0.38f, 0.04f), new Vector2(0.62f, 0.11f));
            panel.AddComponent<LevelIntroPanelController>();
        }

        private static void BuildHudPanel(GameObject panel)
        {
            GameObject topLeft = new GameObject("TopLeft", typeof(RectTransform));
            topLeft.transform.SetParent(panel.transform, false);
            CreateText(topLeft.transform, "RunTimer", "00:00", 36, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.93f), new Vector2(0.22f, 0.99f)).gameObject.AddComponent<TimerDisplay>();

            GameObject topRight = new GameObject("TopRight", typeof(RectTransform));
            topRight.transform.SetParent(panel.transform, false);
            CreateText(topRight.transform, "OrientationLabel", "+Y", 32, TextAlignmentOptions.TopRight, new Vector2(0.78f, 0.93f), new Vector2(0.98f, 0.99f), UiStyle.TextSecondary);

            GameObject topCentre = new GameObject("TopCentre", typeof(RectTransform));
            topCentre.transform.SetParent(panel.transform, false);
            CreateButton(topCentre.transform, "PauseButton", "II", new Vector2(0.47f, 0.93f), new Vector2(0.53f, 0.99f));
            panel.AddComponent<HudPanelController>();
        }

        private static void BuildPausePanel(GameObject panel)
        {
            CreateBackground(panel.transform);
            CreateText(panel.transform, "Title", "PAUSED", 70, TextAlignmentOptions.Center, new Vector2(0.3f, 0.72f), new Vector2(0.7f, 0.84f));
            CreateButton(panel.transform, "ResumeButton", "RESUME", new Vector2(0.36f, 0.5f), new Vector2(0.64f, 0.58f));
            CreateButton(panel.transform, "RetryButton", "RESTART LEVEL", new Vector2(0.36f, 0.39f), new Vector2(0.64f, 0.47f));
            CreateButton(panel.transform, "MenuButton", "MAIN MENU", new Vector2(0.36f, 0.28f), new Vector2(0.64f, 0.36f));
            panel.AddComponent<PausePanelController>();
        }

        private static void BuildResultPanel(GameObject panel)
        {
            CreateBackground(panel.transform);
            GameObject header = new GameObject("ResultHeader", typeof(RectTransform));
            header.transform.SetParent(panel.transform, false);
            CreateText(header.transform, "CompleteLabel", "YOU MADE IT OUT", 64, TextAlignmentOptions.Center, new Vector2(0.2f, 0.8f), new Vector2(0.8f, 0.9f));
            CreateText(header.transform, "PersonalBestBadge", "NEW BEST!", 28, TextAlignmentOptions.Center, new Vector2(0.4f, 0.74f), new Vector2(0.6f, 0.8f), UiStyle.AccentGold);

            GameObject timeBlock = new GameObject("TimeBlock", typeof(RectTransform));
            timeBlock.transform.SetParent(panel.transform, false);
            CreateText(timeBlock.transform, "TimeLabel", "TIME", 26, TextAlignmentOptions.Center, new Vector2(0.45f, 0.62f), new Vector2(0.55f, 0.67f), UiStyle.TextSecondary);
            CreateText(timeBlock.transform, "TimeValue", "00:00", 54, TextAlignmentOptions.Center, new Vector2(0.35f, 0.54f), new Vector2(0.65f, 0.62f)).gameObject.AddComponent<TimerDisplay>();

            GameObject starBlock = new GameObject("StarBlock", typeof(RectTransform));
            starBlock.transform.SetParent(panel.transform, false);
            CreateStars(starBlock.transform, "StarDisplay", new Vector2(0.33f, 0.43f), new Vector2(0.67f, 0.5f));

            GameObject statsBlock = new GameObject("StatsBlock", typeof(RectTransform));
            statsBlock.transform.SetParent(panel.transform, false);
            CreateText(statsBlock.transform, "SwitchCountLabel", "0 orientation switches", 24, TextAlignmentOptions.Center, new Vector2(0.3f, 0.36f), new Vector2(0.7f, 0.42f), UiStyle.TextSecondary);

            GameObject row = new GameObject("ButtonRow", typeof(RectTransform));
            row.transform.SetParent(panel.transform, false);
            CreateButton(row.transform, "RetryButton", "RETRY", new Vector2(0.23f, 0.19f), new Vector2(0.41f, 0.27f));
            CreateButton(row.transform, "NextButton", "NEXT LEVEL", new Vector2(0.43f, 0.19f), new Vector2(0.61f, 0.27f));
            CreateButton(row.transform, "MenuButton", "MENU", new Vector2(0.63f, 0.19f), new Vector2(0.81f, 0.27f));

            panel.AddComponent<LevelResultPanelController>();
        }

        private static void BuildHighScores(GameObject panel)
        {
            CreateBackground(panel.transform);
            GameObject header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(panel.transform, false);
            CreateText(header.transform, "Title", "HIGH SCORES", 60, TextAlignmentOptions.Center, new Vector2(0.3f, 0.88f), new Vector2(0.7f, 0.97f));
            CreateButton(header.transform, "BackButton", "<- BACK", new Vector2(0.03f, 0.9f), new Vector2(0.2f, 0.97f));

            GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollView.transform.SetParent(panel.transform, false);
            RectTransform svRt = scrollView.GetComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.1f, 0.1f);
            svRt.anchorMax = new Vector2(0.9f, 0.85f);
            svRt.offsetMin = Vector2.zero;
            svRt.offsetMax = Vector2.zero;
            scrollView.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(10f, 0f);
            contentRt.offsetMax = new Vector2(-10f, 0f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollView.GetComponent<ScrollRect>();
            sr.viewport = vpRt;
            sr.content = contentRt;
            sr.horizontal = false;
            sr.vertical = true;

            CreateLeaderboardRowTemplate(content.transform);
            panel.AddComponent<HighScoresPanelController>();
        }

        private static void CreateLeaderboardRowTemplate(Transform parent)
        {
            GameObject row = new GameObject("LeaderboardRowTemplate", typeof(RectTransform), typeof(LayoutElement), typeof(LeaderboardRow));
            row.transform.SetParent(parent, false);
            row.SetActive(false);
            LayoutElement le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 70f;

            CreateText(row.transform, "RankLabel", "01", 26, TextAlignmentOptions.Left, new Vector2(0.02f, 0.15f), new Vector2(0.12f, 0.85f));
            CreateText(row.transform, "LevelNameLabel", "The Cube", 26, TextAlignmentOptions.Left, new Vector2(0.14f, 0.15f), new Vector2(0.45f, 0.85f));
            CreateText(row.transform, "TimeLabel", "00:00", 26, TextAlignmentOptions.Center, new Vector2(0.47f, 0.15f), new Vector2(0.62f, 0.85f));
            CreateStars(row.transform, "StarDisplay", new Vector2(0.64f, 0.15f), new Vector2(0.96f, 0.85f));
        }
    }
}
