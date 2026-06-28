using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using LivingAbyss.Player;
using LivingAbyss.Systems;

namespace LivingAbyss.Editor
{
    // Menu: Living Abyss > Setup Player GameObject
    // Creates a ready-to-run Player with all required components.
    public static class PlayerSetupTool
    {
        [MenuItem("Living Abyss/Setup Player GameObject")]
        public static void SetupPlayer()
        {
            // --- Player root ---
            var go = new GameObject("Player_DrYenMinh");
            go.tag   = "Player";
            go.layer = LayerMask.NameToLayer("Player");

            // Rigidbody2D (gravity managed manually by PlayerController)
            var rb              = go.AddComponent<Rigidbody2D>();
            rb.gravityScale     = 0f;
            rb.freezeRotation   = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation    = RigidbodyInterpolation2D.Interpolate;

            // Collider — 12×18px at PPU=16 → 0.75 × 1.125 units
            var col             = go.AddComponent<CapsuleCollider2D>();
            col.size            = new Vector2(0.65f, 1.05f);  // slightly smaller than sprite for fairness
            col.offset          = new Vector2(0f, 0f);
            col.direction       = CapsuleDirection2D.Vertical;

            // Sprite renderer (placeholder — replace with actual sprite sheet)
            var sr              = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder     = 10;

            // Input System
            var playerInput     = go.AddComponent<PlayerInput>();
            var inputActions    = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            if (inputActions != null)
            {
                playerInput.actions             = inputActions;
                playerInput.defaultActionMap    = "Player";
                playerInput.notificationBehavior = PlayerNotifications.SendMessages;
            }
            else
            {
                Debug.LogWarning("[PlayerSetup] InputSystem_Actions.inputactions not found at Assets/.");
            }

            // Player scripts
            go.AddComponent<PlayerInputHandler>();
            var ctrl = go.AddComponent<PlayerController>();

            // Set layer masks (assumes layers exist; add them in Project Settings > Tags & Layers)
            var solidMask    = LayerMask.GetMask("Ground", "Solid");
            var organicMask  = LayerMask.GetMask("OrganicTerrain");
            var platformMask = LayerMask.GetMask("Platform");
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("_solidLayer").intValue    = solidMask;
            so.FindProperty("_organicLayer").intValue  = organicMask;
            so.FindProperty("_platformLayer").intValue = platformMask;
            so.ApplyModifiedProperties();

            // --- SymbiosisSystem (separate persistent GameObject) ---
            if (SymbiosisSystem.Instance == null)
            {
                var symbGo = new GameObject("SymbiosisSystem");
                symbGo.AddComponent<SymbiosisSystem>();
                Undo.RegisterCreatedObjectUndo(symbGo, "Create SymbiosisSystem");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Player");
            Selection.activeGameObject = go;
            Debug.Log("[PlayerSetup] Player created. " +
                      "Add layers: Ground, Solid, OrganicTerrain, Platform, Player in Project Settings.");
        }

        [MenuItem("Living Abyss/Setup Player GameObject", validate = true)]
        public static bool ValidateSetupPlayer()
        {
            return !Application.isPlaying;
        }
    }
}
