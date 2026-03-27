using MakeItOut.Runtime.Flow;
using MakeItOut.Runtime.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MakeItOut.Runtime.Player
{
    /// <summary>
    /// Shows and hides UI panels in response to GameManager state changes.
    ///
    /// All panel references are assigned in the inspector. To regenerate the scenes
    /// with panels pre-wired, use: Tools → Make It Out → Create Flow Scenes.
    /// </summary>
    public sealed class HudManager : MonoBehaviour
    {
        public static HudManager Instance { get; private set; }

        [Header("Panels — assign in the inspector")]
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

            // Auto-find level select controller from the already-assigned panel.
            if (_levelSelectController == null && LevelSelectPanel != null)
                _levelSelectController = LevelSelectPanel.GetComponent<LevelSelectPanelController>();

            // Hide all panels immediately so nothing flickers on the first frame.
            SetAllPanelsInactive();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnStateChanged;
                GameManager.Instance.OnStateChanged += OnStateChanged;
                OnStateChanged(GameManager.Instance.CurrentState);
            }
            else
            {
                // Fallback for playing a scene directly in the editor without Bootstrap.
                bool isMainMenu = SceneManager.GetActiveScene().name == "MainMenu";
                OnStateChanged(isMainMenu ? GameState.MainMenu : GameState.LoadingLevel);
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
            SetAllPanelsInactive();

            switch (state)
            {
                case GameState.Boot:
                case GameState.LoadingLevel:
                    if (LoadingPanel != null) LoadingPanel.SetActive(true);
                    break;

                case GameState.MainMenu:
                    if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
                    break;

                case GameState.LevelSelect:
                    if (LevelSelectPanel != null) LevelSelectPanel.SetActive(true);
                    _levelSelectController?.RefreshButtons();
                    break;

                case GameState.LevelIntro:
                    if (LevelIntroPanel != null) LevelIntroPanel.SetActive(true);
                    break;

                case GameState.Playing:
                    if (HudPanel != null) HudPanel.SetActive(true);
                    break;

                case GameState.Paused:
                    if (PausePanel != null) PausePanel.SetActive(true);
                    break;

                case GameState.LevelResult:
                    if (LevelResultPanel != null) LevelResultPanel.SetActive(true);
                    break;

                case GameState.HighScores:
                    if (HighScoresPanel != null) HighScoresPanel.SetActive(true);
                    break;
            }
        }

        private void SetAllPanelsInactive()
        {
            // Use explicit null checks — Unity serialized "None" references throw
            // UnassignedReferenceException with C#'s ?. operator because the managed
            // wrapper is non-null even when the native side has been destroyed.
            if (LoadingPanel     != null) LoadingPanel.SetActive(false);
            if (HudPanel         != null) HudPanel.SetActive(false);
            if (MainMenuPanel    != null) MainMenuPanel.SetActive(false);
            if (LevelSelectPanel != null) LevelSelectPanel.SetActive(false);
            if (LevelIntroPanel  != null) LevelIntroPanel.SetActive(false);
            if (LevelResultPanel != null) LevelResultPanel.SetActive(false);
            if (HighScoresPanel  != null) HighScoresPanel.SetActive(false);
            if (PausePanel       != null) PausePanel.SetActive(false);
        }
    }
}
