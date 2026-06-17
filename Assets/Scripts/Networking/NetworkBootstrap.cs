using UnityEngine;
using Unity.Netcode;

namespace Networking
{
    /// <summary>
    /// Ensures that the necessary networking systems (NetworkManager, Matchmaking, Lobby) 
    /// exist in the scene. This is useful for testing scenes directly without going through the Main Menu.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _networkManagerPrefab;
        [SerializeField] private GameObject _onlineSystemsPrefab;
        [SerializeField] private GameObject _lobbyClientPrefab;

        private void Awake()
        {
            // Ensure NetworkManager exists
            if (NetworkManager.Singleton == null)
            {
                if (_networkManagerPrefab != null)
                {
                    GameObject go = Instantiate(_networkManagerPrefab);
                    // Force persistence
                    DontDestroyOnLoad(go);
                    Debug.Log("[NetworkBootstrap] NetworkManager instanciado e persistente.");
                }
                else
                {
                    Debug.LogWarning("[NetworkBootstrap] NetworkManagerPrefab não atribuído!");
                }
            }
            else
            {
                // If it already exists, ensure it stays persistent
                DontDestroyOnLoad(NetworkManager.Singleton.gameObject);
            }

            // Ensure Matchmaking systems exist
            if (MatchmakingController.Instance == null)
            {
                if (_onlineSystemsPrefab != null)
                {
                    Instantiate(_onlineSystemsPrefab);
                    Debug.Log("[NetworkBootstrap] NetworkOnlineSystems instanciado.");
                }
                else
                {
                    Debug.LogWarning("[NetworkBootstrap] OnlineSystemsPrefab não atribuído!");
                }
            }

            // Ensure Lobby Client exists
            if (LobbyClientManager.Instance == null)
            {
                if (_lobbyClientPrefab != null)
                {
                    Instantiate(_lobbyClientPrefab);
                    Debug.Log("[NetworkBootstrap] LobbyClientManager instanciado.");
                }
                else
                {
                    Debug.LogWarning("[NetworkBootstrap] LobbyClientPrefab não atribuído!");
                }
            }
            
            // This object is only a temporary helper to spawn the singletons
            Destroy(gameObject);
        }
    }
}
