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
        [SerializeField] private GameObject _lobbyManagerPrefab; // Referência ao prefab do sistema de lobby

        private void Awake()
        {
            // Padrão Singleton para acesso global e persistência entre cenas
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Garante que os serviços da Unity (Relay/Auth) estejam prontos
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

        // Fluxo para iniciar como Host Online (Relay + Node.js)
        public async Task<string> StartHostOnline(string roomName, bool isPublic, int maxPlayers)
        {
            Debug.Log("[Matchmaking] Iniciando Host Online...");
            await EnsureInitialized();

            // Limpa conexões ativas antes de começar uma nova
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[Matchmaking] Parando conexão anterior...");
                NetworkManager.Singleton.Shutdown();
                await Task.Delay(1000);
            }

            // 1. Cria alocação no Unity Relay
            Debug.Log("[Matchmaking] Criando alocação Relay...");
            string relayJoinCode = await RelayManager.Instance.CreateRelay(maxPlayers);
            if (string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.LogError("[Matchmaking] Falha ao criar Relay.");
                return null;
            }

            // 2. Inicia o Host no Netcode
            Debug.Log($"[Matchmaking] Relay pronto: {relayJoinCode}. Iniciando Host Netcode...");
            bool hostStarted = NetworkManager.Singleton.StartHost();
            if (!hostStarted)
            {
                Debug.LogError("[Matchmaking] Falha ao iniciar Host Netcode.");
                return null;
            }

            // 3. Força o spawn do LobbyServerManager para gerenciar os slots
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

            // Aguarda a sincronização do sistema de lobby
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

            // 4. Registra os detalhes da sala no servidor Node.js
            Debug.Log("[Matchmaking] Registrando sala no servidor Node.js...");
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            DiscoveryManager.Instance.CreateRoom(relayJoinCode, roomName, isPublic, maxPlayers, (nodeJsCode) =>
            {
                tcs.SetResult(nodeJsCode);
            });

            string finalCode = await tcs.Task;
            Debug.Log($"[Matchmaking] Sala registrada! Código: {finalCode}");
            return finalCode;
        }

        // Fluxo para conectar como Cliente Online usando o PIN
        public async Task<bool> StartClientOnline(string nodeJsCode)
        {
            Debug.Log($"[Matchmaking] Iniciando Cliente Online para o código: {nodeJsCode}...");
            await EnsureInitialized();

            // Limpa conexões antigas
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[Matchmaking] Parando conexão anterior...");
                NetworkManager.Singleton.Shutdown();
                await Task.Delay(1000);
            }

            // 1. Busca o IP (Código Relay) no Node.js através do PIN
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

            // 2. Conecta ao Relay
            Debug.Log($"[Matchmaking] Código Relay obtido: {relayJoinCode}. Conectando ao Relay...");
            bool relayJoined = await RelayManager.Instance.JoinRelay(relayJoinCode);
            if (!relayJoined)
            {
                Debug.LogError("[Matchmaking] Falha ao conectar ao Relay.");
                return false;
            }

            // 3. Inicia o Cliente no Netcode
            Debug.Log("[Matchmaking] Iniciando Cliente Netcode...");
            return NetworkManager.Singleton.StartClient();
        }
    }
}
