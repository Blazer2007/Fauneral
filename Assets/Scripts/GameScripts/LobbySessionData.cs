/// <summary>
/// Dados estáticos do lobby actual. Sobrevive a mudanças de cena.
/// </summary>
public static class LobbySessionData
{
    public static string Pin { get; set; }
    public static string RoomName { get; set; }
    public static bool IsPublic { get; set; }
    public static int MaxPlayers { get; set; }
    public static int CurrentPlayers { get; set; }
    public static bool IsCreator { get; set; }
    public static ulong MyClientId { get; set; }
    public static ulong CreatorId { get; set; }

    /// <summary>"clientId0,clientId1,..." — ulong.MaxValue = vazio</summary>
    public static string SlotsData { get; set; }

    /// <summary>"true,false,..." — estado de pronto por slot</summary>
    public static string ReadyData { get; set; }

    public static bool IsInLobby => !string.IsNullOrEmpty(Pin);

    public static void Clear()
    {
        Pin = null; RoomName = null; IsPublic = false;
        MaxPlayers = 0; CurrentPlayers = 0;
        IsCreator = false; MyClientId = 0; CreatorId = 0;
        SlotsData = null; ReadyData = null;
    }
}