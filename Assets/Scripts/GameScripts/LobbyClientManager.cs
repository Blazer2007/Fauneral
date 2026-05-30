using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Persiste entre cenas via DontDestroyOnLoad.
/// Envia ServerRpcs e recebe callbacks do LobbyServerManager.
/// </summary>
public class LobbyClientManager : NetworkBehaviour
{
    public static LobbyClientManager Instance { get; private set; }

    private LobbyServerManager _server;
    private LobbyServerManager Server
    {
        get { if (_server == null) _server = FindFirstObjectByType<LobbyServerManager>(); return _server; }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsClient) { enabled = false; return; }
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[LobbyClientManager] Pronto e persistente.");
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) { Instance = null; LobbySessionData.Clear(); }
    }

    // ── API PÚBLICA ────────────────────────────────────────────────

    public void CreateLobby(string roomName, bool isPublic, int maxPlayers)
    {
        if (!CheckConnected()) return;
        Server?.CreateLobbyServerRpc(roomName, isPublic, maxPlayers);
    }

    public void JoinLobby(string pin)
    {
        if (!CheckConnected()) return;
        if (string.IsNullOrWhiteSpace(pin)) { OnError("Introduz um PIN válido."); return; }
        Server?.JoinLobbyServerRpc(pin.Trim());
    }

    public void RequestPublicLobbies()
    {
        if (!CheckConnected()) return;
        Server?.RequestPublicLobbiesServerRpc();
    }

    public void LeaveLobby()
    {
        Server?.LeaveLobbyServerRpc();
        LobbySessionData.Clear();
    }

    /// <summary>
    /// Chamado pelo botão Ready de cada PlayerSpawn.
    /// toggles o estado de pronto do cliente local.
    /// </summary>
    public void SetReady(bool ready)
    {
        if (!CheckConnected()) return;
        Server?.SetReadyServerRpc(ready);
    }

    // ── CALLBACKS DO SERVIDOR ──────────────────────────────────────

    public void OnLobbyCreated(string pin, string roomName, bool isPublic, int maxPlayers)
    {
        LobbySessionData.Pin = pin;
        LobbySessionData.RoomName = roomName;
        LobbySessionData.IsPublic = isPublic;
        LobbySessionData.MaxPlayers = maxPlayers;
        LobbySessionData.CurrentPlayers = 1;
        LobbySessionData.IsCreator = true;
        LobbySessionData.MyClientId = NetworkManager.Singleton.LocalClientId;

        Debug.Log($"[Client] Lobby criado: {pin}");
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
        LobbySessionData.MyClientId = NetworkManager.Singleton.LocalClientId;

        Debug.Log($"[Client] Entrei no lobby {pin} ({current}/{max})");
        SceneManager.LoadScene("LobbyMenu");
    }

    /// <summary>
    /// Recebido sempre que alguém entra ou sai — traz slots + ready states completos.
    /// </summary>
    public void OnFullStateReceived(string pin, string slotsData, string readyData,
        int currentPlayers, int maxPlayers, ulong creatorId)
    {
        if (LobbySessionData.Pin != pin) return;

        LobbySessionData.CurrentPlayers = currentPlayers;
        LobbySessionData.MaxPlayers = maxPlayers;
        LobbySessionData.SlotsData = slotsData;
        LobbySessionData.ReadyData = readyData;
        LobbySessionData.CreatorId = creatorId;

        LobbyMenuUI.Instance?.RefreshFullState(slotsData, readyData, currentPlayers, maxPlayers);
    }

    /// <summary>
    /// Recebido sempre que alguém muda o estado de pronto — só ready states, slots não mudam.
    /// </summary>
    public void OnReadyStateReceived(string pin, string readyData)
    {
        if (LobbySessionData.Pin != pin) return;

        LobbySessionData.ReadyData = readyData;
        LobbyMenuUI.Instance?.RefreshReadyStates(readyData);
    }

    public void OnError(string message)
    {
        Debug.LogWarning($"[Client] Erro: {message}");
        OnServerError?.Invoke(message);
    }

    public void OnPublicLobbiesReceived(string data)
    {
        var list = ParsePublicLobbies(data);
        JoinRoomUI.Instance?.PopulatePublicLobbies(list);
    }

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

public class PublicLobbyEntry
{
    public string Pin;
    public string RoomName;
    public int CurrentPlayers;
    public int MaxPlayers;
}