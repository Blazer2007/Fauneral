using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// Place a trigger collider (box or edge) below the arena.
    /// When a player falls into it, FallDeath() is called automatically.
    /// </summary>
    public class FallDetector : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if what fell is a player
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
                health.FallDeath();
        }
    }
}