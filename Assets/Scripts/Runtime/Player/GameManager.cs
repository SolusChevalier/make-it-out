using System.Collections;
using System.Linq;
using System;
using MakeItOut.Runtime.Flow;
using MakeItOut.Runtime.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MakeItOut.Runtime.Player
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Boot;
        public ActiveLevelContext ActiveLevel { get; private set; }

        public event Action<GameState> OnStateChanged;

        [Header("Scenes")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        [SerializeField] private string _gameSceneName = "Game";

        private readonly System.Diagnostics.Stopwatch _runTimer = new System.Diagnostics.Stopwatch();
        public TimeSpan RunElapsed => _runTimer.Elapsed;

        private bool _isLoadingScene;
        private bool _isBooting;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ActiveLevel = new ActiveLevelContext();
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (LevelLoader.Instance != null)
            {
                LevelLoader.Instance.OnLevelReady += OnLevelReady;
            }

            TransitionTo(GameState.Boot);
        }

        private void OnDestroy()
        {
            if (LevelLoader.Instance != null)
                LevelLoader.Instance.OnLevelReady -= OnLevelReady;
        }

        public void NotifyGenerationComplete()
        {
            // Backward-compatible hook for legacy callers.
            OnLevelReady();
        }

        private void OnBootComplete()
        {
            TransitionTo(GameState.MainMenu);
        }

        private IEnumerator BootCoroutine()
        {
            if (_isBooting)
                yield break;

            _isBooting = true;
            yield return null;
            _isBooting = false;
            OnBootComplete();
        }

        public void GoToMainMenu()
        {
            if (_isLoadingScene)
                return;

            StartCoroutine(LoadMainMenuSceneCoroutine());
        }

        public void GoToLevelSelect()
        {
            if (!Guard(GameState.MainMenu, GameState.LevelResult, GameState.HighScores))
                return;

            TransitionTo(GameState.LevelSelect);
        }

        public void GoToHighScores()
        {
            if (!Guard(GameState.MainMenu))
                return;

            TransitionTo(GameState.HighScores);
        }

        public void SelectLevel(int index)
        {
            if (!Guard(GameState.LevelSelect))
                return;

            if (!ServiceLocator.Progression.IsUnlocked(index))
            {
                Debug.LogWarning($"GameManager: Level {index} is not unlocked.");
                return;
            }

            LevelDefinition level = ServiceLocator.Progression.GetLevel(index);
            int seed = SeedResolver.Resolve(level);

            ActiveLevel.Definition = level;
            ActiveLevel.LevelIndex = index;
            ActiveLevel.ResolvedSeed = seed;

            TransitionTo(GameState.LevelIntro);
        }

        public void ConfirmLevelStart()
        {
            if (!Guard(GameState.LevelIntro))
                return;

            BeginLevelLoad();
        }

        public void RetryLevel()
        {
            if (!Guard(GameState.LevelResult, GameState.Paused, GameState.Playing))
                return;

            int index = ActiveLevel.LevelIndex;
            int seed = ActiveLevel.ResolvedSeed;
            LevelDefinition level = ServiceLocator.Progression.GetLevel(index);

            ActiveLevel.Definition = level;
            ActiveLevel.LevelIndex = index;
            ActiveLevel.ResolvedSeed = seed;

            BeginLevelLoad();
        }

        public void GoToNextLevel()
        {
            if (!Guard(GameState.LevelResult))
                return;

            int next = ActiveLevel.LevelIndex + 1;
            if (!ServiceLocator.Progression.IsUnlocked(next))
            {
                Debug.LogWarning("GameManager: Next level is not yet unlocked.");
                GoToLevelSelect();
                return;
            }

            LevelDefinition level = ServiceLocator.Progression.GetLevel(next);
            int seed = SeedResolver.Resolve(level);

            ActiveLevel.Definition = level;
            ActiveLevel.LevelIndex = next;
            ActiveLevel.ResolvedSeed = seed;

            TransitionTo(GameState.LevelIntro);
        }

        private void BeginLevelLoad()
        {
            if (_isLoadingScene)
                return;

            TransitionTo(GameState.LoadingLevel);
            StartCoroutine(BeginLevelLoadCoroutine());
        }

        private IEnumerator BeginLevelLoadCoroutine()
        {
            _isLoadingScene = true;

            if (SceneManager.GetActiveScene().name != _gameSceneName)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Single);
                while (!load.isDone)
                    yield return null;
            }

            // Let scene singletons wake up
            yield return null;

            if (LevelLoader.Instance == null)
            {
                Debug.LogError("GameManager: LevelLoader is missing in current scene.");
                _isLoadingScene = false;
                yield break;
            }

            LevelLoader.Instance.LoadLevel(
                ActiveLevel.Definition,
                ActiveLevel.LevelIndex,
                ActiveLevel.ResolvedSeed);

            _isLoadingScene = false;
        }

        private void OnLevelReady()
        {
            TransitionTo(GameState.Playing);
            _runTimer.Restart();
        }

        public void PauseRun()
        {
            if (!Guard(GameState.Playing))
                return;

            _runTimer.Stop();
            TransitionTo(GameState.Paused);
        }

        public void ResumeRun()
        {
            if (!Guard(GameState.Paused))
                return;

            _runTimer.Start();
            TransitionTo(GameState.Playing);
        }

        public void TriggerWin()
        {
            if (CurrentState != GameState.Playing)
                return;

            _runTimer.Stop();
            float elapsed = (float)_runTimer.Elapsed.TotalSeconds;

            ActiveLevel.ElapsedSeconds = elapsed;
            ActiveLevel.IsComplete = true;
            ActiveLevel.StarsEarned = ServiceLocator.Scoring.EvaluateStars(ActiveLevel.Definition, elapsed);
            ActiveLevel.IsPersonalBest = ServiceLocator.Scoring.IsPersonalBest(ActiveLevel.Definition.LevelId, elapsed);

            ServiceLocator.Persistence.SaveResult(
                ActiveLevel.Definition.LevelId,
                elapsed,
                ActiveLevel.StarsEarned);

            TransitionTo(GameState.LevelResult);
        }

        public void TriggerFail()
        {
            if (CurrentState != GameState.Playing)
                return;

            _runTimer.Stop();
            ActiveLevel.ElapsedSeconds = (float)_runTimer.Elapsed.TotalSeconds;
            ActiveLevel.IsComplete = false;
            ActiveLevel.StarsEarned = 0;
            ActiveLevel.IsPersonalBest = false;

            TransitionTo(GameState.LevelResult);
        }

        public void RestartRun()
        {
            RetryLevel();
        }

        public void NotifyOrientationSwitch()
        {
            if (CurrentState == GameState.Playing)
                ActiveLevel.OrientationSwitchCount++;
        }

        private void TransitionTo(GameState next)
        {
            CurrentState = next;
            OnStateChanged?.Invoke(next);

            if (next == GameState.Boot)
                StartCoroutine(BootCoroutine());

            Debug.Log($"GameManager: -> {next}");
        }

        private bool Guard(params GameState[] permitted)
        {
            if (permitted.Any(s => CurrentState == s))
                return true;

            Debug.LogWarning(
                $"GameManager: Transition rejected. Current state {CurrentState} is not in permitted set [{string.Join(", ", permitted)}].");
            return false;
        }

        private IEnumerator LoadMainMenuSceneCoroutine()
        {
            _isLoadingScene = true;
            AsyncOperation load = SceneManager.LoadSceneAsync(_mainMenuSceneName, LoadSceneMode.Single);
            while (!load.isDone)
                yield return null;
            _isLoadingScene = false;

            TransitionTo(GameState.MainMenu);
        }

        private void Update()
        {
            if (CurrentState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
                PauseRun();
            else if (CurrentState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape))
                ResumeRun();
        }
    }
}
