using UnityEngine;
using UnityEngine.InputSystem;
using LivingAbyss.Core;

namespace LivingAbyss.Player
{
    // Reads raw input, exposes buffered/processed values to PlayerController.
    // Uses Send Messages behavior — callbacks signature: OnActionName(InputValue value)
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputHandler : MonoBehaviour
    {
        // --- Horizontal ---
        public float MoveX        { get; private set; }

        // --- Vertical intent (for drop-through) ---
        public bool MoveDownHeld  { get; private set; }

        // --- Jump ---
        public bool JumpHeld      { get; private set; }
        public bool JumpBuffered  { get; private set; }

        // --- Skills ---
        public bool CeilingGripHeld  { get; private set; }
        public bool AcidVenomHeld    { get; private set; }
        public bool SymbiosisHeld    { get; private set; }
        public bool BioRhythmPressed { get; private set; }

        // --- UI ---
        public bool InteractPressed  { get; private set; }
        public bool InventoryPressed { get; private set; }
        public bool MapPressed       { get; private set; }
        public bool JournalPressed   { get; private set; }
        public bool PausePressed     { get; private set; }

        // Drop-through: held ↓ for DROP_HOLD_TIME seconds
        public bool DropPlatformTriggered { get; private set; }
        private float _dropTimer;

        // Jump buffer timer
        private float _jumpBufferTimer;

        private void Update()
        {
            // Tick jump buffer
            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= Time.deltaTime;
                JumpBuffered = _jumpBufferTimer > 0f;
            }

            // Tick drop-through hold
            if (MoveDownHeld)
            {
                _dropTimer += Time.deltaTime;
                DropPlatformTriggered = _dropTimer >= GameConstants.DROP_HOLD_TIME;
            }
            else
            {
                _dropTimer = 0f;
                DropPlatformTriggered = false;
            }
        }

        public void ConsumeJumpBuffer()
        {
            _jumpBufferTimer = 0f;
            JumpBuffered     = false;
        }

        public void ConsumeDropPlatform()
        {
            _dropTimer            = 0f;
            DropPlatformTriggered = false;
        }

        // ── Input System — Send Messages callbacks ────────────────────────────
        // PlayerInput (Send Messages) calls OnActionName(InputValue value) on this GameObject.

        public void OnMove(InputValue value)
        {
            var v        = value.Get<Vector2>();
            MoveX        = v.x;
            MoveDownHeld = v.y < -0.5f;
        }

        public void OnJump(InputValue value)
        {
            JumpHeld = value.isPressed;
            if (value.isPressed)
            {
                _jumpBufferTimer = GameConstants.JUMP_BUFFER_TIME;
                JumpBuffered     = true;
            }
        }

        public void OnCeilingGrip(InputValue value)
        {
            CeilingGripHeld = value.isPressed;
        }

        public void OnAcidVenom(InputValue value)
        {
            AcidVenomHeld = value.isPressed;
        }

        public void OnSymbiosis(InputValue value)
        {
            SymbiosisHeld = value.isPressed;
        }

        public void OnBioRhythm(InputValue value)
        {
            BioRhythmPressed = value.isPressed;
        }

        public void OnInteract(InputValue value)
        {
            InteractPressed = value.isPressed;
        }

        public void OnInventory(InputValue value)
        {
            InventoryPressed = value.isPressed;
        }

        public void OnMap(InputValue value)
        {
            MapPressed = value.isPressed;
        }

        public void OnJournal(InputValue value)
        {
            JournalPressed = value.isPressed;
        }

        public void OnPause(InputValue value)
        {
            PausePressed = value.isPressed;
        }
    }
}
