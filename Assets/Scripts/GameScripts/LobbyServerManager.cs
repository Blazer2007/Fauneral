using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyServerManager : NetworkBehaviour
{
    public static LobbyServerManager Instance { get; private set; }

    [Header("Configuração")]
    [SerializeField] private int _pinLength = 4;

    [Tooltip("Se true, faz spawn automático quando o servidor inicia (requer que este prefab esteja na cena do servidor)")]
    [SerializeField] private bool _autoSpawnOnServer = true;

    private Dictionary<string, LobbyData> _lobbies = new();
    private Dictionary<ulong, string> _clientLobbyMap = new();

    public Canvas _publicLobbiesCanvas;
    public override void OnNetworkSpawn()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject); // Garante que o gerenciador do lobby não morra ao mudar de cena

        if (!IsServer)
        {
            Debug.Log("[LobbyServerManager] Referência de rede obtida pelo cliente.");
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        Debug.Log("[Server] LobbyServerManager em rede e pronto.");
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
            
            // If the server manager is being destroyed, we should probably clear all its rooms from discovery
            foreach(var pin in _lobbies.Keys)
            {
                Networking.DiscoveryManager.Instance?.DeleteRoom(pin);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateLobbyServerRpc(string roomName, bool isPublic, int maxPlayers, string forcedPin = "",
        ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;
        Debug.Log($"[Server] CreateLobbyServerRpc recebido de {client}. Pin: {forcedPin}");

        if (_clientLobbyMap.ContainsKey(client))
        {
            ErrorClientRpc("Já estás num lobby.", MakeTarget(client));
            return;
        }

        maxPlayers = Mathf.Clamp(maxPlayers, 2, 4);
        string pin = string.IsNullOrEmpty(forcedPin) ? GenerateUniquePin() : forcedPin;

        var lobby = new LobbyData
        {
            Pin = pin,
            RoomName = string.IsNullOrWhiteSpace(roomName) ? $"Sala {pin}" : roomName.Trim(),
            IsPublic = isPublic,
            MaxPlayers = maxPlayers,
            CreatorClientId = client
        };

        lobby.InitSlots();
        lobby.AssignSlot(client);

        _lobbies[pin] = lobby;
        _clientLobbyMap[client] = pin;

        Debug.Log($"[Server] Lobby registrado com sucesso: PIN={pin}. Enviando RPCs...");
        
        // Sincroniza a nova sala com o servidor de descoberta Node.js
        Networking.DiscoveryManager.Instance?.UpdateRoom(pin, 1);

        LobbyCreatedClientRpc(pin, lobby.RoomName, isPublic, maxPlayers, MakeTarget(client));
        BroadcastFullState(lobby);
    }

    [ServerRpc(RequireOwnership = false)]
    public void JoinLobbyServerRpc(string pin, ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;
        Debug.Log($"[Server] JoinLobbyServerRpc recebido de {client} para o Pin: {pin}");

        if (_clientLobbyMap.ContainsKey(client)) { ErrorClientRpc("Já estás num lobby.", MakeTarget(client)); return; }
        if (!_lobbies.TryGetValue(pin, out LobbyData lobby)) { ErrorClientRpc("PIN inválido.", MakeTarget(client)); return; }
        if (lobby.IsFull) { ErrorClientRpc("Lobby cheio.", MakeTarget(client)); return; }

        int slot = lobby.AssignSlot(client);
        _clientLobbyMap[client] = pin;

        Debug.Log($"[Server] Cliente {client} → slot {slot} em {pin}");
        
        // Atualiza a contagem de jogadores no Node.js
        Networking.DiscoveryManager.Instance?.UpdateRoom(pin, lobby.PlayerCount);

        LobbyJoinedClientRpc(pin, lobby.RoomName, lobby.IsPublic,
            lobby.PlayerCount, lobby.MaxPlayers, MakeTarget(client));

        BroadcastFullState(lobby);
    }

    [ServerRpc(RequireOwnership = false)]
    public void LeaveLobbyServerRpc(ServerRpcParams rpc = default)
        => RemoveClient(rpc.Receive.SenderClientId);

    [ServerRpc(RequireOwnership = false)]
    public void RequestPublicLobbiesServerRpc(ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in _lobbies)
        {
            LobbyData l = kvp.Value;
            if (!l.IsPublic || l.IsFull) continue;
            if (sb.Length > 0) sb.Append(";");
            sb.Append($"{l.Pin}|{l.RoomName}|{l.PlayerCount}|{l.MaxPlayers}");
        }
        PublicLobbiesClientRpc(sb.ToString(), MakeTarget(client));
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready, ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;
        if (!_clientLobbyMap.TryGetValue(client, out string pin)) return;
        if (!_lobbies.TryGetValue(pin, out LobbyData lobby)) return;

        lobby.SetReady(client, ready);
        BroadcastReadyState(lobby);

        if (lobby.AllReady())
            EnableStartGameClientRpc(MakeTarget(lobby.CreatorClientId));
        else
            DisableStartGameClientRpc(MakeTarget(lobby.CreatorClientId));
    }

    [ClientRpc]
    private void LobbyCreatedClientRpc(string pin, string roomName, bool isPublic,
        int maxPlayers, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnLobbyCreated(pin, roomName, isPublic, maxPlayers);

    [ClientRpc]
    private void LobbyJoinedClientRpc(string pin, string roomName, bool isPublic,
        int current, int max, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnLobbyJoined(pin, roomName, isPublic, current, max);

    [ClientRpc]
    private void FullStateClientRpc(string pin, string slotsData, string readyData,
        int currentPlayers, int maxPlayers, ulong creatorId, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnFullStateReceived(
               pin, slotsData, readyData, currentPlayers, maxPlayers, creatorId);

    [ClientRpc]
    private void ReadyStateClientRpc(string pin, string readyData, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnReadyStateReceived(pin, readyData);

    [ClientRpc]
    private void EnableStartGameClientRpc(ClientRpcParams p = default)
        => LobbyMenuUI.Instance?.SetStartGameVisible(true);

    [ClientRpc]
    private void DisableStartGameClientRpc(ClientRpcParams p = default)
        => LobbyMenuUI.Instance?.SetStartGameVisible(false);

    [ClientRpc]
    private void ErrorClientRpc(string message, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnError(message);

    [ClientRpc]
    private void PublicLobbiesClientRpc(string data, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnPublicLobbiesReceived(data);

    private void BroadcastFullState(LobbyData lobby)
    {
        string slots = lobby.SerializeSlots();
        string ready = lobby.SerializeReady();
        foreach (ulong id in lobby.Clients)
            FullStateClientRpc(lobby.Pin, slots, ready,
                lobby.PlayerCount, lobby.MaxPlayers, lobby.CreatorClientId, MakeTarget(id));
    }

    private void BroadcastReadyState(LobbyData lobby)
    {
        string ready = lobby.SerializeReady();
        foreach (ulong id in lobby.Clients)
            ReadyStateClientRpc(lobby.Pin, ready, MakeTarget(id));
    }

    private void RemoveClient(ulong clientId)
    {
        if (!_clientLobbyMap.TryGetValue(clientId, out string pin)) return;
        _clientLobbyMap.Remove(clientId);
        if (!_lobbies.TryGetValue(pin, out LobbyData lobby)) return;

        lobby.ReleaseSlot(clientId);

        if (lobby.Clients.Count == 0)
        {
            _lobbies.Remove(pin);
            Debug.Log($"[Server] Lobby {pin} destruído");
            Networking.DiscoveryManager.Instance?.DeleteRoom(pin);
        }
        else
        {
            Networking.DiscoveryManager.Instance?.UpdateRoom(pin, lobby.PlayerCount);
            BroadcastFullState(lobby);
            if (lobby.AllReady())
                EnableStartGameClientRpc(MakeTarget(lobby.CreatorClientId));
            else
                DisableStartGameClientRpc(MakeTarget(lobby.CreatorClientId));
        }
    }

    private void HandleDisconnect(ulong clientId) => RemoveClient(clientId);

    private string GenerateUniquePin()
    {
        string pin; int tries = 0;
        do { pin = ""; for (int i = 0; i < _pinLength; i++) pin += Random.Range(0, 10); }
        while (_lobbies.ContainsKey(pin) && ++tries < 1000);
        return pin;
    }

    private static ClientRpcParams MakeTarget(ulong id) =>
        new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { id } } };
}