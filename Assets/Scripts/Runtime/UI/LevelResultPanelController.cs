using MakeItOut.Runtime.Flow;
using MakeItOut.Runtime.Player;
using MakeItOut.Runtime.Progression;
using TMPro;
using UnityEngine;

namespace MakeItOut.Runtime.UI
{
    public class LevelResultPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _completeLabel;
        [SerializeField] private GameObject _personalBestBadge;
        [SerializeField] private TimerDisplay _timeDisplay;
        [SerializeField] private StarRatingDisplay _starDisplay;
        [SerializeField] private TMP_Text _switchCountLabel;
        [SerializeField] private UiButton _retryButton;
        [SerializeField] private UiButton _nextButton;
        [SerializeField] private UiButton _menuButton;

        private void Awake()
        {
            if (GetComponent<PanelSlideIn>() == null)
                gameObject.AddComponent<PanelSlideIn>();

            if (_completeLabel == null)
                _completeLabel = transform.Find("ResultHeader/CompleteLabel")?.GetComponent<TMP_Text>();
            if (_personalBestBadge == null)
                _personalBestBadge = transform.Find("ResultHeader/PersonalBestBadge")?.gameObject;
            if (_timeDisplay == null)
                _timeDisplay = transform.Find("TimeBlock/TimeValue")?.GetComponent<TimerDisplay>();
            if (_starDisplay == null)
                _starDisplay = transform.Find("StarBlock/StarDisplay")?.GetComponent<StarRatingDisplay>();
            if (_switchCountLabel == null)
                _switchCountLabel = transform.Find("StatsBlock/SwitchCountLabel")?.GetComponent<TMP_Text>();
            if (_retryButton == null)
                _retryButton = transform.Find("ButtonRow/RetryButton")?.GetComponent<UiButton>();
            if (_nextButton == null)
                _nextButton = transform.Find("ButtonRow/NextButton")?.GetComponent<UiButton>();
            if (_menuButton == null)
                _menuButton = transform.Find("ButtonRow/MenuButton")?.GetComponent<UiButton>();

            _retryButton?.AddListener(() => GameManager.Instance.RetryLevel());
            _nextButton?.AddListener(() => GameManager.Instance.GoToNextLevel());
            _menuButton?.AddListener(() => GameManager.Instance.GoToMainMenu());
        }

        private void OnEnable()
        {
            ActiveLevelContext ctx = GameManager.Instance?.ActiveLevel;
            if (ctx == null)
                return;

            if (_completeLabel != null)
                _completeLabel.text = ctx.IsComplete ? "YOU MADE IT OUT" : "RUN ENDED";
            _personalBestBadge?.SetActive(ctx.IsPersonalBest && ctx.IsComplete);
            _timeDisplay?.SetTime(ctx.ElapsedSeconds);

            _starDisplay?.SetStars(0);
            if (ctx.IsComplete && _starDisplay != null)
                StartCoroutine(_starDisplay.AnimateReveal(ctx.StarsEarned));

            if (_switchCountLabel != null)
                _switchCountLabel.text = $"{ctx.OrientationSwitchCount} orientation switches";

            int nextIndex = ctx.LevelIndex + 1;
            bool nextExists = nextIndex < ServiceLocator.Progression.GetTotalCampaignLevels();
            bool nextUnlocked = nextExists && ServiceLocator.Progression.IsUnlocked(nextIndex);
            _nextButton?.SetInteractable(nextUnlocked);
        }
    }
}
