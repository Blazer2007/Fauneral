using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }
    [SerializeField] private GameObject _playerPrefab;
    // Remove o _spawnPoints do Inspector � vamos buscar da cena dinamicamente
    private Transform spawnPointsParent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[PlayerSpawner] Awake chamado");
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        // Compara ignorando maiúsculas/minúsculas e possivelmente caminhos
        if (!sceneName.EndsWith("GameScene"))
        {
            Debug.LogError("[PlayerSpawner] N�o encontrei 'PlayerSpawnPoints' na GameScene!");
            return;
        }

        Transform[] spawnPoints = GameObject.FindGameObjectWithTag("PlayerSpawnPoints")?.GetComponentsInChildren<Transform>();

        // GetComponentsInChildren inclui o pr�prio pai, por isso filtramos
        List<Transform> points = new List<Transform>();
        foreach (Transform t in spawnPointsParent.GetComponentsInChildren<Transform>())
            if (t != spawnPointsParent.transform) points.Add(t);

        if (points.Count == 0)
        {
            Debug.LogError("[PlayerSpawner] PlayerSpawnPoints n�o tem filhos!");
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