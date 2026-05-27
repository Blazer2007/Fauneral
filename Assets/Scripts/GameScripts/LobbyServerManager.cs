using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Corre EXCLUSIVAMENTE no servidor dedicado.
/// Gere criação/entrada em lobbies.
/// Suporta nome de sala, modo público/privado e limite de jogadores.
///
/// Setup:
///   - GameObject "LobbyManager" com NetworkObject + este script.
///   - NetworkManager com StartServer() (ver NetworkManagerUI).
/// </summary>
public class LobbyServerManager : NetworkBehaviour
{
    [Header("Configuração")]
    [SerializeField] private int _pinLength = 4;

    // PIN → dados do lobby
    private Dictionary<string, LobbyData> _lobbies = new Dictionary<string, LobbyData>();
    // clientId → PIN do lobby onde está
    private Dictionary<ulong, string> _clientLobbyMap = new Dictionary<ulong, string>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { enabled = false; return; }
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        Debug.Log("[Server] LobbyServerManager pronto.");
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
    }

    // ── SERVER RPCs ────────────────────────────────────────────────

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
    public void CreateLobbyServerRpc(string roomName, bool isPublic, int maxPlayers,
        ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;

        if (_clientLobbyMap.ContainsKey(client))
        {
            ErrorClientRpc("Já estás num lobby.", MakeTarget(client));
            return;
        }

        maxPlayers = Mathf.Clamp(maxPlayers, 2, 16);
        string pin = GenerateUniquePin();

        var lobby = new LobbyData
        {
            Pin = pin,
            RoomName = string.IsNullOrWhiteSpace(roomName) ? $"Sala {pin}" : roomName.Trim(),
            IsPublic = isPublic,
            MaxPlayers = maxPlayers
        };
        lobby.Clients.Add(client);

        _lobbies[pin] = lobby;
        _clientLobbyMap[client] = pin;

        Debug.Log($"[Server] Lobby criado: PIN={pin} Nome=\"{lobby.RoomName}\" Público={isPublic}");

        LobbyCreatedClientRpc(pin, lobby.RoomName, isPublic, maxPlayers, MakeTarget(client));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void JoinLobbyServerRpc(string pin, ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;

        if (_clientLobbyMap.ContainsKey(client))
        {
            ErrorClientRpc("Já estás num lobby.", MakeTarget(client));
            return;
        }
        if (!_lobbies.TryGetValue(pin, out LobbyData lobby))
        {
            ErrorClientRpc("PIN inválido.", MakeTarget(client));
            return;
        }
        if (lobby.IsFull)
        {
            ErrorClientRpc("Lobby cheio.", MakeTarget(client));
            return;
        }

        lobby.Clients.Add(client);
        _clientLobbyMap[client] = pin;

        Debug.Log($"[Server] Cliente {client} entrou em {pin} ({lobby.PlayerCount}/{lobby.MaxPlayers})");

        LobbyJoinedClientRpc(pin, lobby.RoomName, lobby.IsPublic,
            lobby.PlayerCount, lobby.MaxPlayers, MakeTarget(client));

        BroadcastUpdate(lobby, exclude: client);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void LeaveLobbyServerRpc(ServerRpcParams rpc = default)
    {
        RemoveClient(rpc.Receive.SenderClientId);
    }

    /// <summary>
    /// Devolve a lista de lobbies públicos ao cliente que pediu.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestPublicLobbiesServerRpc(ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;

        // Serializa os lobbies públicos num formato simples: "PIN|Nome|actual|max"
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

    // ── CLIENT RPCs ────────────────────────────────────────────────

    [ClientRpc]
    private void LobbyCreatedClientRpc(string pin, string roomName, bool isPublic,
        int maxPlayers, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnLobbyCreated(pin, roomName, isPublic, maxPlayers);

    [ClientRpc]
    private void LobbyJoinedClientRpc(string pin, string roomName, bool isPublic,
        int current, int max, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnLobbyJoined(pin, roomName, isPublic, current, max);

    [ClientRpc]
    private void LobbyUpdatedClientRpc(string pin, int current, int max,
        ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnLobbyUpdated(pin, current, max);

    [ClientRpc]
    private void ErrorClientRpc(string message, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnError(message);

    [ClientRpc]
    private void PublicLobbiesClientRpc(string data, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnPublicLobbiesReceived(data);

    // ── HELPERS ────────────────────────────────────────────────────

    private void BroadcastUpdate(LobbyData lobby, ulong exclude)
    {
        foreach (ulong id in lobby.Clients)
        {
            if (id == exclude) continue;
            LobbyUpdatedClientRpc(lobby.Pin, lobby.PlayerCount, lobby.MaxPlayers, MakeTarget(id));
        }
    }

    private void RemoveClient(ulong clientId)
    {
        if (!_clientLobbyMap.TryGetValue(clientId, out string pin)) return;
        _clientLobbyMap.Remove(clientId);
        if (!_lobbies.TryGetValue(pin, out LobbyData lobby)) return;

        lobby.Clients.Remove(clientId);
        Debug.Log($"[Server] Cliente {clientId} saiu de {pin}");

        if (lobby.Clients.Count == 0)
        {
            _lobbies.Remove(pin);
            Debug.Log($"[Server] Lobby {pin} destruído");
        }
        else
        {
            BroadcastUpdate(lobby, ulong.MaxValue);
        }
    }

    private void HandleDisconnect(ulong clientId) => RemoveClient(clientId);

    private string GenerateUniquePin()
    {
        string pin;
        int tries = 0;
        do { pin = ""; for (int i = 0; i < _pinLength; i++) pin += Random.Range(0, 10); }
        while (_lobbies.ContainsKey(pin) && ++tries < 1000);
        return pin;
    }

    private static ClientRpcParams MakeTarget(ulong id) =>
        new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { id } } };
}