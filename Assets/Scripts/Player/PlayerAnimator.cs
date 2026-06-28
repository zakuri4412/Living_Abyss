using UnityEngine;
using LivingAbyss.Systems;

namespace LivingAbyss.Player
{
    // Drives the Animator based on controller state.
    // Uses integer hashes — never string lookups in Update.
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int _hashSpeed      = Animator.StringToHash("Speed");
        private static readonly int _hashGrounded   = Animator.StringToHash("Grounded");
        private static readonly int _hashVelY       = Animator.StringToHash("VelY");
        private static readonly int _hashGripping   = Animator.StringToHash("Gripping");
        private static readonly int _hashSymbZone   = Animator.StringToHash("SymbZone");

        private Animator        _anim;
        private SpriteRenderer  _sr;
        private PlayerController _ctrl;
        private Rigidbody2D     _rb;
        private SymbiosisSystem _symb;

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _sr   = GetComponent<SpriteRenderer>();
            _ctrl = GetComponent<PlayerController>();
            _rb   = GetComponent<Rigidbody2D>();
        }

        private void Start() => _symb = SymbiosisSystem.Instance;

        private void Update()
        {
            var vel = _rb.linearVelocity;

            _anim.SetFloat(_hashSpeed,    Mathf.Abs(vel.x));
            _anim.SetFloat(_hashVelY,     vel.y);
            _anim.SetBool(_hashGrounded,  IsGrounded());
            _anim.SetBool(_hashGripping,  _ctrl != null && IsGripping());
            _anim.SetInteger(_hashSymbZone, _symb != null ? (int)_symb.Zone : 2);

            // Flip sprite to face movement direction
            if (vel.x > 0.1f)        _sr.flipX = false;
            else if (vel.x < -0.1f)  _sr.flipX = true;
        }

        private bool IsGrounded() => _ctrl != null ? _ctrl.IsGrounded : Mathf.Abs(_rb.linearVelocity.y) < 0.05f;
        private bool IsGripping() => _ctrl != null && _ctrl.IsGripping;
    }
}
