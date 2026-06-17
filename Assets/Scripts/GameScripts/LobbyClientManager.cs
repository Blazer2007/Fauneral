using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LobbyClientManager : MonoBehaviour
{
    public static LobbyClientManager Instance { get; private set; }
    private static LobbyServerManager Server => LobbyServerManager.Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public async void CreateLobby(string roomName, bool isPublic, int maxPlayers, string forcedPin = "")
    {
        float waitTime = 0;
        while ((NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) && waitTime < 5f) { await Task.Delay(100); waitTime += 0.1f; }
        if (Server == null) { Debug.LogError("[LobbyClientManager] ERRO: LobbyServerManager não encontrado!"); return; }
        Server.CreateLobbyServerRpc(roomName, isPublic, maxPlayers, forcedPin);
    }

    public async void JoinLobby(string pin)
    {
        float waitTime = 0;
        while (waitTime < 10f)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && Server != null) break;
            await Task.Delay(250); waitTime += 0.25f;
        }
        if (Server == null) { Debug.LogError("[LobbyClientManager] ERRO: LobbyServerManager não encontrado após 10s!"); return; }
        Server.JoinLobbyServerRpc(pin.Trim());
    }

    public void RequestPublicLobbies() => Server?.RequestPublicLobbiesServerRpc();
    public void LeaveLobby() { Server?.LeaveLobbyServerRpc(); LobbySessionData.Clear(); }
    public void SetReady(bool ready) => Server?.SetReadyServerRpc(ready);

    public void OnLobbyCreated(string pin, string roomName, bool isPublic, int maxPlayers)
    {
        LobbySessionData.Pin = pin; LobbySessionData.RoomName = roomName; LobbySessionData.IsPublic = isPublic;
        LobbySessionData.MaxPlayers = maxPlayers; LobbySessionData.CurrentPlayers = 1; LobbySessionData.IsCreator = true;
        LobbySessionData.MyClientId = NetworkManager.Singleton.LocalClientId;
        if (NetworkManager.Singleton.IsServer) NetworkManager.Singleton.SceneManager.LoadScene("LobbyMenu", LoadSceneMode.Single);
    }

    public void OnLobbyJoined(string pin, string roomName, bool isPublic, int current, int max)
    {
        LobbySessionData.Pin = pin; LobbySessionData.RoomName = roomName; LobbySessionData.IsPublic = isPublic;
        LobbySessionData.MaxPlayers = max; LobbySessionData.CurrentPlayers = current; LobbySessionData.IsCreator = false;
        LobbySessionData.MyClientId = NetworkManager.Singleton.LocalClientId;
    }

    public void OnFullStateReceived(string pin, string slotsData, string readyData, int currentPlayers, int maxPlayers, ulong creatorId)
    {
        if (LobbySessionData.Pin != pin) return;
        LobbySessionData.CurrentPlayers = currentPlayers; LobbySessionData.MaxPlayers = maxPlayers;
        LobbySessionData.SlotsData = slotsData; LobbySessionData.ReadyData = readyData; LobbySessionData.CreatorId = creatorId;
        LobbyMenuUI.Instance?.RefreshFullState(slotsData, readyData, currentPlayers, maxPlayers);
    }

    public void OnReadyStateReceived(string pin, string readyData)
    {
        if (LobbySessionData.Pin != pin) return;
        LobbySessionData.ReadyData = readyData;
        LobbyMenuUI.Instance?.RefreshReadyStates(readyData);
    }

    public void OnError(string message) => OnServerError?.Invoke(message);
    public void OnPublicLobbiesReceived(string data) => JoinRoomUI.Instance?.PopulatePublicLobbies(ParsePublicLobbies(data));
    public static System.Action<string> OnServerError;

    private List<PublicLobbyEntry> ParsePublicLobbies(string data)
    {
        var result = new List<PublicLobbyEntry>();
        if (string.IsNullOrEmpty(data)) return result;
        foreach (string entry in data.Split(';'))
        {
            string[] parts = entry.Split('|');
            if (parts.Length < 4) continue;
            result.Add(new PublicLobbyEntry { Pin = parts[0], RoomName = parts[1], CurrentPlayers = int.TryParse(parts[2], out int c) ? c : 0, MaxPlayers = int.TryParse(parts[3], out int m) ? m : 0 });
        }
        return result;
    }
}

public class PublicLobbyEntry { public string Pin; public string RoomName; public int CurrentPlayers; public int MaxPlayers; }
