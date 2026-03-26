using MakeItOut.Runtime.Player;
using MakeItOut.Runtime.Progression;
using TMPro;
using UnityEngine;

namespace MakeItOut.Runtime.UI
{
    public class HighScoresPanelController : MonoBehaviour
    {
        [SerializeField] private UiButton _backButton;
        [SerializeField] private Transform _scrollContent;
        [SerializeField] private LeaderboardRow _rowPrefab;

        private void Awake()
        {
            if (_backButton == null)
                _backButton = transform.Find("Header/BackButton")?.GetComponent<UiButton>();
            if (_scrollContent == null)
                _scrollContent = transform.Find("ScrollView/Viewport/Content");
            if (_rowPrefab == null)
                _rowPrefab = transform.Find("ScrollView/Viewport/Content/LeaderboardRowTemplate")?.GetComponent<LeaderboardRow>();

            _backButton?.AddListener(() => GameManager.Instance.GoToMainMenu());
        }

        private void OnEnable()
        {
            if (_scrollContent == null || _rowPrefab == null || ServiceLocator.Persistence == null || ServiceLocator.Instance == null)
                return;

            foreach (Transform child in _scrollContent)
                Destroy(child.gameObject);

            var entries = ServiceLocator.Persistence.GetLeaderboard(ServiceLocator.Instance.RegistryAsset.Registry);
            for (int i = 0; i < entries.Count; i++)
            {
                LeaderboardRow row = Instantiate(_rowPrefab, _scrollContent);
                row.gameObject.SetActive(true);
                row.Bind(i + 1, entries[i]);
            }

            if (entries.Count == 0)
            {
                LeaderboardRow row = Instantiate(_rowPrefab, _scrollContent);
                row.gameObject.SetActive(true);
                row.BindEmpty();
            }
        }
    }

    public class LeaderboardRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rankLabel;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _timeLabel;
        [SerializeField] private StarRatingDisplay _stars;

        private void Awake()
        {
            if (_rankLabel == null)
                _rankLabel = transform.Find("RankLabel")?.GetComponent<TMP_Text>();
            if (_nameLabel == null)
                _nameLabel = transform.Find("LevelNameLabel")?.GetComponent<TMP_Text>();
            if (_timeLabel == null)
                _timeLabel = transform.Find("TimeLabel")?.GetComponent<TMP_Text>();
            if (_stars == null)
                _stars = transform.Find("StarDisplay")?.GetComponent<StarRatingDisplay>();
        }

        public void Bind(int rank, LeaderboardEntry entry)
        {
            if (_rankLabel == null || _nameLabel == null || _timeLabel == null || _stars == null)
                return;

            _rankLabel.text = $"{rank:D2}";
            _nameLabel.text = entry.DisplayName;
            int m = (int)entry.BestTime / 60;
            int s = (int)entry.BestTime % 60;
            _timeLabel.text = $"{m}:{s:D2}";
            _stars.SetStars(entry.BestStars);
        }

        public void BindEmpty()
        {
            if (_rankLabel == null || _nameLabel == null || _timeLabel == null || _stars == null)
                return;

            _rankLabel.text = "--";
            _nameLabel.text = "No scores yet";
            _timeLabel.text = string.Empty;
            _stars.SetStars(0);
        }
    }
}
