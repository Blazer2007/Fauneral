using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gere o uso de cartas em rede.
/// O servidor aplica os StatModifiers via PlayerStats.ApplyAll().
///
/// Setup:
///   - GameObject "CardEffectManager" com NetworkObject + este script
///   - Inspector: _cardDatabase → asset CardDatabase
///   - Cada jogador precisa de ter PlayerStats no mesmo GameObject que PlayerHealth
/// </summary>
public class CardEffectManager : NetworkBehaviour
{
    public static CardEffectManager Instance { get; private set; }

    [SerializeField] private CardDatabase _cardDatabase;

    // clientId → PlayerStats (só no servidor)
    private Dictionary<ulong, PlayerStats> _playerMap = new();

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer) BuildPlayerMap();
    }

    // ── REGISTO ───────────────────────────────────────────────────

    private void BuildPlayerMap()
    {
        _playerMap.Clear();
        foreach (var stats in FindObjectsByType<PlayerStats>(FindObjectsSortMode.None))
        {
            var netObj = stats.GetComponent<NetworkObject>();
            if (netObj != null)
                _playerMap[netObj.OwnerClientId] = stats;
        }
    }

    // ── API PÚBLICA ───────────────────────────────────────────────

    /// <summary>Chamado por CardDisplay.OnCardClicked(cardId).</summary>
    public void UseCard(int cardId)
    {
        if (!IsSpawned || !IsClient) return;
        UseCardServerRpc(cardId);
    }

    // ── SERVER RPC ────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void UseCardServerRpc(int cardId, ServerRpcParams rpc = default)
    {
        ulong senderId = rpc.Receive.SenderClientId;

        ScriptableCard card = _cardDatabase?.Get(cardId);
        if (card == null) { Debug.LogWarning($"[Server] cardId={cardId} inválido."); return; }

        if (!_playerMap.TryGetValue(senderId, out PlayerStats casterStats))
        {
            Debug.LogWarning($"[Server] PlayerStats não encontrado para {senderId}");
            return;
        }

        // Buffs → caster
        casterStats.ApplyAll(card.buffs, card.time);

        // Debuffs → todos os oponentes
        foreach (var kvp in _playerMap)
        {
            if (kvp.Key == senderId) continue;
            kvp.Value.ApplyAll(card.debuffs, card.time);
        }

        Debug.Log($"[Server] {senderId} usou '{card.name}'");

        // Notifica clientes para VFX / som
        CardUsedClientRpc(cardId, senderId);
    }

    // ── CLIENT RPC ────────────────────────────────────────────────

    [ClientRpc]
    private void CardUsedClientRpc(int cardId, ulong casterId)
    {
        ScriptableCard card = _cardDatabase?.Get(cardId);
        if (card == null) return;

        bool isMine = casterId == NetworkManager.Singleton.LocalClientId;
        Debug.Log($"[Client] '{card.name}' usado por {(isMine ? "mim" : $"cliente {casterId}")}");

        // Aqui: animação, som, GameUI.ShowCardEffect(card, isMine)
    }
}
