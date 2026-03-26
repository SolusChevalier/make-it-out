using MakeItOut.Runtime.Player;
using UnityEngine;

namespace MakeItOut.Runtime.UI
{
    public class PausePanelController : MonoBehaviour
    {
        [SerializeField] private UiButton _resumeButton;
        [SerializeField] private UiButton _retryButton;
        [SerializeField] private UiButton _menuButton;

        private void Awake()
        {
            if (_resumeButton == null)
                _resumeButton = transform.Find("ResumeButton")?.GetComponent<UiButton>();
            if (_retryButton == null)
                _retryButton = transform.Find("RetryButton")?.GetComponent<UiButton>();
            if (_menuButton == null)
                _menuButton = transform.Find("MenuButton")?.GetComponent<UiButton>();

            _resumeButton?.AddListener(() => GameManager.Instance.ResumeRun());
            _retryButton?.AddListener(() => GameManager.Instance.RetryLevel());
            _menuButton?.AddListener(() => GameManager.Instance.GoToMainMenu());
        }
    }
}
