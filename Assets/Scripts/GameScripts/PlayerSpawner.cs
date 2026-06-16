using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }
    [SerializeField] private GameObject _playerPrefab;
    // Remove o _spawnPoints do Inspector � vamos buscar da cena dinamicamente

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[PlayerSpawner] Awake chamado");
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode mode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        
        // Use EndsWith to be safe with full paths
        if (!sceneName.EndsWith("GameScene")) return;

        if (!NetworkManager.Singleton.IsServer) return;
        
        // Compara ignorando maiúsculas/minúsculas e possivelmente caminhos
        if (!sceneName.EndsWith("GameScene"))
        {
            Debug.LogError("[PlayerSpawner] N�o encontrei 'PlayerSpawnPoints' na GameScene!");
            return;
        }

        Transform[] spawnPoints = spawnPointsParent.GetComponentsInChildren<Transform>();
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
        }
    }
}