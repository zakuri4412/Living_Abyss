namespace LivingAbyss.Core
{
    // All values derived from GDD physics spec (px/frame at 60fps, PPU=16)
    // Conversion: speed = px/f × 60; accel = px/f² × 60²; then ÷ PPU for Unity units
    public static class GameConstants
    {
        public const int INTERNAL_WIDTH = 320;
        public const int INTERNAL_HEIGHT = 180;
        public const int RENDER_SCALE = 4;
        public const int PIXELS_PER_UNIT = 16;

        private const float FPS = 60f;
        private const float PPU = PIXELS_PER_UNIT;

        // Horizontal movement
        public const float MAX_RUN_SPEED     = 2.2f * FPS / PPU;           // 8.25 u/s
        public const float ACCELERATION      = 0.18f * FPS * FPS / PPU;    // 40.5 u/s²
        public const float DECEL_GROUND      = 0.26f * FPS * FPS / PPU;    // 58.5 u/s²
        public const float DECEL_NO_INPUT    = 0.14f * FPS * FPS / PPU;    // 31.5 u/s² (micro-slide)

        // Vertical movement
        public const float GRAVITY_FALLING         = 0.38f * FPS * FPS / PPU; // 85.5 u/s²
        public const float GRAVITY_RISING_HELD     = 0.28f * FPS * FPS / PPU; // 63.0 u/s²
        public const float GRAVITY_RISING_RELEASED = 0.55f * FPS * FPS / PPU; // 123.75 u/s²
        public const float JUMP_VELOCITY           = 6.4f  * FPS / PPU;       // 24.0 u/s
        public const float MAX_FALL_SPEED          = 9.0f  * FPS / PPU;       // 33.75 u/s

        // Timings (frames → seconds)
        public const float COYOTE_TIME_GROUND   = 8f  / FPS;   // 0.133s
        public const float COYOTE_TIME_CEILING  = 6f  / FPS;   // 0.100s
        public const float JUMP_BUFFER_TIME     = 10f / FPS;   // 0.167s
        public const float DROP_HOLD_TIME       = 6f  / FPS;   // 0.100s

        // Corner correction (px → Unity units)
        public const float CORNER_CORRECTION_H = 3f / PPU;     // ±3px horizontal
        public const float CORNER_CORRECTION_V = 2f / PPU;     // ±2px vertical

        // Wall jump
        public const int   WALL_JUMP_MAX_SAME_SIDE    = 2;
        public const float WALL_JUMP_VEL_PENALTY      = 0.25f; // −25% Y velocity per extra jump
        public const float WALL_JUMP_HORIZONTAL_FORCE = 6f * FPS / PPU;

        // Ceiling grip (Ký sinh bào)
        public const float CEILING_GRIP_BASE_DURATION = 4.5f;  // seconds

        // Peristaltic wall crush escape
        public const float PERISTALTIC_CRUSH_SYMB_COST = 0.05f;

        // Acid venom misuse cost
        public const float ACID_WRONG_POS_SYMB_COST = 0.08f;

        // Symbiosis zone thresholds (0-1)
        public const float SYMB_DANGER_MAX  = 0.20f;
        public const float SYMB_LOW_MAX     = 0.40f;
        public const float SYMB_MID_MAX     = 0.65f;
        public const float SYMB_HIGH_MAX    = 0.85f;
        // Above HIGH_MAX = Harmony

        // Symbiosis modifiers
        public const float DANGER_RUN_MULT      = 0.75f;
        public const float DANGER_JUMP_MULT     = 0.85f;
        public const float DANGER_PULSE_MULT    = 1.20f;
        public const float LOW_RUN_MULT         = 0.88f;
        public const float LOW_COYOTE_REDUCTION = 3f / FPS;    // -3f frames
        public const float LOW_ACID_CD_ADD      = 0.5f;
        public const float HIGH_GRIP_MULT       = 1.40f;
        public const float HIGH_CORNER_BONUS    = 4f / PPU;    // +4px corner correction
        public const float HARMONY_RUN_MULT     = 1.12f;
    }
}
