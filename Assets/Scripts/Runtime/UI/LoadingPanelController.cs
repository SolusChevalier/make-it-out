using MakeItOut.Runtime.Flow;
using MakeItOut.Runtime.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MakeItOut.Runtime.UI
{
    public class LoadingPanelController : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _statusLabel;

        private void Awake()
        {
            if (_fill == null)
                _fill = transform.Find("ProgressBarBackground/ProgressBarFill")?.GetComponent<Image>();
            if (_statusLabel == null)
                _statusLabel = transform.Find("StatusLabel")?.GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (_fill != null)
                _fill.fillAmount = 0f;
            if (_statusLabel != null)
                _statusLabel.text = "Loading...";
        }

        private void Update()
        {
            if (!gameObject.activeSelf)
                return;

            float progress = 0f;
            string status = "Loading...";
            GameManager manager = GameManager.Instance;
            if (manager != null && manager.CurrentState == GameState.LoadingLevel && LevelLoader.Instance != null)
            {
                progress = LevelLoader.Instance.LoadProgress;
                string name = manager.ActiveLevel?.Definition?.DisplayName ?? string.Empty;
                status = progress < 1f
                    ? $"Generating {name}... {Mathf.RoundToInt(progress * 100f)}%"
                    : "Building world...";
            }
            else if (manager != null && manager.CurrentState == GameState.Boot)
            {
                status = "Starting...";
            }

            if (_fill != null)
            {
                _fill.fillAmount = Mathf.Lerp(_fill.fillAmount, progress, Time.deltaTime * 8f);
                _fill.color = Color.Lerp(new Color(0.4f, 0.4f, 0.4f), UiStyle.AccentGold, _fill.fillAmount);
            }
            if (_statusLabel != null)
                _statusLabel.text = status;
        }
    }
}
