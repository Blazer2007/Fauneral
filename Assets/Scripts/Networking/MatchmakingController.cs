using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Networking
{
    public class MatchmakingController : MonoBehaviour
    {
        public static MatchmakingController Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject _lobbyManagerPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async Task EnsureInitialized()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        public async Task<string> StartHostOnline(int maxPlayers)
        {
            Debug.Log("[Matchmaking] Iniciando Host Online...");
            await EnsureInitialized();

            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[Matchmaking] Parando conexão anterior...");
                NetworkManager.Singleton.Shutdown();
                await Task.Delay(1000);
            }

            Debug.Log("[Matchmaking] Criando alocação Relay...");
            string relayJoinCode = await RelayManager.Instance.CreateRelay(maxPlayers);
            if (string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.LogError("[Matchmaking] Falha ao criar Relay.");
                return null;
            }

            Debug.Log($"[Matchmaking] Relay pronto: {relayJoinCode}. Iniciando Host Netcode...");
            bool hostStarted = NetworkManager.Singleton.StartHost();
            if (!hostStarted)
            {
                Debug.LogError("[Matchmaking] Falha ao iniciar Host Netcode.");
                return null;
            }

            // Força o spawn do LobbyServerManager se necessário
            if (LobbyServerManager.Instance == null)
            {
                if (_lobbyManagerPrefab != null)
                {
                    Debug.Log("[Matchmaking] Instanciando LobbyServerManager manualmente...");
                    GameObject go = Instantiate(_lobbyManagerPrefab);
                    go.GetComponent<NetworkObject>().Spawn();
                }
                else
                {
                    Debug.LogWarning("[Matchmaking] Prefab do LobbyManager não atribuído no MatchmakingController. Tentando esperar pelo spawn automático...");
                }
            }

            // Garante que o LobbyServerManager seja instanciado
            float timeout = 5f;
            while (LobbyServerManager.Instance == null && timeout > 0)
            {
                Debug.Log("[Matchmaking] Aguardando LobbyServerManager...");
                await Task.Delay(200);
                timeout -= 0.2f;
            }

            if (LobbyServerManager.Instance == null)
            {
                Debug.LogError("[Matchmaking] Erro Crítico: LobbyServerManager não apareceu! Verifique se o Prefab está atribuído.");
                return null;
            }

            Debug.Log("[Matchmaking] Registrando sala no servidor Node.js...");
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            DiscoveryManager.Instance.CreateRoom(relayJoinCode, (nodeJsCode) =>
            {
                tcs.SetResult(nodeJsCode);
            });

            string finalCode = await tcs.Task;
            Debug.Log($"[Matchmaking] Sala registrada! Código: {finalCode}");
            return finalCode;
        }

        public async Task<bool> StartClientOnline(string nodeJsCode)
        {
            Debug.Log($"[Matchmaking] Iniciando Cliente Online para o código: {nodeJsCode}...");
            await EnsureInitialized();

            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[Matchmaking] Parando conexão anterior...");
                NetworkManager.Singleton.Shutdown();
                await Task.Delay(1000);
            }

            Debug.Log("[Matchmaking] Buscando código Relay no Node.js...");
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            DiscoveryManager.Instance.JoinRoom(nodeJsCode, (relayJoinCode) =>
            {
                tcs.SetResult(relayJoinCode);
            });

            string relayJoinCode = await tcs.Task;
            if (string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.LogError("[Matchmaking] Código de sala inválido ou servidor offline.");
                return false;
            }

            Debug.Log($"[Matchmaking] Código Relay obtido: {relayJoinCode}. Conectando ao Relay...");
            bool relayJoined = await RelayManager.Instance.JoinRelay(relayJoinCode);
            if (!relayJoined)
            {
                Debug.LogError("[Matchmaking] Falha ao conectar ao Relay.");
                return false;
            }

            Debug.Log("[Matchmaking] Iniciando Cliente Netcode...");
            return NetworkManager.Singleton.StartClient();
        }
    }
}
