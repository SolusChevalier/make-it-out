using System;
using MakeItOut.Runtime.MazeGeneration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MakeItOut.Runtime.Player
{
    public sealed class HudManager : MonoBehaviour
    {
        public static HudManager Instance;

        [Header("Panels")]
        public GameObject LoadingPanel;
        public GameObject HudPanel;
        public GameObject WinPanel;
        public GameObject FailPanel;

        [Header("Loading")]
        public Slider LoadingBar;
        public TMP_Text LoadingLabel;

        [Header("In-Run HUD")]
        public TMP_Text TimerDisplay;
        public TMP_Text OrientationDisplay;

        [Header("Win Screen")]
        public TMP_Text WinTimeDisplay;
        public Button WinRestartButton;

        [Header("Fail Screen")]
        public TMP_Text FailReasonDisplay;
        public Button FailRestartButton;

        private bool _restartListenersBound;

        private void Awake()
        {
            Instance = this;
            EnsureCanvasHierarchy();
            ShowLoadingScreen();
        }

        private void Start()
        {
            BindRuntimeEvents();
        }

        private void OnDestroy()
        {
            GameManager gameManager = GameManager.Instance;

            if (_restartListenersBound)
            {
                if (gameManager != null && WinRestartButton != null)
                {
                    WinRestartButton.onClick.RemoveListener(gameManager.RestartRun);
                }

                if (gameManager != null && FailRestartButton != null)
                {
                    FailRestartButton.onClick.RemoveListener(gameManager.RestartRun);
                }
            }

            if (gameManager != null)
            {
                gameManager.OnStateChanged -= OnStateChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            if (!_restartListenersBound)
            {
                BindRuntimeEvents();
            }

            if (GameManager.Instance.CurrentState == GameState.Playing)
            {
                UpdateInRunHud();

                if (Input.GetKeyDown(KeyCode.R))
                {
                    GameManager.Instance.RestartRun();
                }
            }

            if (GameManager.Instance.CurrentState == GameState.Loading)
            {
                UpdateLoadingBar();
            }
        }

        public void ShowEndScreen(GameState state)
        {
            if (HudPanel != null)
            {
                HudPanel.SetActive(false);
            }

            if (state == GameState.Win)
            {
                if (WinTimeDisplay != null)
                {
                    WinTimeDisplay.text = FormatTime(GameManager.Instance.RunElapsed);
                }

                if (WinPanel != null)
                {
                    WinPanel.SetActive(true);
                }
            }
            else
            {
                if (FailReasonDisplay != null)
                {
                    FailReasonDisplay.text = "You fell out of the maze.";
                }

                if (FailPanel != null)
                {
                    FailPanel.SetActive(true);
                }
            }
        }

        private void BindRuntimeEvents()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            if (!_restartListenersBound)
            {
                if (WinRestartButton != null)
                {
                    WinRestartButton.onClick.AddListener(GameManager.Instance.RestartRun);
                }

                if (FailRestartButton != null)
                {
                    FailRestartButton.onClick.AddListener(GameManager.Instance.RestartRun);
                }

                _restartListenersBound = true;
            }

            GameManager.Instance.OnStateChanged -= OnStateChanged;
            GameManager.Instance.OnStateChanged += OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    if (LoadingPanel != null)
                    {
                        LoadingPanel.SetActive(false);
                    }

                    if (HudPanel != null)
                    {
                        HudPanel.SetActive(true);
                    }

                    if (WinPanel != null)
                    {
                        WinPanel.SetActive(false);
                    }

                    if (FailPanel != null)
                    {
                        FailPanel.SetActive(false);
                    }
                    break;

                case GameState.Loading:
                    ShowLoadingScreen();
                    break;
            }
        }

        private void ShowLoadingScreen()
        {
            if (LoadingPanel != null)
            {
                LoadingPanel.SetActive(true);
            }

            if (HudPanel != null)
            {
                HudPanel.SetActive(false);
            }

            if (WinPanel != null)
            {
                WinPanel.SetActive(false);
            }

            if (FailPanel != null)
            {
                FailPanel.SetActive(false);
            }
        }

        private void UpdateLoadingBar()
        {
            if (MazeGenerator.Instance == null || LoadingBar == null || LoadingLabel == null)
            {
                return;
            }

            float p = MazeGenerator.Instance.Progress;
            LoadingBar.value = p;
            LoadingLabel.text = p < 1f
                ? $"Generating maze... {Mathf.RoundToInt(p * 100f)}%"
                : "Building world...";
        }

        private void UpdateInRunHud()
        {
            if (TimerDisplay != null)
            {
                TimerDisplay.text = FormatTime(GameManager.Instance.RunElapsed);
            }

            if (OrientationDisplay != null && CameraOrientation.Instance != null)
            {
                Vector3 up = CameraOrientation.Instance.Up;
                OrientationDisplay.text = $"UP  {FormatAxis(up)}";
            }
        }

        private static string FormatTime(TimeSpan t)
        {
            return $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
        }

        private static string FormatAxis(Vector3 v)
        {
            if (v == Vector3.up) return "+Y";
            if (v == -Vector3.up) return "-Y";
            if (v == Vector3.right) return "+X";
            if (v == -Vector3.right) return "-X";
            if (v == Vector3.forward) return "+Z";
            if (v == -Vector3.forward) return "-Z";
            return v.ToString("F0");
        }

        private void EnsureCanvasHierarchy()
        {
            if (HasAllReferences())
            {
                return;
            }

            Canvas canvas = GetOrCreateCanvas();
            EnsureEventSystem();

            LoadingPanel = CreatePanel(canvas.transform, "LoadingPanel", new Color(0f, 0f, 0f, 0.95f));
            HudPanel = CreatePanel(canvas.transform, "HudPanel", new Color(0f, 0f, 0f, 0f));
            WinPanel = CreatePanel(canvas.transform, "WinPanel", new Color(0f, 0f, 0f, 0.65f));
            FailPanel = CreatePanel(canvas.transform, "FailPanel", new Color(0f, 0f, 0f, 0.65f));

            GameObject loadingBackground = CreatePanel(LoadingPanel.transform, "Background", new Color(0f, 0f, 0f, 1f));
            RectTransform loadingBackgroundRect = loadingBackground.GetComponent<RectTransform>();
            loadingBackgroundRect.anchorMin = Vector2.zero;
            loadingBackgroundRect.anchorMax = Vector2.one;
            loadingBackgroundRect.offsetMin = Vector2.zero;
            loadingBackgroundRect.offsetMax = Vector2.zero;

            LoadingBar = CreateSlider(LoadingPanel.transform, "LoadingBar", new Vector2(0.2f, 0.46f), new Vector2(0.8f, 0.52f));
            LoadingLabel = CreateText(
                LoadingPanel.transform,
                "LoadingLabel",
                "Generating maze... 0%",
                32f,
                TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.54f),
                new Vector2(0.8f, 0.64f));

            TimerDisplay = CreateText(
                HudPanel.transform,
                "TimerDisplay",
                "00:00",
                32f,
                TextAlignmentOptions.Top,
                new Vector2(0.4f, 0.93f),
                new Vector2(0.6f, 0.99f));

            OrientationDisplay = CreateText(
                HudPanel.transform,
                "OrientationDisplay",
                "UP  +Y",
                28f,
                TextAlignmentOptions.TopRight,
                new Vector2(0.72f, 0.93f),
                new Vector2(0.98f, 0.99f));

            CreatePanel(WinPanel.transform, "Background", new Color(0f, 0f, 0f, 0.7f));
            CreateText(
                WinPanel.transform,
                "Title",
                "YOU MADE IT OUT",
                48f,
                TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.62f),
                new Vector2(0.8f, 0.78f));

            WinTimeDisplay = CreateText(
                WinPanel.transform,
                "WinTimeDisplay",
                "00:00",
                36f,
                TextAlignmentOptions.Center,
                new Vector2(0.3f, 0.5f),
                new Vector2(0.7f, 0.6f));

            WinRestartButton = CreateButton(
                WinPanel.transform,
                "WinRestartButton",
                "PLAY AGAIN",
                new Vector2(0.37f, 0.34f),
                new Vector2(0.63f, 0.43f));

            CreatePanel(FailPanel.transform, "Background", new Color(0f, 0f, 0f, 0.7f));
            CreateText(
                FailPanel.transform,
                "Title",
                "YOU FELL",
                48f,
                TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.62f),
                new Vector2(0.8f, 0.78f));

            FailReasonDisplay = CreateText(
                FailPanel.transform,
                "FailReasonDisplay",
                "You fell out of the maze.",
                30f,
                TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.48f),
                new Vector2(0.8f, 0.58f));

            FailRestartButton = CreateButton(
                FailPanel.transform,
                "FailRestartButton",
                "TRY AGAIN",
                new Vector2(0.37f, 0.34f),
                new Vector2(0.63f, 0.43f));
        }

        private bool HasAllReferences()
        {
            return LoadingPanel != null &&
                   HudPanel != null &&
                   WinPanel != null &&
                   FailPanel != null &&
                   LoadingBar != null &&
                   LoadingLabel != null &&
                   TimerDisplay != null &&
                   OrientationDisplay != null &&
                   WinTimeDisplay != null &&
                   WinRestartButton != null &&
                   FailReasonDisplay != null &&
                   FailRestartButton != null;
        }

        private static Canvas GetOrCreateCanvas()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = color;

            return panel;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject sliderGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderGo.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = anchorMin;
            sliderRect.anchorMax = anchorMax;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            Image background = sliderGo.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.2f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(4f, 4f);
            fillAreaRect.offsetMax = new Vector2(-4f, -4f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = Color.white;

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;

            return slider;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(parent, false);

            RectTransform rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;

            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.15f);

            Button button = buttonGo.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.15f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.35f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.1f);
            button.colors = colors;

            CreateText(
                buttonGo.transform,
                "Label",
                label,
                30f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one);

            return button;
        }
    }
}
