using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// ScriptableObject that holds all round/match settings.
    /// Create via: Right Click > Create > Fauneral > Round Settings
    /// </summary>
    [CreateAssetMenu(fileName = "RoundSettings", menuName = "Fauneral/Round Settings")]
    public class RoundSettings : ScriptableObject
    {
        [Header("Match")]
        [Tooltip("How many rounds a player needs to win the match")]
        [Min(1)] public int RoundsToWin = 5;

        [Header("Timing")]
        [Tooltip("Delay in seconds between a round ending and the next one starting")]
        [Range(0f, 10f)] public float RoundEndDelay = 2f;

        [Tooltip("Time limit per round in seconds. Set to 0 to disable.")]
        [Range(0f, 300f)] public float RoundTimeLimit = 0f;
    }
}