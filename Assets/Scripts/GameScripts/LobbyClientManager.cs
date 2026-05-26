using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Persiste entre cenas via DontDestroyOnLoad.
/// Envia ServerRpcs e recebe os callbacks do LobbyServerManager.
/// 
/// Setup:
///   - 1 GameObject "LobbyClientManager" na primeira cena de rede (ex: PlayMenu).
///   - Componentes: NetworkObject + este script.
///   - NÃO duplicar em outras cenas — sobrevive automaticamente.
/// </summary>
public class LobbyClientManager : NetworkBehaviour
{
    public static LobbyClientManager Instance { get; private set; }

    // Referência lazy ao servidor (encontrada uma vez e cached)
    private LobbyServerManager _server;
    private LobbyServerManager Server
    {
        get
        {
            if (_server == null)
                _server = FindFirstObjectByType<LobbyServerManager>();
            return _server;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsClient) { enabled = false; return; }

        if (Instance != null && Instance != this)
        {
            // Já existe um — destrói este duplicado (pode acontecer ao recarregar cenas)
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[LobbyClientManager] Pronto e persistente.");
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
            LobbySessionData.Clear();
        }
    }

    // ── API PÚBLICA ────────────────────────────────────────────────

    /// <summary>Chamado pelo CreateRoomUI ao clicar "Criar".</summary>
    public void CreateLobby(string roomName, bool isPublic, int maxPlayers)
    {
        if (!CheckConnected()) return;
        Server?.CreateLobbyServerRpc(roomName, isPublic, maxPlayers);
    }

    /// <summary>Chamado pelo JoinRoomUI ao confirmar o PIN.</summary>
    public void JoinLobby(string pin)
    {
        if (!CheckConnected()) return;
        if (string.IsNullOrWhiteSpace(pin))
        {
            OnError("Introduz um PIN válido.");
            return;
        }
        Server?.JoinLobbyServerRpc(pin.Trim());
    }

    /// <summary>Chamado pelo JoinRoomUI ao entrar na cena para pedir salas públicas.</summary>
    public void RequestPublicLobbies()
    {
        if (!CheckConnected()) return;
        Server?.RequestPublicLobbiesServerRpc();
    }

    /// <summary>Sai do lobby actual.</summary>
    public void LeaveLobby()
    {
        Server?.LeaveLobbyServerRpc();
        LobbySessionData.Clear();
    }

    // ── CALLBACKS (chamados pelo LobbyServerManager via ClientRpc) ──

    public void OnLobbyCreated(string pin, string roomName, bool isPublic, int maxPlayers)
    {
        LobbySessionData.Pin = pin;
        LobbySessionData.RoomName = roomName;
        LobbySessionData.IsPublic = isPublic;
        LobbySessionData.MaxPlayers = maxPlayers;
        LobbySessionData.CurrentPlayers = 1;
        LobbySessionData.IsCreator = true;

        Debug.Log($"[Client] Lobby criado: {pin} \"{roomName}\"");
        SceneManager.LoadScene("LobbyMenu");
    }

    public void OnLobbyJoined(string pin, string roomName, bool isPublic, int current, int max)
    {
        LobbySessionData.Pin = pin;
        LobbySessionData.RoomName = roomName;
        LobbySessionData.IsPublic = isPublic;
        LobbySessionData.MaxPlayers = max;
        LobbySessionData.CurrentPlayers = current;
        LobbySessionData.IsCreator = false;

        Debug.Log($"[Client] Entrei no lobby {pin} ({current}/{max})");
        SceneManager.LoadScene("LobbyMenu");
    }

    public void OnLobbyUpdated(string pin, int current, int max)
    {
        if (LobbySessionData.Pin != pin) return;
        LobbySessionData.CurrentPlayers = current;

        // Notifica a LobbyMenuUI se estiver na cena certa
        LobbyMenuUI.Instance?.RefreshPlayerCount(current, max);
    }

    public void OnError(string message)
    {
        Debug.LogWarning($"[Client] Erro do servidor: {message}");

        // Notifica qualquer UI activa que implemente ILobbyErrorReceiver
        // Em vez de depender de singletons específicos, usa um evento global simples
        OnServerError?.Invoke(message);
    }

    public void OnPublicLobbiesReceived(string data)
    {
        var list = ParsePublicLobbies(data);
        JoinRoomUI.Instance?.PopulatePublicLobbies(list);
    }

    /// <summary>Evento para qualquer UI receber erros do servidor.</summary>
    public static System.Action<string> OnServerError;

    // ── HELPERS ───────────────────────────────────────────────────

    private bool CheckConnected()
    {
        if (!IsSpawned || !IsClient)
        {
            Debug.LogWarning("[LobbyClientManager] Não está ligado ao servidor.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Parse do formato "PIN|Nome|actual|max;PIN|Nome|actual|max;..."
    /// </summary>
    private List<PublicLobbyEntry> ParsePublicLobbies(string data)
    {
        var result = new List<PublicLobbyEntry>();
        if (string.IsNullOrEmpty(data)) return result;

        foreach (string entry in data.Split(';'))
        {
            string[] parts = entry.Split('|');
            if (parts.Length < 4) continue;
            result.Add(new PublicLobbyEntry
            {
                Pin = parts[0],
                RoomName = parts[1],
                CurrentPlayers = int.TryParse(parts[2], out int c) ? c : 0,
                MaxPlayers = int.TryParse(parts[3], out int m) ? m : 0
            });
        }
        return result;
    }
}

/// <summary>Entrada na lista de lobbies públicos.</summary>
public class PublicLobbyEntry
{
    public string Pin;
    public string RoomName;
    public int CurrentPlayers;
    public int MaxPlayers;
}