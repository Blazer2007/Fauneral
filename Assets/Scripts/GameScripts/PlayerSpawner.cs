using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }
    [SerializeField] private GameObject _playerPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[PlayerSpawner] Awake chamado");
    }

    private void Start()
    {
        Debug.Log($"[PlayerSpawner] Start - NetworkManager existe: {NetworkManager.Singleton != null}");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[PlayerSpawner] NetworkManager.Singleton is null in Start!");
            return;
        }

        // Subscreve ao evento de conexão do NetworkManager para esperar pela rede iniciar
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        
        // Se o servidor já iniciou antes do Start (ex: host já ativo), chama manualmente
        if (NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[PlayerSpawner] Server already listening, calling OnServerStarted manually.");
            OnServerStarted();
        }
        
        Debug.Log("[PlayerSpawner] Subscribed to OnServerStarted");
    }

    private void OnServerStarted()
    {
        Debug.Log("[PlayerSpawner] OnServerStarted called - subscribing to OnLoadEventCompleted");
        // Evita subscrição dupla
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode mode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"[PlayerSpawner] OnSceneLoadCompleted - scene: {sceneName}, IsServer: {NetworkManager.Singleton.IsServer}");

        if (!NetworkManager.Singleton.IsServer) return;
        
        // Compara ignorando maiúsculas/minúsculas e possivelmente caminhos
        if (!sceneName.EndsWith("GameScene"))
        {
            Debug.Log($"[PlayerSpawner] Scene {sceneName} is not GameScene, skipping spawn.");
            return;
        }

        Debug.Log($"[PlayerSpawner] Spawning {clientsCompleted.Count} players in {sceneName}");

        GameObject spawnPointsParent = GameObject.Find("PlayerSpawnPoints");
        if (spawnPointsParent == null)
        {
            Debug.LogError($"[PlayerSpawner] 'PlayerSpawnPoints' not found in {sceneName}!");
            return;
        }

        List<Transform> points = new List<Transform>();
        foreach (Transform t in spawnPointsParent.GetComponentsInChildren<Transform>())
            if (t != spawnPointsParent.transform) points.Add(t);

        if (points.Count == 0)
        {
            Debug.LogError("[PlayerSpawner] No spawn points found under 'PlayerSpawnPoints'!");
            return;
        }

        Debug.Log($"[PlayerSpawner] Found {points.Count} spawn points");

        GameUI gameUI = GameObject.FindFirstObjectByType<GameUI>(); // Tenta encontrar o GameUI na cena carregada
        if (gameUI == null)
        {
            Debug.LogError("[PlayerSpawner] GameUI não encontrado na GameScene!");
            return;
        }

        int i = 0;
        foreach (ulong clientId in clientsCompleted)
        {
            Transform point = points[i % points.Count];
            var player = Instantiate(_playerPrefab, point.position, point.rotation);
            var netObj = player.GetComponent<NetworkObject>();
            var health = player.GetComponent<PlayerHealth>();
            if (health != null) health.SetPlayerIndex(i);
            if (netObj != null)
            {
                netObj.SpawnAsPlayerObject(clientId, true);
                gameUI.ConnectHPBarsAndPlayerHPWithPlayers(clientId);
            }
            else
            {
                Debug.LogError("[PlayerSpawner] Player prefab missing NetworkObject!");
            }
            i++;
           
            gameUI.RefreshPlayerListClientRpc();
        }


        
    }

    // Método para respawnar os jogadores após outra ronda começar, depois da escolha de cartas (Depois chamado no script RoundManager), mantendo os stats de upgrade dos jogadores
    //public void RespawnPlayersForNewRound()
    //{
    //    Debug.Log("[PlayerSpawner] Respawning players for new round");
    //    PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
    //    foreach (PlayerHealth player in players) 
    //    {
    //        if (player.IsAlive) continue; // Só respawnar os mortos
    //        Transform spawnPoint = player.transform.parent; // Assume que o spawn point é o pai do jogador
    //        if (spawnPoint == null)
    //        {
    //            Debug.LogError($"[PlayerSpawner] No spawn point found for player {player.name}!");
    //            continue;
    //        }

    //        // Move o jogador para o spawn point e reseta HP
    //        player.transform.position = spawnPoint.position;
    //        player.transform.rotation = spawnPoint.rotation;
    //        player.NetCurrentHP.Value = player.MaxHP;
    //        player.NetIsAlive.Value = true;
    //        Debug.Log($"[PlayerSpawner] Respawned player {player.name} at {spawnPoint.position}");

            

    //    }
    //}


}