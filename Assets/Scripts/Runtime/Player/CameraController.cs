using MakeItOut.Runtime.GridSystem;
using UnityEngine;

namespace MakeItOut.Runtime.Player
{
    public sealed class CameraController : MonoBehaviour
    {
        private static readonly KeyCode[] s_switchKeys =
        {
            KeyCode.RightArrow,
            KeyCode.LeftArrow,
            KeyCode.UpArrow,
            KeyCode.DownArrow,
        };

        public static CameraController Instance;

        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Camera _cam;
        [SerializeField] private TransparencyManager _transparencyManager;

        [Header("Settings")]
        [Min(0.01f)]
        [SerializeField] private float _transitionDuration = 0.4f;
        [SerializeField] private float _zoomMin = 8f;
        [SerializeField] private float _zoomMax = 28f;
        [SerializeField] private float _zoomSpeed = 3f;
        [Min(0f)]
        [SerializeField] private float _cameraDistance = 20f;

        private Quaternion _currentOrientation;
        private Quaternion _targetOrientation;
        private Quaternion _transitionStartOrientation;
        private float _transitionProgress;
        private bool _isTransitioning;
        private float _currentZoom;
        private PlayerController _playerController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            Quaternion startingOrientation = _cam != null
                ? _cam.transform.rotation
                : Quaternion.identity;

            _currentOrientation = SnapToCardinal(startingOrientation);
            _targetOrientation = _currentOrientation;
            _currentZoom = (_zoomMin + _zoomMax) * 0.5f;
            _playerController = _playerTransform != null ? _playerTransform.GetComponent<PlayerController>() : null;

            if (_cam != null)
            {
                _cam.orthographic = true;
                _cam.orthographicSize = _currentZoom;
                ApplyCameraTransform(_currentOrientation);
            }

            PublishOrientation();
            PositionCamera(_currentOrientation);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            HandleZoom();

            if (_isTransitioning)
            {
                UpdateTransition();
                return;
            }

            HandleInput();
            PositionCamera(_currentOrientation);
        }

        private void LateUpdate()
        {
            if (_transparencyManager == null || _playerTransform == null || WorldGrid.Instance == null)
            {
                return;
            }

            Vector3Int playerGrid = WorldGrid.Instance.WorldToGrid(_playerTransform.position);
            _transparencyManager.UpdateTransparency(playerGrid, _currentOrientation);
        }

        private void HandleZoom()
        {
            if (_cam == null)
            {
                return;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _currentZoom = Mathf.Clamp(_currentZoom - scroll * _zoomSpeed, _zoomMin, _zoomMax);
            _cam.orthographicSize = _currentZoom;
        }

        private void HandleInput()
        {
            for (int i = 0; i < s_switchKeys.Length; i++)
            {
                KeyCode key = s_switchKeys[i];
                if (!Input.GetKeyDown(key))
                {
                    continue;
                }

                BeginTransition(ComputeTargetOrientation(key));
                return;
            }
        }

        private void BeginTransition(Quaternion target)
        {
            _targetOrientation = target;
            _transitionStartOrientation = _currentOrientation;
            _transitionProgress = 0f;
            _isTransitioning = true;

            if (_playerController != null)
            {
                _playerController.OnCameraSwitchStart();
            }
        }

        private void UpdateTransition()
        {
            if (_transitionDuration <= 0f)
            {
                _transitionProgress = 1f;
            }
            else
            {
                _transitionProgress += Time.deltaTime / _transitionDuration;
            }

            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                _isTransitioning = false;
                _currentOrientation = SnapToCardinal(_targetOrientation);

                PublishOrientation();
                if (_playerController != null)
                {
                    _playerController.OnCameraSwitchComplete();
                }

                PositionCamera(_currentOrientation);
                return;
            }

            Quaternion animated = Quaternion.Slerp(
                _transitionStartOrientation,
                _targetOrientation,
                SmoothStep(_transitionProgress));

            ApplyCameraTransform(animated);
            PositionCamera(animated);
        }

        private Quaternion ComputeTargetOrientation(KeyCode key)
        {
            Vector3 forward = _currentOrientation * Vector3.forward;
            Vector3 right = _currentOrientation * Vector3.right;

            Quaternion delta = key switch
            {
                KeyCode.RightArrow => Quaternion.AngleAxis(90f, forward),
                KeyCode.LeftArrow => Quaternion.AngleAxis(-90f, forward),
                KeyCode.UpArrow => Quaternion.AngleAxis(-90f, right),
                KeyCode.DownArrow => Quaternion.AngleAxis(90f, right),
                _ => Quaternion.identity,
            };

            return SnapToCardinal(delta * _currentOrientation);
        }

        private void PublishOrientation()
        {
            if (CameraOrientation.Instance != null)
            {
                CameraOrientation.Instance.Up = _currentOrientation * Vector3.up;
                CameraOrientation.Instance.Right = _currentOrientation * Vector3.right;
                CameraOrientation.Instance.Forward = _currentOrientation * Vector3.forward;
            }

            ApplyCameraTransform(_currentOrientation);
        }

        private void ApplyCameraTransform(Quaternion orientation)
        {
            if (_cam == null)
            {
                return;
            }

            _cam.transform.rotation = orientation;
        }

        private void PositionCamera(Quaternion orientation)
        {
            if (_cam == null || _playerTransform == null)
            {
                return;
            }

            Vector3 viewForward = orientation * Vector3.forward;
            Vector3 playerPos = _playerTransform.position;
            _cam.transform.position = playerPos - viewForward * _cameraDistance;
        }

        private static Quaternion SnapToCardinal(Quaternion q)
        {
            Vector3 forward = q * Vector3.forward;
            Vector3 up = q * Vector3.up;

            forward = SnapAxis(forward);
            up = SnapAxis(up);

            if (Vector3.Cross(forward, up).sqrMagnitude < 0.01f)
            {
                up = SnapAxis(q * Vector3.right);
            }

            return Quaternion.LookRotation(forward, up);
        }

        private static Vector3 SnapAxis(Vector3 v)
        {
            float ax = Mathf.Abs(v.x);
            float ay = Mathf.Abs(v.y);
            float az = Mathf.Abs(v.z);

            if (ax >= ay && ax >= az)
            {
                return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            }

            if (ay >= ax && ay >= az)
            {
                return new Vector3(0f, Mathf.Sign(v.y), 0f);
            }

            return new Vector3(0f, 0f, Mathf.Sign(v.z));
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private void OnDrawGizmos()
        {
            if (_playerTransform == null)
            {
                return;
            }

            Vector3 pos = _playerTransform.position;
            Quaternion drawOrientation = Application.isPlaying ? _currentOrientation : transform.rotation;

            Gizmos.color = Color.green;
            Gizmos.DrawRay(pos, drawOrientation * Vector3.up * 3f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(pos, drawOrientation * Vector3.right * 3f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(pos, drawOrientation * Vector3.forward * 3f);
        }
    }
}
