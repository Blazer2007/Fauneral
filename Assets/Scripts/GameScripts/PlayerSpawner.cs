using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }
    [SerializeField] private GameObject _playerPrefab;

    // Lista de jogadores spawnados, acessível pelo RoundManager
    public List<PlayerHealth> SpawnedPlayers { get; private set; } = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton?.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton?.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode mode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (!sceneName.EndsWith("GameScene")) return;

        List<Transform> points = GetSpawnPoints();
        if (points.Count == 0) { Debug.LogError("[PlayerSpawner] Sem spawn points!"); return; }

        GameUI gameUI = FindFirstObjectByType<GameUI>();
        if (gameUI == null) { Debug.LogError("[PlayerSpawner] GameUI não encontrado!"); return; }

        SpawnedPlayers.Clear();
        int i = 0;
        foreach (ulong clientId in clientsCompleted)
        {
            Transform point = points[i % points.Count];
            var player = Instantiate(_playerPrefab, point.position, point.rotation);
            var health = player.GetComponent<PlayerHealth>();
            var netObj = player.GetComponent<NetworkObject>();

            if (health != null) health.SetPlayerIndex(i);
            if (netObj != null) netObj.SpawnAsPlayerObject(clientId, true);

            SpawnedPlayers.Add(health);
            i++;
        }

        // Inicializa barras depois de todos spawnados
        gameUI.InitialiseHPBarsClientRpc();
    }

    public List<Transform> GetSpawnPoints()
    {
        GameObject parent = GameObject.FindGameObjectWithTag("PlayerSpawnPoints");
        var points = new List<Transform>();
        if (parent == null) return points;
        foreach (Transform t in parent.GetComponentsInChildren<Transform>())
            if (t != parent.transform) points.Add(t);
        return points;
    }
}