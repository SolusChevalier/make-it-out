using MakeItOut.Runtime.MazeGeneration;
using MakeItOut.Runtime.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MakeItOut.Runtime.Dev
{
    public sealed class DevHudOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private MazeGenerator _mazeGenerator;
        [SerializeField] private DevSceneBootstrap _devSceneBootstrap;

        [Header("Display")]
        [SerializeField] private bool _showHud = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F3;
        [SerializeField] private Vector2 _position = new Vector2(10f, 10f);
        [Min(160f)]
        [SerializeField] private float _width = 340f;

        private GUIStyle _style;

        private void Awake()
        {
            if (_playerController == null)
            {
                _playerController = FindObjectOfType<PlayerController>();
            }

            if (_mazeGenerator == null)
            {
                _mazeGenerator = FindObjectOfType<MazeGenerator>();
            }

            if (_devSceneBootstrap == null)
            {
                _devSceneBootstrap = FindObjectOfType<DevSceneBootstrap>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _showHud = !_showHud;
            }
        }

        private void OnGUI()
        {
            if (!_showHud)
            {
                return;
            }

            EnsureStyle();

            Vector3? camUp = null;
            Vector3? camRight = null;
            Vector3? camForward = null;
            if (CameraOrientation.Instance != null)
            {
                camUp = CameraOrientation.Instance.Up;
                camRight = CameraOrientation.Instance.Right;
                camForward = CameraOrientation.Instance.Forward;
            }

            string seedLabel = _devSceneBootstrap != null
                ? _devSceneBootstrap.CurrentSeedLabel
                : "n/a";

            string hud = DevHudFormatter.BuildStatusText(
                SceneManager.GetActiveScene().name,
                seedLabel,
                _mazeGenerator != null ? _mazeGenerator.Progress : 0f,
                _playerController != null ? _playerController.GridPosition : Vector3Int.zero,
                _playerController != null ? _playerController.Velocity : Vector3.zero,
                _playerController != null && _playerController.IsSwitching,
                camUp,
                camRight,
                camForward);

            float height = _style.CalcHeight(new GUIContent(hud), _width);
            GUI.Box(new Rect(_position.x, _position.y, _width, height + 12f), GUIContent.none);
            GUI.Label(new Rect(_position.x + 6f, _position.y + 6f, _width - 12f, height), hud, _style);
        }

        private void EnsureStyle()
        {
            if (_style != null)
            {
                return;
            }

            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = false,
                normal = { textColor = Color.white },
            };
        }
    }
}
