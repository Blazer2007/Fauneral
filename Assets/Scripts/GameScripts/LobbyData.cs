using System.Collections.Generic;

/// <summary>
/// Dados de um lobby. Só existe no servidor.
/// </summary>
public class LobbyData
{
    public string Pin;
    public string RoomName;
    public bool IsPublic;
    public int MaxPlayers;
    public List<ulong> Clients = new List<ulong>();

    public bool IsFull => Clients.Count >= MaxPlayers;
    public int PlayerCount => Clients.Count;
}