using System.Collections.Generic;
using MakeItOut.Runtime.Progression;
using MakeItOut.Runtime.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MakeItOut.Runtime.UI
{
    public class LevelSelectPanelController : MonoBehaviour
    {
        [SerializeField] private UiButton _backButton;
        [SerializeField] private Transform _scrollContent;
        [SerializeField] private LevelButtonItem _levelButtonPrefab;
        [SerializeField] private ScrollRect _scrollRect;

        private readonly List<LevelButtonItem> _buttons = new List<LevelButtonItem>();

        private void Awake()
        {
            if (_backButton == null)
                _backButton = transform.Find("Header/BackButton")?.GetComponent<UiButton>();
            if (_scrollContent == null)
                _scrollContent = transform.Find("ScrollView/Viewport/Content");
            if (_levelButtonPrefab == null)
                _levelButtonPrefab = transform.Find("ScrollView/Viewport/Content/LevelButtonTemplate")?.GetComponent<LevelButtonItem>();
            if (_scrollRect == null)
                _scrollRect = transform.Find("ScrollView")?.GetComponent<ScrollRect>();

            _backButton?.AddListener(() => GameManager.Instance.GoToMainMenu());
        }

        private void OnEnable()
        {
            RefreshButtons();
        }

        public void RefreshButtons()
        {
            foreach (LevelButtonItem b in _buttons)
            {
                if (b != null)
                    Destroy(b.gameObject);
            }
            _buttons.Clear();

            if (_scrollContent == null || _levelButtonPrefab == null || ServiceLocator.Progression == null)
                return;

            int total = ServiceLocator.Progression.GetTotalCampaignLevels();
            for (int i = 0; i < total; i++)
            {
                LevelDefinition def = ServiceLocator.Progression.GetLevel(i);
                bool unlocked = ServiceLocator.Progression.IsUnlocked(i);
                int stars = ServiceLocator.Persistence.GetBestStars(def.LevelId);
                float bestTime = ServiceLocator.Persistence.GetBestTime(def.LevelId);

                LevelButtonItem btn = Instantiate(_levelButtonPrefab, _scrollContent);
                btn.gameObject.SetActive(true);
                btn.Bind(i, def, unlocked, stars, bestTime);
                _buttons.Add(btn);
            }

            ScrollToCurrentLevel();
        }

        private void ScrollToCurrentLevel()
        {
            if (_scrollRect == null || _buttons.Count == 0)
                return;

            int current = ServiceLocator.Progression.GetHighestUnlockedIndex();
            if (current < 0 || current >= _buttons.Count)
                return;

            float normalised = 1f - (float)current / Mathf.Max(1, _buttons.Count - 1);
            _scrollRect.verticalNormalizedPosition = normalised;
        }
    }

}
