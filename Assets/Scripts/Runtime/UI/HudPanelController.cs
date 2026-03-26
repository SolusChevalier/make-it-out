using MakeItOut.Runtime.Player;
using TMPro;
using UnityEngine;

namespace MakeItOut.Runtime.UI
{
    public class HudPanelController : MonoBehaviour
    {
        [SerializeField] private TimerDisplay _timer;
        [SerializeField] private TMP_Text _orientationLabel;
        [SerializeField] private UiButton _pauseButton;

        private void Awake()
        {
            if (_timer == null)
                _timer = transform.Find("TopLeft/RunTimer")?.GetComponent<TimerDisplay>();
            if (_orientationLabel == null)
                _orientationLabel = transform.Find("TopRight/OrientationLabel")?.GetComponent<TMP_Text>();
            if (_pauseButton == null)
                _pauseButton = transform.Find("TopCentre/PauseButton")?.GetComponent<UiButton>();

            _pauseButton?.AddListener(() => GameManager.Instance.PauseRun());
        }

        private void Update()
        {
            if (!gameObject.activeSelf || GameManager.Instance == null)
                return;

            float elapsed = (float)GameManager.Instance.RunElapsed.TotalSeconds;
            _timer?.SetTime(elapsed);

            bool overLimit = GameManager.Instance.ActiveLevel?.Definition != null &&
                elapsed > GameManager.Instance.ActiveLevel.Definition.StarThresholds[3];
            _timer?.SetColor(overLimit ? UiStyle.AccentRed : UiStyle.TextPrimary);

            if (CameraOrientation.Instance != null && _orientationLabel != null)
                _orientationLabel.text = FormatAxis(CameraOrientation.Instance.Up);
        }

        private static string FormatAxis(Vector3 v)
        {
            if (v == Vector3.up) return "+Y";
            if (v == -Vector3.up) return "-Y";
            if (v == Vector3.right) return "+X";
            if (v == -Vector3.right) return "-X";
            if (v == Vector3.forward) return "+Z";
            if (v == -Vector3.forward) return "-Z";
            return "??";
        }
    }
}
