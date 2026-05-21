using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// Manages the round loop: start, end, winner detection, round counter, and match end.
    
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Drag a RoundSettings ScriptableObject here")]
        [SerializeField] private RoundSettings _settings;

        [Header("Players")]
        [Tooltip("Drag all PlayerHealth components here (one per player)")]
        [SerializeField] private List<PlayerHealth> Players = new List<PlayerHealth>();

        
        [Header("Runtime State (Read Only)")]
        [SerializeField] public bool MatchOver { get; private set; } = false;
        [SerializeField] public bool RoundActive { get; private set; } = false;

        // Tracks how many rounds each player has won. Key = PlayerIndex.
        public Dictionary<int, int> RoundWins { get; private set; } = new Dictionary<int, int>();

        private GameUI _gameUI;

        private void Awake()
        {
            _gameUI = FindFirstObjectByType<GameUI>();

            if (_settings == null)
                Debug.LogWarning("[RoundManager] No RoundSettings assigned!", this);

            foreach (var player in Players)
                RoundWins[player.PlayerIndex] = 0;
        }

        private void Start()
        {
            StartRound();
        }

        /// <summary>
        /// Resets all players and starts a new round.
        /// </summary>
        public void StartRound()
        {
            if (MatchOver) return;

            foreach (var player in Players)
                player.ResetHP();

            RoundActive = true;
            _gameUI?.UpdateRoundWins(RoundWins);
            _gameUI?.HideEndScreen();
        }

        /// <summary>
        /// Called by PlayerHealth whenever a player dies.
        /// </summary>
        public void OnPlayerDied(PlayerHealth deadPlayer)
        {
            if (!RoundActive) return;

            List<PlayerHealth> alive = new List<PlayerHealth>();
            foreach (var p in Players)
                if (p.IsAlive) alive.Add(p);

            if (alive.Count <= 1)
            {
                RoundActive = false;
                PlayerHealth winner = alive.Count == 1 ? alive[0] : null;
                StartCoroutine(EndRound(winner));
            }
        }

        private IEnumerator EndRound(PlayerHealth winner)
        {
            int delay = _settings != null ? (int)_settings.RoundEndDelay : 2;
            int roundsToWin = _settings != null ? _settings.RoundsToWin : 5;

            if (winner != null)
            {
                RoundWins[winner.PlayerIndex]++;
                _gameUI?.ShowRoundWinner(winner.PlayerIndex, RoundWins);

                if (RoundWins[winner.PlayerIndex] >= roundsToWin)
                {
                    MatchOver = true;
                    _gameUI?.ShowMatchWinner(winner.PlayerIndex);
                    yield break;
                }
            }
            else
            {
                _gameUI?.ShowDraw();
            }

            yield return new WaitForSeconds(delay);
            StartRound();
        }

        /// <summary>
        /// Resets the entire match (call this from a "Play Again" button).
        /// </summary>
        public void RestartMatch()
        {
            MatchOver = false;
            foreach (var key in new List<int>(RoundWins.Keys))
                RoundWins[key] = 0;

            StartRound();
        }
    }
}