using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Networking;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private RoundSettings _settings;

    private List<PlayerHealth> Players = new List<PlayerHealth>();

    [Header("Runtime State (Read Only)")]
    public bool MatchOver { get; private set; } = false;
    public bool RoundActive { get; private set; } = false;

    public Dictionary<int, int> RoundWins { get; private set; } = new Dictionary<int, int>();
    public int RoundNumber { get; private set; } = 0;

    private GameUI _gameUI;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _gameUI = FindFirstObjectByType<GameUI>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            StartCoroutine(WaitAndBeginFirstRound());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (!IsServer || MatchOver) return;

        // Se sobrar apenas 1 (ou 0) jogadores ativos, termina a partida e volta para o lobby
        if (NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            Debug.Log("[RoundManager] Jogadores insuficientes após desconexão. Retornando ao lobby.");
            ReturnToLobby();
        }
    }

    private IEnumerator WaitAndBeginFirstRound()
    {
        // Espera um pouco para garantir que o PlayerSpawner terminou o seu trabalho
        yield return new WaitForSeconds(2.0f);

        int expectedPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        
        // Se já começarmos com menos de 2 (não deveria acontecer pelo Lobby), volta
        if (expectedPlayers < 2)
        {
            ReturnToLobby();
            yield break;
        }

        float timeout = 10f;

        PlayerHealth[] allPlayers = null;
        while (timeout > 0)
        {
            allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            if (allPlayers.Length >= expectedPlayers && expectedPlayers > 0)
                break;

            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }

        int playerCount = allPlayers != null ? allPlayers.Length : 0;
        Debug.Log($"[RoundManager] {playerCount} jogadores prontos. Iniciando primeira fase de cartas.");
        
        if (!string.IsNullOrEmpty(LobbySessionData.Pin))
        {
            DiscoveryManager.Instance?.LogMatchStart(LobbySessionData.Pin, playerCount);
        }

        BeginCardSelection(ulong.MaxValue); 
    }

    // Método chamado pelo CardSelectionManager no Servidor quando todos escolheram cartas
    public void StartNextRound()
    {
        if (!IsServer) return;
        StartRoundClientRpc();
    }

    [ClientRpc]
    private void StartRoundClientRpc()
    {
        if (MatchOver) return;

        RoundNumber++;
        RoundActive = true;

        // Atualiza lista local de jogadores
        Players.Clear();
        Players.AddRange(FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None));
        Players.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

        foreach (var player in Players)
        {
            if (!RoundWins.ContainsKey(player.PlayerIndex))
                RoundWins[player.PlayerIndex] = 0;
                
            player.ResetHP();
        }

        _gameUI?.UpdateRoundWins(RoundWins);
        _gameUI?.HideEndScreen();

        // Reposicionamento
        GameObject spawnPointsParent = GameObject.Find("PlayerSpawnPoints");
        if (spawnPointsParent != null)
        {
            List<Transform> points = new List<Transform>();
            foreach (Transform t in spawnPointsParent.GetComponentsInChildren<Transform>())
                if (t != spawnPointsParent.transform) points.Add(t);

            for (int i = 0; i < Players.Count; i++)
            {
                if (i >= points.Count) break;
                Transform point = points[i];
                Players[i].transform.position = point.position;
                Players[i].SetPlayerState(true);
            }
        }
        
        Debug.Log($"[RoundManager] Ronda {RoundNumber} iniciada.");
    }

    public void OnPlayerDied(PlayerHealth deadPlayer)
    {
        if (!IsServer || !RoundActive) return;

        var alive = new List<PlayerHealth>();
        foreach (var p in Players)
            if (p != null && p.IsAlive) alive.Add(p);

        if (alive.Count <= 1)
        {
            RoundActive = false;
            PlayerHealth winner = (alive.Count == 1) ? alive[0] : null; 
            StartCoroutine(EndRound(winner));
        }
    }

    private IEnumerator EndRound(PlayerHealth winner)
    {
        float delay = _settings != null ? _settings.RoundEndDelay : 2f;
        int roundsToWin = _settings != null ? _settings.RoundsToWin : 5;
        ulong winnerId = ulong.MaxValue;

        if (winner != null)
        {
            RoundWins[winner.PlayerIndex]++;
            _gameUI?.ShowRoundWinner(winner.PlayerIndex, RoundWins);
            winnerId = winner.OwnerClientId;

            if (RoundWins[winner.PlayerIndex] >= roundsToWin)
            {
                MatchOver = true;
                _gameUI?.ShowMatchWinner(winner.PlayerIndex);
                
                if (!string.IsNullOrEmpty(LobbySessionData.Pin))
                    DiscoveryManager.Instance?.LogMatchEnd(LobbySessionData.Pin, winner.PlayerIndex, RoundNumber);

                yield return new WaitForSeconds(5f); // Mostra o vencedor por 5 segundos
                ReturnToLobby();
                yield break;
            }
        }
        else
        {
            _gameUI?.ShowDraw();
        }

        yield return new WaitForSeconds(delay);

        if (IsServer)
            BeginCardSelection(winnerId);
    }

    private void ReturnToLobby()
    {
        if (!IsServer) return;
        
        string pin = LobbySessionData.Pin;
        if (!string.IsNullOrEmpty(pin) && LobbyServerManager.Instance != null)
        {
            LobbyServerManager.Instance.ReturnAllToLobby(pin);
        }
        else
        {
            Debug.Log("[RoundManager] Retornando ao LobbyMenu (sem PIN ou Manager).");
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    public void RestartMatch()
    {
        if (!IsServer) return;
        RestartMatchClientRpc();
    }

    [ClientRpc]
    private void RestartMatchClientRpc()
    {
        MatchOver = false;
        RoundNumber = 0;
        RoundWins.Clear();
        if (IsServer) BeginCardSelection(ulong.MaxValue);
    }

    private void BeginCardSelection(ulong winnerId)
    {
        if (!IsServer) return;
        int nextRound = RoundNumber + 1;

        if (CardSelectionManager.Instance != null)
            CardSelectionManager.Instance.BeginSelection(nextRound, winnerId);
        else
            StartNextRound();
    }
}