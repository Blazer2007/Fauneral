using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Persiste entre cenas via DontDestroyOnLoad.
/// Usa LobbyServerManager.Instance para chamar RPCs —
/// funciona porque o servidor faz Spawn do LobbyServerManager como NetworkObject,
/// tornando-o visível em todos os clientes.
/// </summary>
public class LobbyClientManager : NetworkBehaviour
{
    public static LobbyClientManager Instance { get; private set; }

    // Atalho local para não repetir LobbyServerManager.Instance em todo o lado
    private static LobbyServerManager Server => LobbyServerManager.Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[LobbyClientManager] Destruindo duplicata.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[LobbyClientManager] Instância configurada e persistente.");
    }

    public override void OnNetworkSpawn()
    {
        if (!IsClient) { enabled = false; return; }
        Instance = this; 
        Debug.Log($"[LobbyClientManager] Spawnado na rede. ClientId: {NetworkManager.Singleton.LocalClientId}");
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) { Instance = null; LobbySessionData.Clear(); }
    }

    // ── API PÚBLICA ────────────────────────────────────────────────

    public async void CreateLobby(string roomName, bool isPublic, int maxPlayers, string forcedPin = "")
    {
        Debug.Log($"[LobbyClientManager] Preparando criação de lobby. PIN: {forcedPin}. IsSpawned: {IsSpawned}");
        if (!CheckConnected()) return;

        float waitTime = 0;
        while (Server == null && waitTime < 10f) // Aumentado para 10s para conexões lentas
        {
            Debug.Log("[LobbyClientManager] Aguardando LobbyServerManager aparecer na rede...");
            await System.Threading.Tasks.Task.Delay(250);
            waitTime += 0.25f;
        }

        if (Server == null) 
        { 
            Debug.LogError("[LobbyClientManager] ERRO: LobbyServerManager não encontrado no servidor! RPC não enviado."); 
            return; 
        }
        
        Debug.Log($"[LobbyClientManager] Enviando CreateLobbyServerRpc para o servidor. Nome: {roomName}");
        Server.CreateLobbyServerRpc(roomName, isPublic, maxPlayers, forcedPin);
    }

    public async void JoinLobby(string pin)
    {
        Debug.Log($"[LobbyClientManager] Preparando entrada no lobby: {pin}. IsSpawned: {IsSpawned}");
        if (!CheckConnected()) return;

        float waitTime = 0;
        while (Server == null && waitTime < 10f) // Aumentado para 10s
        {
            Debug.Log("[LobbyClientManager] Aguardando LobbyServerManager aparecer na rede...");
            await System.Threading.Tasks.Task.Delay(250);
            waitTime += 0.25f;
        }

        if (Server == null) 
        { 
            Debug.LogError("[LobbyClientManager] ERRO: LobbyServerManager não encontrado! Verifique a conexão com o Host."); 
            return; 
        }

        Debug.Log($"[LobbyClientManager] Enviando JoinLobbyServerRpc para PIN: {pin}");
        Server.JoinLobbyServerRpc(pin.Trim());
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

    public void SetReady(bool ready)
    {
        if (!CheckConnected()) return;
        Server?.SetReadyServerRpc(ready);
    }

    // ── CALLBACKS DO SERVIDOR ──────────────────────────────────────

    public void OnLobbyCreated(string pin, string roomName, bool isPublic, int maxPlayers)
    {
        Debug.Log($"[LobbyClientManager] SUCESSO: Lobby criado no servidor. PIN: {pin}. Mudando para LobbyMenu...");
        
        LobbySessionData.Pin = pin;
        LobbySessionData.RoomName = roomName;
        LobbySessionData.IsPublic = isPublic;
        LobbySessionData.MaxPlayers = maxPlayers;
        LobbySessionData.CurrentPlayers = 1;
        LobbySessionData.IsCreator = true;
        LobbySessionData.MyClientId = NetworkManager.Singleton.LocalClientId;

        SceneManager.LoadScene("LobbyMenu");
    }

    public void OnLobbyJoined(string pin, string roomName, bool isPublic, int current, int max)
    {
        Debug.Log($"[LobbyClientManager] SUCESSO: Entrei no lobby {pin}. Mudando para LobbyMenu...");

        LobbySessionData.Pin = pin;
        LobbySessionData.RoomName = roomName;
        LobbySessionData.IsPublic = isPublic;
        LobbySessionData.MaxPlayers = max;
        LobbySessionData.CurrentPlayers = current;
        LobbySessionData.IsCreator = false;
        LobbySessionData.MyClientId = NetworkManager.Singleton.LocalClientId;

        SceneManager.LoadScene("LobbyMenu");
    }

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
        JoinRoomUI.Instance?.PopulatePublicLobbies(ParsePublicLobbies(data));
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