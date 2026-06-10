using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gere a fase de selecção de cartas entre rondas.
///
/// FLUXO:
///   1. RoundManager chama BeginSelection(roundNumber, lastWinnerClientId)
///   2. Servidor sorteia 4–6 cartas por jogador (respeitando raridade e DealType)
///   3. Servidor envia as cartas a cada cliente via ReceiveCardOfferClientRpc (targeted)
///   4. Cada cliente mostra o canvas e o jogador clica numa carta
///   5. Cliente envia ConfirmSelectionServerRpc(cardId)
///   6. Quando todos confirmaram, servidor aplica os efeitos e chama RoundManager.StartRound()
///
/// DEALS:
///   VENCEDOR → Deal with the Devil
///     Pool de cartas com raridade superior (effectiveRound+2).
///     Os DEBUFFS da carta escolhida são aplicados ao próprio vencedor.
///     Mais poder, mas com custo — é um pacto.
///
///   PERDEDOR → Deal with the Angel
///     Pool de cartas com raridade base.
///     Apenas os BUFFS são aplicados. Os debuffs são ignorados.
///     É uma graça — sem custo, mas sem o poder extra do diabo.
///
///   EMPATE → sem modificador, pool base, sem custo para ninguém.
/// </summary>
public class CardSelectionManager : NetworkBehaviour
{
    public static CardSelectionManager Instance { get; private set; }

    // ── INSPECTOR ─────────────────────────────────────────────────

    [Header("Referências")]
    [SerializeField] private CardDataBase  _cardDatabase;
    [SerializeField] private RarityTable   _rarityTable;
    [SerializeField] private GameObject    _selectionCanvas;

    [Tooltip("Arrasta aqui os GameObjects dos slots de carta (CardDisplay), em ordem")]
    [SerializeField] private List<CardDisplay> _cardSlots;

    [SerializeField] private RoundManager _roundManager;

    [Header("Configuração")]
    [Tooltip("Quantas cartas são oferecidas ao jogador em cada selecção")]
    [Range(3, 6)]
    [SerializeField] private int _offeredCardCount = 4;

    // ── ESTADO (servidor) ─────────────────────────────────────────

    // clientId → cardId que o jogador escolheu (-1 = ainda não escolheu)
    private Dictionary<ulong, int> _pendingChoices = new();

    // Número da ronda actual (para escalar raridade)
    private int _currentRound = 1;

    // ClientId do vencedor da última ronda (para Deal Angel/Devil)
    private ulong _lastWinnerId = ulong.MaxValue; // MaxValue = sem vencedor (draw)

    // ── UNITY ─────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        HideCanvas();
    }

    // ── API PÚBLICA (chamada pelo RoundManager) ───────────────────

    /// <summary>
    /// Inicia a fase de selecção. Chamado pelo RoundManager no servidor.
    /// winnerId = ulong.MaxValue se foi empate.
    /// </summary>
    public void BeginSelection(int roundNumber, ulong winnerId)
    {
        if (!IsServer) return;

        _currentRound  = roundNumber;
        _lastWinnerId  = winnerId;
        _pendingChoices.Clear();

        // Regista todos os clientes como "ainda não escolheram"
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            _pendingChoices[clientId] = -1;

        // Sorteia e envia cartas a cada jogador
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            int[] offeredIds = DrawCardsForPlayer(clientId);
            SendCardOfferClientRpc(offeredIds, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            });
        }
    }

    // ── SERVIDOR: sorteia cartas ──────────────────────────────────

    private int[] DrawCardsForPlayer(ulong clientId)
    {
        bool isWinner = (clientId == _lastWinnerId);
        bool isDraw   = (_lastWinnerId == ulong.MaxValue);

        // VENCEDOR → Deal with the Devil
        //   Pool de cartas mais raras/poderosas, mas os debuffs da carta são aplicados a si próprio.
        //   Representa o risco de um pacto: ganhas poder mas pagas um preço.
        //   Implementação: effectiveRound +2 (acede a cartas de raridade superior).
        //
        // PERDEDOR → Deal with the Angel
        //   Pool de cartas limpas (só buffs, sem custo). Raridade normal — é uma graça, não um pacto.
        //   Implementação: effectiveRound base, mas debuffs NÃO são aplicados a si próprio.
        //
        // EMPATE → sem modificador, pool base.
        int effectiveRound = _currentRound;
        if (isWinner && !isDraw) effectiveRound += 2; // Devil: acede a pool mais poderosa

        var offered = new List<int>();
        var used    = new HashSet<int>();
        int attempts = 0;

        while (offered.Count < _offeredCardCount && attempts < 200)
        {
            attempts++;
            string targetRarity = _rarityTable != null
                ? _rarityTable.RollRarity(effectiveRound)
                : "Common";

            int idx = TryGetCardOfRarity(targetRarity, used);
            if (idx >= 0)
            {
                offered.Add(idx);
                used.Add(idx);
            }
        }

        // Fallback: completa com cartas aleatórias se não houver suficientes de uma raridade
        int safetyAttempts = 0;
        while (offered.Count < _offeredCardCount && safetyAttempts < 200)
        {
            safetyAttempts++;
            int idx = Random.Range(0, _cardDatabase.Cards.Length);
            if (!used.Contains(idx))
            {
                offered.Add(idx);
                used.Add(idx);
            }
        }

        return offered.ToArray();
    }

    private int TryGetCardOfRarity(string rarity, HashSet<int> exclude)
    {
        if (_cardDatabase == null || _cardDatabase.Cards.Length == 0) return -1;

        // Junta todos os índices com a raridade pedida, excluindo os já usados
        var candidates = new List<int>();
        for (int i = 0; i < _cardDatabase.Cards.Length; i++)
        {
            var card = _cardDatabase.Cards[i];
            if (card != null && card.rarity == rarity && !exclude.Contains(i))
                candidates.Add(i);
        }

        if (candidates.Count == 0) return -1;
        return candidates[Random.Range(0, candidates.Count)];
    }

    // ── CLIENT RPC: mostra as cartas ao jogador ───────────────────

    [ClientRpc]
    private void SendCardOfferClientRpc(int[] cardIds, ClientRpcParams _ = default)
    {
        ShowCanvas(cardIds);
    }

    // ── SERVER RPC: jogador confirmou a sua escolha ───────────────

    [ServerRpc(RequireOwnership = false)]
    private void ConfirmSelectionServerRpc(int cardId, ServerRpcParams rpc = default)
    {
        ulong senderId = rpc.Receive.SenderClientId;

        if (!_pendingChoices.ContainsKey(senderId))
        {
            Debug.LogWarning($"[CardSelection] Cliente {senderId} não estava na lista de espera.");
            return;
        }

        ScriptableCard card = _cardDatabase?.Get(cardId);
        if (card == null)
        {
            Debug.LogWarning($"[CardSelection] cardId={cardId} inválido.");
            return;
        }

        // Aplica os buffs permanentemente ao próprio jogador (duration=0 → permanente em PlayerStats)
        var stats = GetPlayerStats(senderId);
        if (stats != null)
        {
            // Buffs → sempre aplicados ao próprio jogador
            stats.ApplyAll(card.buffs, 0f);

            // Deal with the Devil (vencedor): os debuffs da carta são aplicados a si próprio.
            // É o preço do pacto — ganhas cartas mais poderosas mas sofres as consequências.
            bool isWinner = (senderId == _lastWinnerId) && (_lastWinnerId != ulong.MaxValue);
            if (isWinner)
                stats.ApplyAll(card.debuffs, 0f);

            // Deal with the Angel (perdedor): sem penalidade — os debuffs NÃO são aplicados.
            // Os debuffs da carta continuam a existir no ScriptableCard mas são ignorados aqui.
        }

        _pendingChoices[senderId] = cardId;
        Debug.Log($"[CardSelection] Jogador {senderId} escolheu carta '{card.name}'");

        // Notifica o cliente para fechar o canvas
        HideCanvasClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
        });

        CheckAllPlayersChosen();
    }

    private void CheckAllPlayersChosen()
    {
        foreach (var kvp in _pendingChoices)
        {
            if (kvp.Value == -1) return; // Ainda há jogadores à espera
        }

        // Todos escolheram → inicia a ronda
        Debug.Log("[CardSelection] Todos os jogadores escolheram. A iniciar ronda...");
        _roundManager?.StartRound();
    }

    // ── CLIENT RPC: fecha o canvas ────────────────────────────────

    [ClientRpc]
    private void HideCanvasClientRpc(ClientRpcParams _ = default)
    {
        HideCanvas();
    }

    // ── HELPERS CLIENTE ───────────────────────────────────────────

    private void ShowCanvas(int[] cardIds)
    {
        if (_selectionCanvas != null)
            _selectionCanvas.SetActive(true);

        for (int i = 0; i < _cardSlots.Count; i++)
        {
            if (i >= cardIds.Length)
            {
                _cardSlots[i].gameObject.SetActive(false);
                continue;
            }

            var card = _cardDatabase?.Get(cardIds[i]);
            if (card == null) continue;

            _cardSlots[i].gameObject.SetActive(true);
            _cardSlots[i]._Card = card;
            _cardSlots[i].CardId = cardIds[i];

            // Chama Refresh via reflexão para não quebrar a API actual do CardDisplay
            var refreshMethod = typeof(CardDisplay).GetMethod("Refresh",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            refreshMethod?.Invoke(_cardSlots[i], null);
        }
    }

    private void HideCanvas()
    {
        if (_selectionCanvas != null)
            _selectionCanvas.SetActive(false);
    }

    /// <summary>
    /// Chamado pelo CardDisplay quando o jogador clica numa carta.
    /// Substitui o UseCard() do CardEffectManager para a fase de upgrade.
    /// </summary>
    public void PlayerSelectedCard(int cardId)
    {
        if (!IsSpawned || !IsClient) return;
        HideCanvas(); // Feedback imediato no cliente
        ConfirmSelectionServerRpc(cardId);
    }

    // ── HELPERS SERVIDOR ──────────────────────────────────────────

    private PlayerStats GetPlayerStats(ulong clientId)
    {
        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (netObj.OwnerClientId == clientId)
            {
                var stats = netObj.GetComponent<PlayerStats>();
                if (stats != null) return stats;
            }
        }
        return null;
    }
}