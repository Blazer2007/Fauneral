using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Corre EXCLUSIVAMENTE no servidor dedicado.
/// Gere lobbies com slots, estados de pronto e botão StartGame.
/// </summary>
public class LobbyServerManager : NetworkBehaviour
{
    [Header("Configuração")]
    [SerializeField] private int _pinLength = 4;

    private Dictionary<string, LobbyData> _lobbies = new Dictionary<string, LobbyData>();
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

    // ═══════════════════════════════════════════════════════════════
    //  SERVER RPCs
    // ═══════════════════════════════════════════════════════════════

    [ServerRpc(RequireOwnership = false)]
    public void CreateLobbyServerRpc(string roomName, bool isPublic, int maxPlayers,
        ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;
        if (_clientLobbyMap.ContainsKey(client)) { ErrorClientRpc("Já estás num lobby.", MakeTarget(client)); return; }

        maxPlayers = Mathf.Clamp(maxPlayers, 2, 4);
        string pin = GenerateUniquePin();

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

        Debug.Log($"[Server] Lobby criado: PIN={pin} \"{lobby.RoomName}\"");

        LobbyCreatedClientRpc(pin, lobby.RoomName, isPublic, maxPlayers, MakeTarget(client));
        BroadcastFullState(lobby);
    }

    [ServerRpc(RequireOwnership = false)]
    public void JoinLobbyServerRpc(string pin, ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;
        if (_clientLobbyMap.ContainsKey(client)) { ErrorClientRpc("Já estás num lobby.", MakeTarget(client)); return; }
        if (!_lobbies.TryGetValue(pin, out LobbyData lobby)) { ErrorClientRpc("PIN inválido.", MakeTarget(client)); return; }
        if (lobby.IsFull) { ErrorClientRpc("Lobby cheio.", MakeTarget(client)); return; }

        int slot = lobby.AssignSlot(client);
        _clientLobbyMap[client] = pin;

        Debug.Log($"[Server] Cliente {client} → slot {slot} em {pin}");

        LobbyJoinedClientRpc(pin, lobby.RoomName, lobby.IsPublic,
            lobby.PlayerCount, lobby.MaxPlayers, MakeTarget(client));

        // Envia estado completo a TODOS (incluindo o novo)
        BroadcastFullState(lobby);
    }

    [ServerRpc(RequireOwnership = false)]
    public void LeaveLobbyServerRpc(ServerRpcParams rpc = default)
        => RemoveClient(rpc.Receive.SenderClientId);

    /// <summary>
    /// Devolve a lista de lobbies públicos ao cliente que pediu.
    /// </summary>
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

    /// <summary>
    /// Cliente comunica que está pronto (ready=true) ou cancela (ready=false).
    /// Após alterar, o servidor verifica se TODOS estão prontos e notifica o criador.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready, ServerRpcParams rpc = default)
    {
        ulong client = rpc.Receive.SenderClientId;
        if (!_clientLobbyMap.TryGetValue(client, out string pin)) return;
        if (!_lobbies.TryGetValue(pin, out LobbyData lobby)) return;

        lobby.SetReady(client, ready);
        Debug.Log($"[Server] Cliente {client} ready={ready} em {pin}");

        // Envia novo estado de prontos a todos
        BroadcastReadyState(lobby);

        // Se todos prontos, habilita StartGame só para o criador
        if (lobby.AllReady())
            EnableStartGameClientRpc(MakeTarget(lobby.CreatorClientId));
        else
            DisableStartGameClientRpc(MakeTarget(lobby.CreatorClientId));
    }

    // ═══════════════════════════════════════════════════════════════
    //  CLIENT RPCs
    // ═══════════════════════════════════════════════════════════════

    [ClientRpc]
    private void LobbyCreatedClientRpc(string pin, string roomName, bool isPublic,
        int maxPlayers, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnLobbyCreated(pin, roomName, isPublic, maxPlayers);

    [ClientRpc]
    private void LobbyJoinedClientRpc(string pin, string roomName, bool isPublic,
        int current, int max, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnLobbyJoined(pin, roomName, isPublic, current, max);

    /// <summary>
    /// Envia slots + estados de pronto a todos os membros.
    /// slotsData  = "cId0,cId1,..."   (ulong.MaxValue = vazio)
    /// readyData  = "true,false,..."
    /// creatorId  = clientId do criador (para saber quem mostra StartGame)
    /// </summary>
    [ClientRpc]
    private void FullStateClientRpc(string pin, string slotsData, string readyData,
        int currentPlayers, int maxPlayers, ulong creatorId, ClientRpcParams p = default)
        => LobbyClientManager.Instance?.OnFullStateReceived(
               pin, slotsData, readyData, currentPlayers, maxPlayers, creatorId);

    /// <summary>Só atualiza estados de pronto (sem mudar slots).</summary>
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

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Envia slots + ready states a todos os membros.</summary>
    private void BroadcastFullState(LobbyData lobby)
    {
        string slots = lobby.SerializeSlots();
        string ready = lobby.SerializeReady();
        foreach (ulong id in lobby.Clients)
            FullStateClientRpc(lobby.Pin, slots, ready,
                lobby.PlayerCount, lobby.MaxPlayers, lobby.CreatorClientId, MakeTarget(id));
    }

    /// <summary>Envia só os ready states a todos os membros.</summary>
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
        Debug.Log($"[Server] Cliente {clientId} saiu de {pin}");

        if (lobby.Clients.Count == 0)
        {
            _lobbies.Remove(pin);
            Debug.Log($"[Server] Lobby {pin} destruído");
        }
        else
        {
            BroadcastFullState(lobby);
            // Re-verifica prontos após saída (alguém que estava pronto saiu)
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