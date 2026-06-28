using UnityEngine;

namespace LivingAbyss.Camera
{
    // Place one per room. Player entering the trigger sets the active camera bounds.
    // Size the BoxCollider2D to cover the entire playable area of the room.
    [RequireComponent(typeof(BoxCollider2D))]
    public class CameraBounds : MonoBehaviour
    {
        private BoxCollider2D _col;

        private void Awake()
        {
            _col           = GetComponent<BoxCollider2D>();
            _col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            CameraController.Instance?.SetBounds(_col.bounds);
        }

        // Optional: clear bounds when leaving (open-world sections)
        // private void OnTriggerExit2D(Collider2D other)
        // {
        //     if (other.CompareTag("Player"))
        //         CameraController.Instance?.ClearBounds();
        // }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null) return;
            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
            Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
        }
#endif
    }
}
