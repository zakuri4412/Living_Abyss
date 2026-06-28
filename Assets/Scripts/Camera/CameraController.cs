using UnityEngine;
using LivingAbyss.Core;

namespace LivingAbyss.Camera
{
    // Metroidvania-style camera for Living Abyss.
    // Features: smooth follow · X lookahead · Y deadzone · room bounds · PixelPerfectCamera compatible.
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraController : MonoBehaviour
    {
        // ── Internal resolution constants (GDD §9.2) ──────────────────────────
        private const float PPU       = GameConstants.PIXELS_PER_UNIT;
        private const float CAM_HALF_W = 320f / 2f / PPU; // 10.0 units
        private const float CAM_HALF_H = 180f / 2f / PPU; // 5.625 units

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Follow Speed")]
        [SerializeField] private float _speedX = 8f;   // units/s horizontal catch-up
        [SerializeField] private float _speedY = 6f;   // units/s vertical catch-up

        [Header("Lookahead")]
        [SerializeField] private float _lookaheadDist  = 1.5f;  // max X offset ahead of player
        [SerializeField] private float _lookaheadSpeed = 3.5f;  // how fast lookahead shifts

        [Header("Vertical Deadzone")]
        [SerializeField] private float _deadzoneY = 0.5f; // units — camera ignores movement inside this band

        [Header("Room Bounds")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Bounds _roomBounds;     // set by CameraBounds trigger

        // ── State ─────────────────────────────────────────────────────────────
        private Vector3   _pos;             // current camera world position (Z fixed at -10)
        private float     _lookaheadX;      // current X lookahead offset
        private float     _velocityX;       // smoothdamp ref — horizontal
        private Rigidbody2D _playerRb;
        private float     _targetY;         // tracked separately for deadzone logic
        private bool      _deadzoneActive;

        public static CameraController Instance { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (_target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    _target   = player.transform;
                    _playerRb = player.GetComponent<Rigidbody2D>();
                }
            }

            // Snap to player immediately on first frame
            if (_target != null)
            {
                _pos     = _target.position;
                _pos.z   = -10f;
                _targetY = _target.position.y;
                transform.position = _pos;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            float dt = Time.deltaTime;

            UpdateLookahead(dt);
            UpdateTargetY(dt);

            float goalX = _target.position.x + _lookaheadX;
            float goalY = _targetY;

            // Smooth horizontal — SmoothDamp for responsive-but-not-snappy feel
            _pos.x = Mathf.SmoothDamp(_pos.x, goalX, ref _velocityX, 1f / _speedX);

            // Smooth vertical — simple lerp
            _pos.y = Mathf.Lerp(_pos.y, goalY, _speedY * dt);

            if (_useBounds)
            {
                float preClampX = _pos.x;
                _pos = ClampToBounds(_pos);
                // Kill accumulated SmoothDamp velocity when hitting a horizontal wall;
                // stale velocity causes the camera to bounce away from the boundary.
                if (!Mathf.Approximately(_pos.x, preClampX))
                    _velocityX = 0f;
            }

            _pos.z = -10f;
            transform.position = _pos;
        }

        // ── Lookahead ─────────────────────────────────────────────────────────
        // Camera looks ahead in the direction the player is moving.
        // Smoothly retreats when player slows/stops.

        private void UpdateLookahead(float dt)
        {
            float playerVelX = _playerRb != null ? _playerRb.linearVelocity.x : 0f;
            float targetLookahead = 0f;

            if (Mathf.Abs(playerVelX) > 0.5f)
                targetLookahead = Mathf.Sign(playerVelX) * _lookaheadDist;

            _lookaheadX = Mathf.Lerp(_lookaheadX, targetLookahead, _lookaheadSpeed * dt);
        }

        // ── Vertical deadzone ─────────────────────────────────────────────────
        // Camera only follows Y when the player moves outside the deadzone band.
        // Prevents camera bobbing every jump.

        private void UpdateTargetY(float dt)
        {
            float playerY = _target.position.y;
            float diff    = playerY - _targetY;

            if (Mathf.Abs(diff) > _deadzoneY)
                _targetY = playerY - Mathf.Sign(diff) * _deadzoneY;

            // Clamp _targetY to the achievable camera range BEFORE lerping.
            // Without this, the target overshoots the bound during a jump and then
            // snaps back hard when the player lands — causing the jitter.
            if (_useBounds)
            {
                float minCamY = _roomBounds.min.y + CAM_HALF_H;
                float maxCamY = _roomBounds.max.y - CAM_HALF_H;
                _targetY = minCamY <= maxCamY
                    ? Mathf.Clamp(_targetY, minCamY, maxCamY)
                    : (minCamY + maxCamY) * 0.5f; // room smaller than camera — lock Y
            }
        }

        // ── Room bounds ───────────────────────────────────────────────────────

        private Vector3 ClampToBounds(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x,
                _roomBounds.min.x + CAM_HALF_W,
                _roomBounds.max.x - CAM_HALF_W);

            pos.y = Mathf.Clamp(pos.y,
                _roomBounds.min.y + CAM_HALF_H,
                _roomBounds.max.y - CAM_HALF_H);

            return pos;
        }

        // Called by CameraBounds trigger when player enters a new room
        public void SetBounds(Bounds bounds)
        {
            _roomBounds = bounds;
            _useBounds  = true;
        }

        public void ClearBounds() => _useBounds = false;

        // ── Public API ────────────────────────────────────────────────────────

        public void SetTarget(Transform t)
        {
            _target   = t;
            _playerRb = t != null ? t.GetComponent<Rigidbody2D>() : null;
        }

        // Instantly snap camera to player — use on scene load / respawn
        public void SnapToTarget()
        {
            if (_target == null) return;
            _pos        = _target.position;
            _pos.z      = -10f;
            _targetY    = _target.position.y;
            _lookaheadX = 0f;
            _velocityX  = 0f;
            if (_useBounds)
            {
                _pos = ClampToBounds(_pos);
                float minCamY = _roomBounds.min.y + CAM_HALF_H;
                float maxCamY = _roomBounds.max.y - CAM_HALF_H;
                _targetY = minCamY <= maxCamY
                    ? Mathf.Clamp(_targetY, minCamY, maxCamY)
                    : (minCamY + maxCamY) * 0.5f;
            }
            transform.position = _pos;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Deadzone band
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, _targetY, 0f),
                new Vector3(CAM_HALF_W * 2f, _deadzoneY * 2f, 0f));

            // Room bounds
            if (_useBounds)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
                Gizmos.DrawWireCube(_roomBounds.center, _roomBounds.size);
            }

            // Lookahead target
            if (_target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position,
                    new Vector3(_target.position.x + _lookaheadX, _targetY, 0f));
            }
        }
#endif
    }
}
