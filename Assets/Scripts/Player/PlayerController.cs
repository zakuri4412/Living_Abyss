using UnityEngine;
using LivingAbyss.Core;
using LivingAbyss.Systems;

namespace LivingAbyss.Player
{
    // Dr. Yên Minh — main character controller.
    // Implements all physics from GDD §7.2 and edge cases from §7.4.
    // No HP — state is driven by SymbiosisSystem.
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Collision")]
        [SerializeField] private LayerMask _solidLayer;
        [SerializeField] private LayerMask _organicLayer;    // organic = grippable ceiling/wall
        [SerializeField] private LayerMask _platformLayer;   // one-way platforms (drop-through)
        [SerializeField] private float _skinWidth = 0.02f;   // tiny gap to avoid sinking into geometry

        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;

        // ── Components ────────────────────────────────────────────────────────
        private Rigidbody2D         _rb;
        private PlayerInputHandler  _input;
        private SymbiosisSystem     _symb;
        private CapsuleCollider2D   _col;

        // ── Velocity (owned by us; gravity scale = 0) ─────────────────────────
        private Vector2 _vel;

        // ── Ground / Ceiling state ────────────────────────────────────────────
        private bool _grounded;
        private bool _wasGrounded;
        private bool _touchingCeiling;
        private bool _touchingOrganicCeiling;
        private bool _touchingWallLeft;
        private bool _touchingWallRight;

        // ── Coyote time ───────────────────────────────────────────────────────
        private float _coyoteGroundTimer;
        private float _coyoteCeilingTimer;
        private bool  _usedCoyote;

        // ── Jump state ────────────────────────────────────────────────────────
        private bool _isRising;      // currently in the upward arc of a jump

        // ── Ceiling grip (Ký sinh bào) ────────────────────────────────────────
        private bool  _gripping;
        private float _gripTimer;
        public  bool  HasSporeParasite { get; set; } = false; // unlocked during T1

        // ── Public read-only state (for animator, HUD, skills) ───────────────
        public bool  IsGrounded  => _grounded;
        public bool  IsGripping  => _gripping;
        public bool  IsRising    => _isRising;
        public Vector2 Velocity  => _vel;

        // ── Wall jump ─────────────────────────────────────────────────────────
        private int _lastWallSide;       // -1 = left, +1 = right, 0 = none
        private int _sameWallJumpCount;

        // ── Drop-through ──────────────────────────────────────────────────────
        private PlatformEffector2D[] _platformEffectors; // cached for collision toggling

        // ── Cached symbiosis modifiers ────────────────────────────────────────
        private float _runMult  = 1f;
        private float _jumpMult = 1f;
        private float _gripMult = 1f;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb  = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _input = GetComponent<PlayerInputHandler>();

            _rb.gravityScale           = 0f;   // manual gravity
            _rb.freezeRotation         = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        }

        private void OnEnable()  => EventBus.Subscribe<SymbZoneEnteredEvent>(OnZoneChanged);
        private void OnDisable() => EventBus.Unsubscribe<SymbZoneEnteredEvent>(OnZoneChanged);

        private void Start()
        {
            _symb = SymbiosisSystem.Instance;
            if (_symb != null) PullSymbModifiers();
        }

        // ─────────────────────────────────────────────────────────────────────

        private void Update()
        {
            CheckSurroundings();
            HandleCoyoteTimers();
            HandleDropThrough();
            HandleCeilingGrip();
            HandleJump();
            HandleHorizontalMove();
            ApplyGravity();

            _rb.linearVelocity = _vel;
            EventBus.Publish(new PlayerMoveEvent { Velocity = _vel });

            _wasGrounded = _grounded;
        }

        // ── Collision detection ───────────────────────────────────────────────

        private void CheckSurroundings()
        {
            var b  = _col.bounds;
            var sz = new Vector2(b.size.x - _skinWidth * 2f, _skinWidth * 2f);

            // Ground
            var gHit = Physics2D.BoxCast(b.center, sz, 0f, Vector2.down,
                        b.extents.y + _skinWidth, _solidLayer | _platformLayer);
            _grounded = gHit.collider != null;
            if (_grounded && _vel.y < 0f) { _vel.y = 0f; OnLanded(); }

            // Ceiling
            var solidCeil = Physics2D.BoxCast(b.center, sz, 0f, Vector2.up,
                            b.extents.y + _skinWidth, _solidLayer);
            _touchingCeiling = solidCeil.collider != null;
            if (_touchingCeiling && _vel.y > 0f) _vel.y = 0f;

            var organicCeil = Physics2D.BoxCast(b.center, sz, 0f, Vector2.up,
                              b.extents.y + _skinWidth, _organicLayer);
            _touchingOrganicCeiling = organicCeil.collider != null;

            // Walls (for wall jump)
            var wSz = new Vector2(_skinWidth * 2f, b.size.y - _skinWidth * 2f);
            _touchingWallLeft  = Physics2D.BoxCast(b.center, wSz, 0f, Vector2.left,
                                 b.extents.x + _skinWidth, _solidLayer).collider != null;
            _touchingWallRight = Physics2D.BoxCast(b.center, wSz, 0f, Vector2.right,
                                 b.extents.x + _skinWidth, _solidLayer).collider != null;
        }

        // ── Coyote timers ─────────────────────────────────────────────────────

        private void HandleCoyoteTimers()
        {
            float coyoteGround = GameConstants.COYOTE_TIME_GROUND;
            if (_symb != null) coyoteGround -= _symb.CoyoteReduction;

            if (_grounded)
            {
                _coyoteGroundTimer = coyoteGround;
                _usedCoyote = false;
            }
            else
            {
                _coyoteGroundTimer -= Time.deltaTime;
            }

            if (_gripping)
                _coyoteCeilingTimer = GameConstants.COYOTE_TIME_CEILING;
            else
                _coyoteCeilingTimer -= Time.deltaTime;
        }

        private bool CanJump() =>
            _grounded ||
            (_coyoteGroundTimer > 0f && !_usedCoyote) ||
            _gripping ||
            _touchingWallLeft ||
            _touchingWallRight;

        // ── Drop through one-way platforms ────────────────────────────────────

        private void HandleDropThrough()
        {
            if (!_input.DropPlatformTriggered) return;
            _input.ConsumeDropPlatform();
            // Temporarily ignore platform layer for DROP_HOLD_TIME duration
            // Implemented via Physics2D.IgnoreLayerCollision
            int playerLayer   = gameObject.layer;
            int platformLayer = LayerMaskToLayer(_platformLayer);
            if (platformLayer < 0) return;
            Physics2D.IgnoreLayerCollision(playerLayer, platformLayer, true);
            Invoke(nameof(RestorePlatformCollision), GameConstants.DROP_HOLD_TIME + 0.05f);
        }

        private void RestorePlatformCollision()
        {
            int playerLayer   = gameObject.layer;
            int platformLayer = LayerMaskToLayer(_platformLayer);
            if (platformLayer >= 0)
                Physics2D.IgnoreLayerCollision(playerLayer, platformLayer, false);
        }

        // ── Ceiling grip (Ký sinh bào) ────────────────────────────────────────

        private void HandleCeilingGrip()
        {
            if (!HasSporeParasite) { _gripping = false; return; }

            bool wantsGrip = _input.CeilingGripHeld && _touchingOrganicCeiling;

            if (wantsGrip && !_gripping)
            {
                _gripping  = true;
                _gripTimer = float.IsPositiveInfinity(_gripMult)
                    ? float.MaxValue
                    : GameConstants.CEILING_GRIP_BASE_DURATION * _gripMult;
                _vel.y = 0f;
                _vel.x *= 0.5f; // slight slowdown on attach
            }
            else if (_gripping)
            {
                if (!_input.CeilingGripHeld || !_touchingOrganicCeiling)
                {
                    _gripping = false;
                    return;
                }
                if (!float.IsPositiveInfinity(_gripMult))
                {
                    _gripTimer -= Time.deltaTime;
                    if (_gripTimer <= 0f) _gripping = false;
                }
            }
        }

        // ── Jump ──────────────────────────────────────────────────────────────

        private void HandleJump()
        {
            if (!_input.JumpBuffered) return;
            if (!CanJump()) return;

            _input.ConsumeJumpBuffer();
            _usedCoyote = true;

            if (_gripping)
            {
                // Jump away from ceiling
                _gripping = false;
                _vel.y    = -(GameConstants.JUMP_VELOCITY * _jumpMult * 0.4f);
                return;
            }

            bool isWallJump = !_grounded && !_usedCoyote &&
                              (_touchingWallLeft || _touchingWallRight);

            if (isWallJump)
            {
                DoWallJump();
                return;
            }

            // Normal jump (ground or coyote)
            _vel.y  = GameConstants.JUMP_VELOCITY * _jumpMult;
            _isRising = true;
        }

        private void DoWallJump()
        {
            int side = _touchingWallLeft ? -1 : 1;
            float yVel = GameConstants.JUMP_VELOCITY * _jumpMult;

            // GDD §7.4: third+ wall jump on same side loses 25% Y velocity
            if (side == _lastWallSide)
            {
                _sameWallJumpCount++;
                if (_sameWallJumpCount > GameConstants.WALL_JUMP_MAX_SAME_SIDE)
                    yVel *= 1f - GameConstants.WALL_JUMP_VEL_PENALTY *
                                 (_sameWallJumpCount - GameConstants.WALL_JUMP_MAX_SAME_SIDE);
            }
            else
            {
                _lastWallSide      = side;
                _sameWallJumpCount = 1;
            }

            _vel.y    = yVel;
            _vel.x    = side * GameConstants.WALL_JUMP_HORIZONTAL_FORCE;  // push away from wall
            _isRising = true;
        }

        // ── Horizontal movement ───────────────────────────────────────────────

        private void HandleHorizontalMove()
        {
            float target  = _input.MoveX * GameConstants.MAX_RUN_SPEED * _runMult;
            float current = _vel.x;

            if (_gripping) target *= 0.6f; // slower slide while gripping ceiling

            float accel;
            bool  movingAgainstCurrent = target * current < 0f && Mathf.Abs(current) > 0.01f;

            if (movingAgainstCurrent)
            {
                // Turning around: fast deceleration
                accel = GameConstants.DECEL_GROUND;
            }
            else if (Mathf.Abs(target) > 0.01f)
            {
                accel = GameConstants.ACCELERATION;
            }
            else if (_grounded)
            {
                // No input on ground: micro-slide (GDD §7.1)
                accel = GameConstants.DECEL_NO_INPUT;
            }
            else
            {
                // No input in air: reduced friction
                accel = GameConstants.DECEL_GROUND * 0.5f;
            }

            _vel.x = Mathf.MoveTowards(current, target, accel * Time.deltaTime);

            // Corner correction on ceiling: nudge horizontally to help slide over lips (GDD §7.2)
            if (_touchingCeiling && _vel.y > 0f)
                TryCornerCorrectCeiling();
        }

        // ── Gravity ───────────────────────────────────────────────────────────

        private void ApplyGravity()
        {
            if (_grounded && _vel.y <= 0f) return;
            if (_gripping)                  return; // suspend gravity while clinging

            float g;
            if (_isRising && _vel.y > 0f)
            {
                g = _input.JumpHeld
                    ? GameConstants.GRAVITY_RISING_HELD       // variable height jump
                    : GameConstants.GRAVITY_RISING_RELEASED;
                if (_vel.y <= 0f) _isRising = false;
            }
            else
            {
                g         = GameConstants.GRAVITY_FALLING;
                _isRising = false;
            }

            _vel.y -= g * Time.deltaTime;
            _vel.y  = Mathf.Max(_vel.y, -GameConstants.MAX_FALL_SPEED);
        }

        // ── Corner correction ─────────────────────────────────────────────────

        private void TryCornerCorrectCeiling()
        {
            var b = _col.bounds;
            // Try small offsets left and right; if clear, nudge position
            for (int sign = 1; sign >= -1; sign -= 2)
            {
                float offset = sign * GameConstants.CORNER_CORRECTION_H;
                var   testCenter = (Vector2)b.center + new Vector2(offset, 0f);
                var   hit = Physics2D.BoxCast(testCenter,
                    new Vector2(b.size.x * 0.9f, _skinWidth), 0f, Vector2.up,
                    b.extents.y + _skinWidth, _solidLayer);
                if (hit.collider == null)
                {
                    transform.position += new Vector3(offset, 0f, 0f);
                    break;
                }
            }
        }

        // ── Peristaltic crush escape (GDD §7.4) ──────────────────────────────

        // Called by LivingTerrainEngine when player is caught between contracting walls
        public void EscapePeristalticCrush(Vector2 safePosition)
        {
            transform.position = safePosition;
            _vel = Vector2.zero;
            SymbiosisSystem.Instance?.Damage(GameConstants.PERISTALTIC_CRUSH_SYMB_COST);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void OnLanded()
        {
            _isRising          = false;
            _sameWallJumpCount = 0;
            _lastWallSide      = 0;
        }

        private void OnZoneChanged(SymbZoneEnteredEvent _) => PullSymbModifiers();

        private void PullSymbModifiers()
        {
            if (_symb == null) return;
            _runMult  = _symb.RunSpeedMult;
            _jumpMult = _symb.JumpMult;
            _gripMult = float.IsPositiveInfinity(_symb.GripMult) ? float.PositiveInfinity : _symb.GripMult;
        }

        // Converts a LayerMask to its first set layer index (-1 if none)
        private static int LayerMaskToLayer(LayerMask mask)
        {
            int value = mask.value;
            for (int i = 0; i < 32; i++)
                if ((value & (1 << i)) != 0) return i;
            return -1;
        }

        // ── Debug gizmos ──────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos || _col == null) return;
            var b  = _col.bounds;
            var sz = new Vector2(b.size.x - _skinWidth * 2f, _skinWidth * 2f);

            Gizmos.color = _grounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(b.center + Vector3.down  * b.extents.y, sz);

            Gizmos.color = _gripping ? Color.cyan : (_touchingOrganicCeiling ? Color.blue : Color.grey);
            Gizmos.DrawWireCube(b.center + Vector3.up * b.extents.y, sz);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(b.center + Vector3.left  * b.extents.x,
                new Vector3(_skinWidth * 2f, b.size.y - _skinWidth * 2f, 0f));
            Gizmos.DrawWireCube(b.center + Vector3.right * b.extents.x,
                new Vector3(_skinWidth * 2f, b.size.y - _skinWidth * 2f, 0f));
        }
#endif
    }
}
