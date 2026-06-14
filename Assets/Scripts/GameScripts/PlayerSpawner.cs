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
        Debug.Log($"[PlayerSpawner] Start — NetworkManager existe: {NetworkManager.Singleton != null}");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[PlayerSpawner] NetworkManager.Singleton é null no Start!");
            return;
        }

        // Subscreve ao evento de conexão do NetworkManager para esperar pela rede iniciar
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        Debug.Log("[PlayerSpawner] Subscrito ao OnServerStarted");
    }

    private void OnServerStarted()
    {
        Debug.Log("[PlayerSpawner] OnServerStarted chamado — a subscrever OnLoadEventCompleted");
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
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
        Debug.Log($"[PlayerSpawner] OnSceneLoadCompleted — cena: {sceneName}, IsServer: {NetworkManager.Singleton.IsServer}");

        if (!NetworkManager.Singleton.IsServer) return;
        if (sceneName != "GameScene") return;

        Debug.Log($"[PlayerSpawner] A spawnar {clientsCompleted.Count} jogadores");

        GameObject spawnPointsParent = GameObject.Find("PlayerSpawnPoints");
        if (spawnPointsParent == null)
        {
            Debug.LogError("[PlayerSpawner] 'PlayerSpawnPoints' não encontrado na GameScene!");
            return;
        }

        List<Transform> points = new List<Transform>();
        foreach (Transform t in spawnPointsParent.GetComponentsInChildren<Transform>())
            if (t != spawnPointsParent.transform) points.Add(t);

        Debug.Log($"[PlayerSpawner] Encontrados {points.Count} spawn points");

        int i = 0;
        foreach (ulong clientId in clientsCompleted)
        {
            Transform point = points[i % points.Count];
            var player = Instantiate(_playerPrefab, point.position, point.rotation);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            Debug.Log($"[PlayerSpawner] Spawned cliente {clientId} no ponto {i}");
            i++;
        }
    }
}