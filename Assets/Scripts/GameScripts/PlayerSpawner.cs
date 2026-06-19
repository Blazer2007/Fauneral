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
        Debug.Log("[PlayerSpawner] Awake");
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

        if (!sceneName.EndsWith("GameScene")) return;

        GameObject spawnPointsContainer = GameObject.FindGameObjectWithTag("PlayerSpawnPoints");
        if (spawnPointsContainer == null) spawnPointsContainer = GameObject.Find("PlayerSpawnPoints");

        if (spawnPointsContainer == null)
        {
            Debug.LogError("[PlayerSpawner] PlayerSpawnPoints não encontrado!");
            return;
        }

        List<Transform> points = new List<Transform>();
        foreach (Transform t in spawnPointsContainer.GetComponentsInChildren<Transform>())
            if (t != spawnPointsContainer.transform) points.Add(t);

        if (points.Count == 0)
        {
            Debug.LogError("[PlayerSpawner] Nenhum spawn point válido encontrado!");
            return;
        }

        Debug.Log($"[PlayerSpawner] Spawning {clientsCompleted.Count} players.");

        GameUI gameUI = GameObject.FindFirstObjectByType<GameUI>(); 

        int i = 0;
        foreach (ulong clientId in clientsCompleted)
        {
            Transform point = points[i % points.Count];
            var player = Instantiate(_playerPrefab, point.position, point.rotation);
            
            var health = player.GetComponent<PlayerHealth>();
            if (health != null) health.SetPlayerIndex(i);

            var netObj = player.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.SpawnAsPlayerObject(clientId, true);
            
            i++;
        }

        gameUI?.InitialiseHPBarsClientRpc();
    }
}