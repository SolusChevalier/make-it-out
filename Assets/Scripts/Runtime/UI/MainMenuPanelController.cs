using MakeItOut.Runtime.Player;
using TMPro;
using UnityEngine;

namespace MakeItOut.Runtime.UI
{
    public class MainMenuPanelController : MonoBehaviour
    {
        [SerializeField] private UiButton _playButton;
        [SerializeField] private UiButton _highScoresButton;
        [SerializeField] private TMP_Text _versionLabel;

        private void Awake()
        {
            if (_playButton == null)
                _playButton = transform.Find("ButtonStack/PlayButton")?.GetComponent<UiButton>();
            if (_highScoresButton == null)
                _highScoresButton = transform.Find("ButtonStack/HighScoresButton")?.GetComponent<UiButton>();
            if (_versionLabel == null)
                _versionLabel = transform.Find("VersionLabel")?.GetComponent<TMP_Text>();

            _playButton?.AddListener(() => GameManager.Instance.GoToLevelSelect());
            _highScoresButton?.AddListener(() => GameManager.Instance.GoToHighScores());
            if (_versionLabel != null)
                _versionLabel.text = $"v{Application.version}";
        }
    }
}
