using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [SerializeField] private GameObject _playerPrefab;
    // Remove o _spawnPoints do Inspector — vamos buscar da cena dinamicamente

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode mode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (sceneName != "GameScene") return;

        // Busca os spawn points do GameObject "PlayerSpawnPoints" na GameScene
        GameObject spawnPointsParent = GameObject.Find("PlayerSpawnPoints");
        if (spawnPointsParent == null)
        {
            Debug.LogError("[PlayerSpawner] Não encontrei 'PlayerSpawnPoints' na GameScene!");
            return;
        }

        Transform[] spawnPoints = spawnPointsParent.GetComponentsInChildren<Transform>();
        // GetComponentsInChildren inclui o próprio pai, por isso filtramos
        List<Transform> points = new List<Transform>();
        foreach (Transform t in spawnPoints)
            if (t != spawnPointsParent.transform) points.Add(t);

        if (points.Count == 0)
        {
            Debug.LogError("[PlayerSpawner] PlayerSpawnPoints não tem filhos!");
            return;
        }

        int spawnIndex = 0;
        foreach (ulong clientId in clientsCompleted)
        {
            Transform point = points[spawnIndex % points.Count];
            var player = Instantiate(_playerPrefab, point.position, point.rotation);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            Debug.Log($"[PlayerSpawner] Spawned cliente {clientId} no ponto {spawnIndex}");
            spawnIndex++;
        }
    }
}