using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// Manages the player's HP, death, and respawn.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Stats")]
        [Tooltip("Drag a player health Scriptablefloat here")]
        [SerializeField] private ScriptableFloat _health;

        [Header("Player Settings")]
        [Tooltip("Player index: 0 = P1, 1 = P2, 2 = P3, 3 = P4")]
        [SerializeField] private int _playerIndex = 0;

        [Header("Respawn")]
        [Tooltip("The position this player respawns at")]
        [SerializeField] private Transform _spawnPoint;

        
        [field: Header("Runtime State (Read Only)")]
        [field: SerializeField] public float CurrentHP { get; private set; }
        [field: SerializeField] public bool IsAlive { get; private set; } = true;

        public int PlayerIndex => _playerIndex;
        public float MaxHP => _health != null ? _health.InitialValue : 100f;

        //private RoundManager _roundManager;

        private void Awake()
        {
           // _roundManager = FindFirstObjectByType<RoundManager>();

            if (_health == null)
                Debug.LogWarning($"[PlayerHealth] No PlayerHealthStats assigned to {gameObject.name}!", this);
        }

        private void Start()
        {
            ResetHP();
        }

        /// <summary>
        /// Apply damage to this player.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            CurrentHP -= amount;
            CurrentHP = Mathf.Clamp(CurrentHP, 0, MaxHP);

            if (CurrentHP <= 0)
                Die();
        }

        /// <summary>
        /// Instantly kills the player (e.g. fell off the map).
        /// </summary>
        public void FallDeath()
        {
            if (!IsAlive) return;
            CurrentHP = 0;
            Die();
        }

        private void Die()
        {
            IsAlive = false;
            gameObject.SetActive(false);
           // _roundManager?.OnPlayerDied(this);
        }

        /// <summary>
        /// Resets HP and re-enables the player. Called between rounds.
        /// </summary>
        public void ResetHP()
        {
            CurrentHP = MaxHP;
            IsAlive = true;
            gameObject.SetActive(true);

            if (_spawnPoint != null)
                transform.position = _spawnPoint.position;
        }
    }
}