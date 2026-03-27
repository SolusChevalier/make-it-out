using MakeItOut.Runtime.Flow;
using MakeItOut.Runtime.Player;
using MakeItOut.Runtime.Progression;
using TMPro;
using UnityEngine;

namespace MakeItOut.Runtime.UI
{
    public class LevelIntroPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelNumber;
        [SerializeField] private TMP_Text _levelName;
        [SerializeField] private TMP_Text _gridSizeLabel;
        [SerializeField] private TMP_Text[] _starTargetRows;
        [SerializeField] private TMP_Text _bestTimeLabel;
        [SerializeField] private StarRatingDisplay _bestStars;
        [SerializeField] private GameObject _firstTimeBadge;
        [SerializeField] private UiButton _startButton;
        [SerializeField] private UiButton _backButton;

        private void Awake()
        {
            if (GetComponent<PanelSlideIn>() == null)
                gameObject.AddComponent<PanelSlideIn>();

            if (_levelNumber == null)
                _levelNumber = transform.Find("LevelNumber")?.GetComponent<TMP_Text>();
            if (_levelName == null)
                _levelName = transform.Find("LevelName")?.GetComponent<TMP_Text>();
            if (_gridSizeLabel == null)
                _gridSizeLabel = transform.Find("GridSizeLabel")?.GetComponent<TMP_Text>();
            if ((_starTargetRows == null || _starTargetRows.Length == 0) && transform.Find("StarTargets") != null)
            {
                Transform targets = transform.Find("StarTargets");
                _starTargetRows = new TMP_Text[targets.childCount];
                for (int i = 0; i < targets.childCount; i++)
                    _starTargetRows[i] = targets.GetChild(i).GetComponent<TMP_Text>();
            }
            if (_bestTimeLabel == null)
                _bestTimeLabel = transform.Find("PersonalBestBlock/BestTimeLabel")?.GetComponent<TMP_Text>();
            if (_bestStars == null)
                _bestStars = transform.Find("PersonalBestBlock/BestStarsDisplay")?.GetComponent<StarRatingDisplay>();
            if (_firstTimeBadge == null)
                _firstTimeBadge = transform.Find("FirstTimeBadge")?.gameObject;
            if (_startButton == null)
                _startButton = transform.Find("StartButton")?.GetComponent<UiButton>();
            if (_backButton == null)
                _backButton = transform.Find("BackButton")?.GetComponent<UiButton>();

            _startButton?.AddListener(() => GameManager.Instance.ConfirmLevelStart());
            _backButton?.AddListener(() => GameManager.Instance.GoToLevelSelect());
        }

        private void OnEnable()
        {
            ActiveLevelContext ctx = GameManager.Instance?.ActiveLevel;
            if (ctx?.Definition == null)
                return;

            LevelDefinition def = ctx.Definition;
            _levelNumber.text = $"LEVEL {ctx.LevelIndex + 1:D2}";
            _levelName.text = def.DisplayName;
            _gridSizeLabel.text = $"{def.GridSize} x {def.GridSize} x {def.GridSize}";

            string[] starLabels = { "5 STARS", "4 STARS", "3 STARS", "2 STARS" };
            for (int i = 0; i < 4 && i < _starTargetRows.Length; i++)
            {
                float t = def.StarThresholds[i];
                int m = (int)t / 60;
                int s = (int)t % 60;
                _starTargetRows[i].text = $"{starLabels[i]}   under {m}:{s:D2}";
            }

            float bestTime = ServiceLocator.Persistence.GetBestTime(def.LevelId);
            int bestStars = ServiceLocator.Persistence.GetBestStars(def.LevelId);
            bool hasRecord = bestStars > 0;

            _firstTimeBadge?.SetActive(!hasRecord);
            if (_bestTimeLabel != null)
                _bestTimeLabel.gameObject.SetActive(hasRecord);
            if (_bestStars != null)
                _bestStars.gameObject.SetActive(hasRecord);

            if (hasRecord)
            {
                int bm = (int)bestTime / 60;
                int bs = (int)bestTime % 60;
                _bestTimeLabel.text = $"BEST   {bm}:{bs:D2}";
                _bestStars.SetStars(bestStars);
            }
        }
    }
}
