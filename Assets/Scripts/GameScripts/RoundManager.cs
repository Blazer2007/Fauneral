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

    private List<PlayerHealth> Players = new List<PlayerHealth>();

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
            
        // No Awake não procuramos mais jogadores fixos, faremos isso ao iniciar a ronda
    }

    private void Start()
    {
        if (IsServer)
        {
            // Começa a monitorizar jogadores que se ligam para inicializar os seus RoundWins
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            
            // Inicializa vitórias para quem já está ligado
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                HandleClientConnected(client.ClientId);
            }

            BeginCardSelection(ulong.MaxValue); 
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        // Precisamos de associar o clientId a um índice de jogador (0..3)
        // Por agora, assumimos que o RoundWins usa o PlayerIndex do PlayerHealth.
        // Como o PlayerHealth só existe no GameObject do jogador, 
        // as vitórias serão inicializadas quando a ronda começar e encontrarmos os componentes.
    }

    // ── ROUND LOOP ────────────────────────────────────────────
    [ClientRpc]
    public void StartRoundClientRpc()
    {
        if (MatchOver) return;

        RoundNumber++;
        RoundActive = true;

        // Refresh the list of players in the scene
        Players.Clear();
        Players.AddRange(FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None));
        Players.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

        foreach (var player in Players)
        {
            // Inicializa RoundWins para novos índices encontrados
            if (!RoundWins.ContainsKey(player.PlayerIndex))
                RoundWins[player.PlayerIndex] = 0;
                
            player.ResetHP();
        }

        _gameUI?.UpdateRoundWins(RoundWins);
        _gameUI?.HideEndScreen();

        GameObject spawnPointsParent = GameObject.Find("PlayerSpawnPoints");
        

        List<Transform> points = new List<Transform>();
        foreach (Transform t in spawnPointsParent.GetComponentsInChildren<Transform>())
            if (t != spawnPointsParent.transform) points.Add(t);

        GameUI gameUI = GameObject.FindFirstObjectByType<GameUI>(); // Tenta encontrar o GameUI na cena carregada
        if (gameUI == null)
        {
            Debug.LogError("[PlayerSpawner] GameUI não encontrado na GameScene!");
            return;
        }

        int i = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform point = points[i % points.Count];
            var player = GetComponent<PlayerHealth>();

            var health = player.GetComponent<PlayerHealth>();
            if (health != null) health.SetPlayerIndex(i);
            var netObj = player.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.transform.position = point.position;
            else
                Debug.LogError("[PlayerSpawner] Player prefab missing NetworkObject!");
            i++;
        }
        Debug.Log($"[RoundManager] Ronda {RoundNumber} iniciada com {Players.Count} jogadores.");


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

        
        if (alive.Count == 2)
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
            StartRoundClientRpc();
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

    private void Update()
    {
        if(_gameUI== null) return;
           
        //Atualiza todas as barras de vida de cada jogador a cada frame com a função UpdateIndividualHPBars do script GameUI.cs
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            _gameUI.UpdateIndividualHPBars(clientId);
        }

    }
}
