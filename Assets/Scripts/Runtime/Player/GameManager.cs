using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MakeItOut.Runtime.Player
{
    public enum GameState
    {
        Loading,
        Playing,
        Win,
        Fail,
        Restarting,
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public GameState CurrentState { get; private set; } = GameState.Loading;

        public event Action<GameState> OnStateChanged;

        [Header("Settings")]
        public float WinHoldDuration = 2f;
        public float FailHoldDuration = 1f;

        private float _stateTimer;
        private Stopwatch _runTimer;

        public TimeSpan RunElapsed => _runTimer?.Elapsed ?? TimeSpan.Zero;

        private void Awake()
        {
            Instance = this;
            _runTimer = new Stopwatch();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void NotifyGenerationComplete()
        {
            TransitionTo(GameState.Playing);
            _runTimer.Restart();
        }

        public void TriggerWin()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            TransitionTo(GameState.Win);
        }

        public void TriggerFail()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            TransitionTo(GameState.Fail);
        }

        public void RestartRun()
        {
            if (CurrentState == GameState.Restarting)
            {
                return;
            }

            TransitionTo(GameState.Restarting);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void TransitionTo(GameState next)
        {
            CurrentState = next;
            _stateTimer = 0f;
            OnStateChanged?.Invoke(next);

            if (next == GameState.Win || next == GameState.Fail)
            {
                _runTimer.Stop();
            }
        }

        private void Update()
        {
            if (CurrentState == GameState.Playing ||
                CurrentState == GameState.Loading ||
                CurrentState == GameState.Restarting)
            {
                return;
            }

            _stateTimer += Time.deltaTime;

            float holdDuration = CurrentState == GameState.Win
                ? WinHoldDuration
                : FailHoldDuration;

            if (_stateTimer >= holdDuration && HudManager.Instance != null)
            {
                HudManager.Instance.ShowEndScreen(CurrentState);
            }
        }
    }
}
