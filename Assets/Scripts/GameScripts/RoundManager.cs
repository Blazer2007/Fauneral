using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Versão actualizada do RoundManager.
/// Alterações face ao original:
///   - Conta rondas (_roundNumber)
///   - Em vez de chamar StartRound() directamente após EndRound(),
///     chama CardSelectionManager.BeginSelection() e aguarda que
///     o CardSelectionManager chame StartRound() quando todos escolherem.
///   - StartRound() passa a ser público para o CardSelectionManager o invocar.
/// </summary>
public class RoundManager : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private RoundSettings _settings;

    [Header("Players")]
    [SerializeField] private List<PlayerHealth> Players = new List<PlayerHealth>();

    [Header("Runtime State (Read Only)")]
    public bool MatchOver { get; private set; } = false;
    public bool RoundActive { get; private set; } = false;

    public Dictionary<int, int> RoundWins { get; private set; } = new Dictionary<int, int>();

    // Contador de rondas jogadas (começa em 1 na primeira ronda real)
    public int RoundNumber { get; private set; } = 0;

    private GameUI _gameUI;

    // ── UNITY ─────────────────────────────────────────────────

    private void Awake()
    {
        _gameUI = FindFirstObjectByType<GameUI>();

        if (_settings == null)
            Debug.LogWarning("[RoundManager] No RoundSettings assigned!", this);

        Players.AddRange(FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None));
        Players.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

        foreach (var player in Players)
            RoundWins[player.PlayerIndex] = 0;
    }

    private void Start()
    {
        // Abre a fase de selecção antes da primeira ronda
        // O CardSelectionManager chamará StartRound() quando todos escolherem
        if (IsServer)
            BeginCardSelection(ulong.MaxValue); // Sem vencedor na primeira ronda
    }

    // ── ROUND LOOP ────────────────────────────────────────────

    /// <summary>
    /// Chamado pelo CardSelectionManager quando todos os jogadores escolheram.
    /// </summary>
    public void StartRound()
    {
        if (MatchOver) return;

        RoundNumber++;

        foreach (var player in Players)
            player.ResetHP();

        RoundActive = true;
        _gameUI?.UpdateRoundWins(RoundWins);
        _gameUI?.HideEndScreen();

        Debug.Log($"[RoundManager] Ronda {RoundNumber} iniciada.");
    }

    /// <summary>
    /// Chamado pelo PlayerHealth quando um jogador morre.
    /// </summary>
    public void OnPlayerDied(PlayerHealth deadPlayer)
    {
        if (!RoundActive) return;

        var alive = new List<PlayerHealth>();
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
        float delay = _settings != null ? _settings.RoundEndDelay : 2f;
        int roundsToWin = _settings != null ? _settings.RoundsToWin : 5;

        ulong winnerId = ulong.MaxValue; // Empate por defeito

        if (winner != null)
        {
            RoundWins[winner.PlayerIndex]++;
            _gameUI?.ShowRoundWinner(winner.PlayerIndex, RoundWins);

            // Determina o clientId do vencedor para o CardSelectionManager
            winnerId = GetClientIdOfPlayer(winner);

            if (RoundWins[winner.PlayerIndex] >= roundsToWin)
            {
                MatchOver = true;
                _gameUI?.ShowMatchWinner(winner.PlayerIndex);
                yield break; // Jogo terminou, não há mais selecção de cartas
            }
        }
        else
        {
            _gameUI?.ShowDraw();
        }

        yield return new WaitForSeconds(delay);

        // Em vez de StartRound() directo → abre selecção de cartas
        if (IsServer)
            BeginCardSelection(winnerId);
    }

    // ── RESTART ───────────────────────────────────────────────

    /// <summary>
    /// Reinicia o match completo (botão "Play Again").
    /// </summary>
    public void RestartMatch()
    {
        MatchOver = false;
        RoundNumber = 0;

        foreach (var key in new List<int>(RoundWins.Keys))
            RoundWins[key] = 0;

        if (IsServer)
            BeginCardSelection(ulong.MaxValue);
    }

    // ── HELPERS ───────────────────────────────────────────────

    private void BeginCardSelection(ulong winnerId)
    {
        // RoundNumber ainda não incrementou (acontece em StartRound),
        // por isso passamos RoundNumber+1 como "próxima ronda"
        int nextRound = RoundNumber + 1;

        if (CardSelectionManager.Instance != null)
            CardSelectionManager.Instance.BeginSelection(nextRound, winnerId);
        else
        {
            Debug.LogWarning("[RoundManager] CardSelectionManager não encontrado. A iniciar ronda sem selecção.");
            StartRound();
        }
    }

    /// <summary>
    /// Devolve o clientId do dono de um PlayerHealth.
    /// Requer que o jogador tenha um NetworkObject no mesmo GameObject.
    /// </summary>
    private ulong GetClientIdOfPlayer(PlayerHealth player)
    {
        if (player == null) return ulong.MaxValue;
        var netObj = player.GetComponent<NetworkObject>();
        return netObj != null ? netObj.OwnerClientId : ulong.MaxValue;
    }
}