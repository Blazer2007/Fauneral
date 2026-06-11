using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Distingue servidor de cliente de duas formas (por prioridade):
///   1. Command line argument "-server" (builds standalone e Multiplayer Playmode moderno)
///   2. Tag "Server" no GameObject (Multiplayer Playmode antigo sem suporte a argumentos)
///
/// No Multiplayer Playmode antigo:
///   Perfil "Server"  → Tag do perfil: "Server"   (criar esta tag em Unity: Edit > Tags)
///   Perfis "Client1", "Client2" → Tag: "None"
///
/// No Multiplayer Playmode moderno / builds:
///   Perfil servidor  → Additional Arguments: -server
/// </summary>
public class NetworkManagerUI : MonoBehaviour
{
    [Header("Prefab do LobbyServerManager (só usado pelo servidor)")]
    [SerializeField] private GameObject _lobbyManagerPrefab;

    [Header("Ligação")]
    [SerializeField] private string _serverAddress = "127.0.0.1";
    [SerializeField] private ushort _serverPort = 7777;

    [Header("Fallback para Multiplayer Playmode antigo")]
    [Tooltip("Tag do perfil que deve correr como servidor. Criar em Edit > Tags and Layers.")]
    [SerializeField] private string _serverTag = "Server";

    private void Start()
    {
        SubscribeToServerStarted();

        if (ShouldBeServer())
            StartDedicatedServer();
    }

    private void OnEnable()
    {
        SubscribeToServerStarted();
    }

    private void SubscribeToServerStarted()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    // ── DETECÇÃO ──────────────────────────────────────────────────

    private bool ShouldBeServer()
    {
        // Prioridade 1: argumento de linha de comandos
        foreach (string arg in System.Environment.GetCommandLineArgs())
            if (arg.ToLower() == "-server") return true;

        // Prioridade 2: tag do GameObject (Multiplayer Playmode antigo)
        if (!string.IsNullOrEmpty(_serverTag) && gameObject.CompareTag(_serverTag))
            return true;

        return false;
    }

    // ── SERVIDOR / CLIENTE ────────────────────────────────────────

    public void StartDedicatedServer()
    {
        ConfigureTransport();
        NetworkManager.Singleton.StartServer();
        Debug.Log($"[NetworkManagerUI] Servidor na porta {_serverPort}.");
    }

    public void StartClient()
    {
        ConfigureTransport();
        NetworkManager.Singleton.StartClient();
        Debug.Log($"[NetworkManagerUI] Cliente a ligar a {_serverAddress}:{_serverPort}");
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
    }

    // ── HELPERS ───────────────────────────────────────────────────

    private void ConfigureTransport()
    {
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
            transport.SetConnectionData(_serverAddress, _serverPort);
    }

    private void OnServerStarted()
    {
        // Only the server/host should spawn the lobby manager
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log("[NetworkManagerUI] Servidor iniciado. Verificando LobbyServerManager...");

        if (_lobbyManagerPrefab == null)
        {
            Debug.LogError("[Server] _lobbyManagerPrefab não atribuído!");
            return;
        }

        // Check if already exists to avoid double spawn
        if (LobbyServerManager.Instance != null)
        {
            Debug.Log("[Server] LobbyServerManager já existe, pulando spawn.");
            return;
        }

        GameObject go = Instantiate(_lobbyManagerPrefab);
        go.GetComponent<NetworkObject>().Spawn();
        Debug.Log("[Server] LobbyServerManager instanciado e spawnado na rede.");
    }
}