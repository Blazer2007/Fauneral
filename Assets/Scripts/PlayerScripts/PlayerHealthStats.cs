using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// ScriptableObject that holds all health-related settings for a player.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerHealthStats", menuName = "Fauneral/Player Health Stats")]
    public class PlayerHealthStats : ScriptableObject
    {
        [Header("HP")]
        [Tooltip("Maximum HP for this player")]
        public float MaxHP = 100f;

        [Header("Respawn")]
        [Tooltip("Delay in seconds before the player respawns after dying")]
        [Range(0f, 5f)] public float RespawnDelay = 3f;
    }
}