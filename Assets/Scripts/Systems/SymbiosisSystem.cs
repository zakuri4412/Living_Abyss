using UnityEngine;
using LivingAbyss.Core;

namespace LivingAbyss.Systems
{
    // Replaces HP. Represents how much the cave accepts the player's presence. (GDD §4.1)
    public class SymbiosisSystem : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _value = 0.5f;

        public static SymbiosisSystem Instance { get; private set; }

        public float Value => _value;
        public SymbiosisZone Zone { get; private set; } = SymbiosisZone.Mid;

        // Cached modifiers — consumers read these instead of querying Zone directly
        public float RunSpeedMult     { get; private set; } = 1f;
        public float JumpMult         { get; private set; } = 1f;
        public float GripMult         { get; private set; } = 1f;  // float.PositiveInfinity = infinite grip
        public float TilePulseMult    { get; private set; } = 1f;
        public float CoyoteReduction  { get; private set; } = 0f;  // subtracted from coyote time
        public float AcidCooldownAdd  { get; private set; } = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Zone = Classify(_value);
            RefreshModifiers();
        }

        public void Add(float amount)
        {
            float old = _value;
            _value = Mathf.Clamp01(_value + amount);
            Dispatch(old, _value);
        }

        public void Damage(float amount)
        {
            float old = _value;
            _value = Mathf.Clamp01(_value - amount);
            EventBus.Publish(new SymbDamageEvent { Amount = amount });
            Dispatch(old, _value);

            if (_value <= 0f)
                EventBus.Publish(new PlayerDeathEvent());
        }

        private void Dispatch(float old, float current)
        {
            EventBus.Publish(new SymbChangedEvent { OldValue = old, NewValue = current });
            var newZone = Classify(current);
            if (newZone == Zone) return;
            Zone = newZone;
            EventBus.Publish(new SymbZoneEnteredEvent { Zone = newZone });
            RefreshModifiers();
        }

        private void RefreshModifiers()
        {
            CoyoteReduction = 0f;
            AcidCooldownAdd = 0f;

            switch (Zone)
            {
                case SymbiosisZone.Danger:
                    RunSpeedMult  = GameConstants.DANGER_RUN_MULT;
                    JumpMult      = GameConstants.DANGER_JUMP_MULT;
                    GripMult      = 1f;
                    TilePulseMult = GameConstants.DANGER_PULSE_MULT;
                    break;

                case SymbiosisZone.Low:
                    RunSpeedMult    = GameConstants.LOW_RUN_MULT;
                    JumpMult        = 1f;
                    GripMult        = 1f;
                    TilePulseMult   = 1f;
                    CoyoteReduction = GameConstants.LOW_COYOTE_REDUCTION;
                    AcidCooldownAdd = GameConstants.LOW_ACID_CD_ADD;
                    break;

                case SymbiosisZone.Mid:
                    RunSpeedMult  = 1f;
                    JumpMult      = 1f;
                    GripMult      = 1f;
                    TilePulseMult = 1f;
                    break;

                case SymbiosisZone.High:
                    RunSpeedMult  = 1f;
                    JumpMult      = 1f;
                    GripMult      = GameConstants.HIGH_GRIP_MULT;
                    TilePulseMult = 1f;
                    break;

                case SymbiosisZone.Harmony:
                    RunSpeedMult  = GameConstants.HARMONY_RUN_MULT;
                    JumpMult      = 1f;
                    GripMult      = float.PositiveInfinity; // unlimited ceiling grip
                    TilePulseMult = 1f;
                    break;
            }
        }

        private static SymbiosisZone Classify(float v)
        {
            if (v < GameConstants.SYMB_DANGER_MAX) return SymbiosisZone.Danger;
            if (v < GameConstants.SYMB_LOW_MAX)    return SymbiosisZone.Low;
            if (v < GameConstants.SYMB_MID_MAX)    return SymbiosisZone.Mid;
            if (v < GameConstants.SYMB_HIGH_MAX)   return SymbiosisZone.High;
            return SymbiosisZone.Harmony;
        }

#if UNITY_EDITOR
        // Hot-reload helper: re-apply modifiers when value is tweaked in Inspector
        private void OnValidate()
        {
            Zone = Classify(_value);
            RefreshModifiers();
        }
#endif
    }
}
