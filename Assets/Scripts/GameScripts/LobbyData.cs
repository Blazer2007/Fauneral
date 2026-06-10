using System.Collections.Generic;

/// <summary>
/// Dados de um lobby. Só existe no servidor.
/// Slots[i]      = clientId no slot i  (ulong.MaxValue = vazio)
/// ReadyStates[i]= true se o jogador no slot i clicou Ready
/// </summary>
public class LobbyData
{
    public string Pin;
    public string RoomName;
    public bool IsPublic;
    public int MaxPlayers;
    public ulong CreatorClientId;

    public List<ulong> Clients = new List<ulong>();
    public ulong[] Slots;
    public bool[] ReadyStates;

    public bool IsFull => Clients.Count >= MaxPlayers;
    public int PlayerCount => Clients.Count;

    public void InitSlots()
    {
        Slots = new ulong[MaxPlayers];
        ReadyStates = new bool[MaxPlayers];
        for (int i = 0; i < MaxPlayers; i++)
        {
            Slots[i] = ulong.MaxValue;
            ReadyStates[i] = false;
        }
    }

    /// <summary>Atribui o próximo slot livre. Devolve índice ou -1 se cheio.</summary>
    public int AssignSlot(ulong clientId)
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] == ulong.MaxValue)
            {
                Slots[i] = clientId;
                ReadyStates[i] = false;
                Clients.Add(clientId);
                return i;
            }
        }
        return -1;
    }

    /// <summary>Liberta o slot do cliente e limpa o estado de pronto.</summary>
    public int ReleaseSlot(ulong clientId)
    {
        Clients.Remove(clientId);
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] == clientId)
            {
                Slots[i] = ulong.MaxValue;
                ReadyStates[i] = false;
                return i;
            }
        }
        return -1;
    }

    /// <summary>Marca o slot do cliente como pronto/não pronto.</summary>
    public void SetReady(ulong clientId, bool ready)
    {
        for (int i = 0; i < Slots.Length; i++)
            if (Slots[i] == clientId) { ReadyStates[i] = ready; return; }
    }

    /// <summary>True se todos os jogadores presentes estiverem prontos (e há pelo menos 1).</summary>
    public bool AllReady()
    {
        if (Clients.Count == 0) return false;
        for (int i = 0; i < Slots.Length; i++)
            if (Slots[i] != ulong.MaxValue && !ReadyStates[i]) return false;
        return true;
    }

    /// <summary>"clientId0,clientId1,..." — ulong.MaxValue = vazio</summary>
    public string SerializeSlots() => string.Join(",", Slots);

    /// <summary>"true,false,..." — estado de pronto por slot</summary>
    public string SerializeReady() => string.Join(",", ReadyStates);
}