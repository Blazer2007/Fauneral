using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RoundManager : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private RoundSettings _settings;

    public bool MatchOver { get; private set; } = false;
    public bool RoundActive { get; private set; } = false;
    public Dictionary<int, int> RoundWins { get; private set; } = new();
    public int RoundNumber { get; private set; } = 0;

    private GameUI _gameUI;
    private List<PlayerHealth> _players = new();

    private void Awake()
    {
        _gameUI = FindFirstObjectByType<GameUI>();
    }

    private void Start()
    {
        if (!IsServer) return;
        // Aguarda um momento para os jogadores spawnarem antes de começar
        StartCoroutine(WaitAndStartRound());
    }

    private IEnumerator WaitAndStartRound()
    {
        yield return new WaitForSeconds(1f);
        StartRound();
    }

    public void StartRound()
    {
        if (!IsServer) return;
        if (MatchOver) return;

        RoundNumber++;
        RoundActive = true;

        // Recolhe jogadores do PlayerSpawner
        _players.Clear();
        if (PlayerSpawner.Instance != null)
            _players.AddRange(PlayerSpawner.Instance.SpawnedPlayers);

        List<Transform> points = PlayerSpawner.Instance?.GetSpawnPoints() ?? new();

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i] == null) continue;

            // Inicializa wins
            if (!RoundWins.ContainsKey(_players[i].PlayerIndex))
                RoundWins[_players[i].PlayerIndex] = 0;

            // Reset HP e posição
            _players[i].ResetHP();
            if (i < points.Count)
                _players[i].TeleportTo(points[i].position);
        }

        UpdateUIClientRpc();
        Debug.Log($"[RoundManager] Ronda {RoundNumber} iniciada com {_players.Count} jogadores.");
    }

    [ClientRpc]
    private void UpdateUIClientRpc()
    {
        _gameUI?.UpdateRoundWins(RoundWins);
        _gameUI?.HideEndScreen();
    }

    public void OnPlayerDied(PlayerHealth deadPlayer)
    {
        if (!IsServer) return;
        if (!RoundActive) return;

        var alive = new List<PlayerHealth>();
        foreach (var p in _players)
            if (p != null && p.IsAlive) alive.Add(p);

        if (alive.Count <= 1)
        {
            RoundActive = false;
            PlayerHealth winner = alive.Count == 1 ? alive[0] : null;
            StartCoroutine(EndRound(winner));
        }
    }

    private IEnumerator EndRound(PlayerHealth winner)
    {
        yield return new WaitForSeconds(0.3f);

        int roundsToWin = _settings != null ? _settings.RoundsToWin : 5;
        ulong winnerId = ulong.MaxValue;

        if (winner != null)
        {
            RoundWins[winner.PlayerIndex]++;
            ulong wId = winner.GetComponent<NetworkObject>()?.OwnerClientId ?? ulong.MaxValue;
            ShowRoundWinnerClientRpc(winner.PlayerIndex);

            if (RoundWins[winner.PlayerIndex] >= roundsToWin)
            {
                MatchOver = true;
                ShowMatchWinnerClientRpc(winner.PlayerIndex);
                yield break;
            }

            winnerId = wId;
        }
        else
        {
            ShowDrawClientRpc();
        }

        float delay = _settings != null ? _settings.RoundEndDelay : 2f;
        yield return new WaitForSeconds(delay);

        if (CardSelectionManager.Instance != null)
            CardSelectionManager.Instance.BeginSelection(RoundNumber + 1, winnerId);
        else
            StartRound();
    }

    [ClientRpc] private void ShowRoundWinnerClientRpc(int playerIndex) => _gameUI?.ShowRoundWinner(playerIndex, RoundWins);
    [ClientRpc] private void ShowMatchWinnerClientRpc(int playerIndex) => _gameUI?.ShowMatchWinner(playerIndex);
    [ClientRpc] private void ShowDrawClientRpc() => _gameUI?.ShowDraw();

    public void RestartMatch()
    {
        if (!IsServer) return;
        MatchOver = false;
        RoundNumber = 0;
        foreach (var key in new List<int>(RoundWins.Keys))
            RoundWins[key] = 0;

        if (CardSelectionManager.Instance != null)
            CardSelectionManager.Instance.BeginSelection(1, ulong.MaxValue);
        else
            StartRound();
    }
}