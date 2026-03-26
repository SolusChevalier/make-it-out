using System.Collections;
using MakeItOut.Runtime.GridSystem;
using UnityEngine;

namespace MakeItOut.Runtime.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        public float MoveSpeed = 7f;
        public float JumpForce = 9f;
        public float Gravity = 22f;
        public float StepHeight = 2f;

        [Header("Ladder")]
        public float ClimbSpeed = 4f;

        private CharacterController _cc;
        private Vector3 _velocity;
        private Vector3Int _gridPos;
        private bool _isGrounded;
        private bool _isOnLadder;
        private bool _isSwitching;
        private bool _isStepping;

        /// <summary>Exposed for tests and diagnostics.</summary>
        public Vector3Int GridPosition => _gridPos;

        /// <summary>Exposed for tests and diagnostics.</summary>
        public bool IsSwitching => _isSwitching;

        /// <summary>Exposed for tests and diagnostics.</summary>
        public Vector3 Velocity => _velocity;

        /// <summary>Exposed for diagnostics HUD.</summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>Exposed for diagnostics HUD.</summary>
        public bool IsOnLadder => _isOnLadder;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _cc = GetComponent<CharacterController>();
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
            if (_isSwitching)
            {
                return;
            }

            _gridPos = WorldGrid.Instance.WorldToGrid(transform.position);

            if (CameraOrientation.Instance == null)
            {
                return;
            }

            Vector3 camUp = CameraOrientation.Instance.Up;
            Vector3 camRight = CameraOrientation.Instance.Right;

            Vector3 basePoint = transform.position - camUp * (_cc.height * 0.5f - _cc.radius);
            bool physicsGround = Physics.SphereCast(
                basePoint,
                _cc.radius * 0.9f,
                -camUp,
                out _,
                GridConfig.BlockSize * 0.15f,
                LayerMask.GetMask("Default"));

            Vector3Int below = _gridPos + Vector3Int.RoundToInt(-camUp);
            bool gridGround = WorldGrid.Instance.IsSolid(below);

            _isGrounded = physicsGround && gridGround;

            if (_isGrounded)
            {
                float downComponent = Vector3.Dot(_velocity, -camUp);
                if (downComponent < 0f)
                {
                    _velocity -= -camUp * downComponent;
                }
            }

            _isOnLadder = WorldGrid.Instance.GetFeature(_gridPos) == FeatureType.Ladder;

            float hInput = Input.GetAxis("Horizontal");
            if (_isStepping)
            {
                hInput = 0f;
            }

            Vector3 hMove = camRight * hInput * MoveSpeed;

            if (!_isStepping)
            {
                if (_isOnLadder)
                {
                    float vInput = Input.GetAxis("Vertical");
                    _velocity = camRight * hInput * MoveSpeed
                                + CameraOrientation.Instance.Up * vInput * ClimbSpeed;
                }
                else
                {
                    _velocity -= camUp * Gravity * Time.deltaTime;

                    if (Input.GetButtonDown("Jump") && _isGrounded)
                    {
                        _velocity += camUp * JumpForce;
                        _isGrounded = false;
                    }
                }
            }

            if (_isGrounded && Mathf.Abs(hInput) > 0.1f && !_isStepping)
            {
                Vector3 moveDir = camRight * Mathf.Sign(hInput);
                Vector3Int ahead = _gridPos + Vector3Int.RoundToInt(moveDir);
                Vector3Int aheadAbove = ahead + Vector3Int.RoundToInt(camUp);

                if (WorldGrid.Instance.IsSolid(ahead) && !WorldGrid.Instance.IsSolid(aheadAbove))
                {
                    _isStepping = true;
                    StartCoroutine(SmoothStepUp(camUp));
                }
            }

            Vector3 move = hMove * Time.deltaTime;

            if (!_isOnLadder)
            {
                move += _velocity * Time.deltaTime;
            }
            else
            {
                move = _velocity * Time.deltaTime;
            }

            if (_isStepping)
            {
                move = Vector3.zero;
            }

            _cc.Move(move);

            _gridPos = WorldGrid.Instance.WorldToGrid(transform.position);

            if (WorldGrid.Instance.GetFeature(_gridPos) == FeatureType.Exit)
            {
                GameManager.Instance.TriggerWin();
            }

            // Out of bounds check - triggers fail if player leaves the grid.
            if (!WorldGrid.Instance.InBounds(_gridPos))
            {
                GameManager.Instance.TriggerFail();
            }
        }

        public void OnCameraSwitchStart()
        {
            _isSwitching = true;
            _velocity = Vector3.zero;
            _isStepping = false;
            StopAllCoroutines();
        }

        public void OnCameraSwitchComplete()
        {
            _isSwitching = false;
        }

        public void Teleport(Vector3 worldPosition)
        {
            _cc.enabled = false;
            transform.position = worldPosition;
            _cc.enabled = true;
            _velocity = Vector3.zero;
        }

        private IEnumerator SmoothStepUp(Vector3 stepCamUp)
        {
            const float duration = 0.1f;
            float elapsed = 0f;
            Vector3 accumulated = Vector3.zero;

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    Vector3 desiredTotal = stepCamUp * StepHeight * t;
                    Vector3 delta = desiredTotal - accumulated;
                    accumulated = desiredTotal;

                    if (delta.sqrMagnitude > 0f)
                    {
                        _cc.Move(delta);
                    }

                    yield return null;
                }
            }
            finally
            {
                _isStepping = false;
            }
        }
    }
}
