/// <summary>
/// Dados estáticos do lobby actual.
/// Por ser estático, sobrevive a mudanças de cena sem precisar de DontDestroyOnLoad.
/// 
/// A cena CreateRoom escreve aqui depois de receber o PIN do servidor.
/// A cena JoinRoom escreve aqui depois de entrar com sucesso.
/// A cena LobbyMenu lê daqui no Start() para mostrar a informação correcta.
/// </summary>
public static class LobbySessionData
{
    // ── Dados do lobby actual ──────────────────────────────────────
    public static string Pin { get; set; }
    public static string RoomName { get; set; }
    public static bool IsPublic { get; set; }
    public static int MaxPlayers { get; set; }
    public static int CurrentPlayers { get; set; }

    /// <summary>True se este cliente foi quem criou o lobby.</summary>
    public static bool IsCreator { get; set; }

    /// <summary>True enquanto está num lobby activo.</summary>
    public static bool IsInLobby => !string.IsNullOrEmpty(Pin);

    /// <summary>Limpa todos os dados (ao sair do lobby ou ao desligar).</summary>
    public static void Clear()
    {
        Pin = null;
        RoomName = null;
        IsPublic = false;
        MaxPlayers = 0;
        CurrentPlayers = 0;
        IsCreator = false;
    }
}