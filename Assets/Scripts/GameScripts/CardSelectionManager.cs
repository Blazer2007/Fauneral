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
    [SerializeField] private CardDataBase _cardDatabase;
    [SerializeField] private RarityTable _rarityTable;
    [SerializeField] private GameObject _selectionCanvas;

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

    // clientId → número de cartas clicáveis que o jogador já adquiriu (limite 3)
    private Dictionary<ulong, int> _clickableCount = new();

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

        _currentRound = roundNumber;
        _lastWinnerId = winnerId;
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
        bool isDraw = (_lastWinnerId == ulong.MaxValue);
        bool isWinner = !isDraw && (clientId == _lastWinnerId);
        bool isFirstRound = (_currentRound <= 1);

        var offered = new List<int>();  // Índices das cartas oferecidas
        var used = new HashSet<int>();  // Índices já escolhidos (evita duplicados)

        // ── REGRA 1: a carta GARANTIDA (slot 0) ──────────────────────
        // Ronda 1 → sem Angel/Devil (não há vencedor/perdedor ainda).
        // Vencedor → 1 carta Devil garantida.
        // Perdedor → 1 carta Angel garantida.
        if (!isFirstRound)
        {
            CardDealType guaranteed = isWinner ? CardDealType.Devil : CardDealType.Angel;
            int guaranteedIdx = TryGetCardOfDeal(guaranteed, used);
            if (guaranteedIdx >= 0)
            {
                offered.Add(guaranteedIdx);
                used.Add(guaranteedIdx);
            }
        }

        // ── REGRA 2: o resto dos slots são cartas NEUTRAS ────────────
        int safety = 0;
        while (offered.Count < _offeredCardCount && safety < 500)
        {
            safety++;
            int idx = TryGetNeutralCard(clientId, used);
            if (idx < 0) break; // não há mais neutras disponíveis
            offered.Add(idx);
            used.Add(idx);
        }

        // ── FALLBACK: completa com qualquer carta neutra restante ────
        if (offered.Count < _offeredCardCount)
        {
            for (int i = 0; i < _cardDatabase.Cards.Length && offered.Count < _offeredCardCount; i++)
            {
                var c = _cardDatabase.Cards[i];
                if (c != null && c.dealType == CardDealType.Neutral && !used.Contains(i))
                {
                    offered.Add(i);
                    used.Add(i);
                }
            }
        }

        return offered.ToArray();
    }

    /// <summary>Sorteia uma carta de um dado DealType (Angel/Devil), respeitando raridade pela ronda.</summary>
    private int TryGetCardOfDeal(CardDealType deal, HashSet<int> exclude)
    {
        if (_cardDatabase == null || _cardDatabase.Cards.Length == 0) return -1;

        string targetRarity = _rarityTable != null ? _rarityTable.RollRarity(_currentRound) : null;

        // 1ª tentativa: deal + raridade sorteada
        var candidates = new List<int>();
        for (int i = 0; i < _cardDatabase.Cards.Length; i++)
        {
            var card = _cardDatabase.Cards[i];
            if (card == null || exclude.Contains(i)) continue;
            if (card.dealType != deal) continue;
            if (targetRarity != null && card.rarity != targetRarity) continue;
            candidates.Add(i);
        }

        // 2ª tentativa: qualquer carta do deal (ignora raridade)
        if (candidates.Count == 0)
        {
            for (int i = 0; i < _cardDatabase.Cards.Length; i++)
            {
                var card = _cardDatabase.Cards[i];
                if (card == null || exclude.Contains(i)) continue;
                if (card.dealType == deal) candidates.Add(i);
            }
        }

        if (candidates.Count == 0) return -1;
        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// Sorteia uma carta neutra. Respeita o limite de 3 cartas clicáveis por jogador:
    /// se o jogador já tem 3 clicáveis, cartas clicáveis deixam de aparecer para ele.
    /// </summary>
    private int TryGetNeutralCard(ulong clientId, HashSet<int> exclude)
    {
        if (_cardDatabase == null || _cardDatabase.Cards.Length == 0) return -1;

        bool atClickableLimit = GetClickableCount(clientId) >= 3;
        string targetRarity = _rarityTable != null ? _rarityTable.RollRarity(_currentRound) : null;

        // 1ª tentativa: neutra + raridade sorteada (+ filtro de clicáveis)
        var candidates = new List<int>();
        for (int i = 0; i < _cardDatabase.Cards.Length; i++)
        {
            var card = _cardDatabase.Cards[i];
            if (card == null || exclude.Contains(i)) continue;
            if (card.dealType != CardDealType.Neutral) continue;
            if (atClickableLimit && card.isClickable) continue;
            if (targetRarity != null && card.rarity != targetRarity) continue;
            candidates.Add(i);
        }

        // 2ª tentativa: qualquer neutra (ignora raridade, mantém filtro de clicáveis)
        if (candidates.Count == 0)
        {
            for (int i = 0; i < _cardDatabase.Cards.Length; i++)
            {
                var card = _cardDatabase.Cards[i];
                if (card == null || exclude.Contains(i)) continue;
                if (card.dealType != CardDealType.Neutral) continue;
                if (atClickableLimit && card.isClickable) continue;
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0) return -1;
        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>Quantas cartas clicáveis o jogador já possui (rastreado no servidor).</summary>
    private int GetClickableCount(ulong clientId)
    {
        return _clickableCount.TryGetValue(clientId, out int n) ? n : 0;
    }

    // ── CLIENT RPC: mostra as cartas ao jogador ───────────────────

    [ClientRpc]
    private void SendCardOfferClientRpc(int[] cardIds, ClientRpcParams rpcParams = default)
    {
        ShowCanvas(cardIds);
    }

    // ── SERVER RPC: jogador confirmou a sua escolha ───────────────

    [ServerRpc(RequireOwnership = false)]
    private void ConfirmSelectionServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

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

        var stats = GetPlayerStats(senderId);

        if (card.isClickable)
        {
            // ── CARTA CLICÁVEL (activável com tecla) ──────────────────
            // Não aplica nada agora — vai para o inventário do jogador para uso em ronda.
            // Conta para o limite de 3 clicáveis.
            if (!_clickableCount.ContainsKey(senderId)) _clickableCount[senderId] = 0;
            _clickableCount[senderId]++;
            Debug.Log($"[CardSelection] Jogador {senderId} adquiriu carta CLICÁVEL '{card.id}' " +
                      $"({_clickableCount[senderId]}/3)"); 
            AddCardToPlayerClientRpc(cardId, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
            });

            Debug.Log($"[CardSelection] Jogador {senderId} adquiriu carta CLICÁVEL '{card.name}' " +
                      $"({_clickableCount[senderId]}/3)");
        }
        else if (stats != null)
        {
            // ── CARTA PASSIVA (buff permanente) ───────────────────────
            // Buffs → sempre aplicados permanentemente ao próprio jogador.
            stats.ApplyAll(card.buffs, 0f);

            // Deal with the Devil: aplica também os debuffs (o custo do pacto).
            // Angel / Neutral: sem custo — os debuffs são ignorados.
            if (card.dealType == CardDealType.Devil)
                stats.ApplyAll(card.debuffs, 0f);

            Debug.Log($"[CardSelection] Jogador {senderId} adquiriu carta PASSIVA '{card.name}' " +
                      $"(deal={card.dealType})");
        }

        _pendingChoices[senderId] = cardId;

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
        if (RoundManager.Instance != null)
            RoundManager.Instance.StartNextRound();
    }

    // ── CLIENT RPC: adiciona carta ao PlayerCardUser ──────────────

    [ClientRpc]
    private void AddCardToPlayerClientRpc(int cardId, ClientRpcParams rpcParams = default)
    {
        ScriptableCard card = _cardDatabase?.Get(cardId);
        if (card == null) return;

        // Encontra o PlayerCardUser do jogador local (dono do NetworkObject local).
        // NOTA: no host o servidor é dono de vários NetworkObjects, por isso não basta
        // verificar IsOwner — temos de confirmar que o objecto tem mesmo um PlayerCardUser.
        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (!netObj.IsOwner) continue;

            var cardUser = netObj.GetComponent<PlayerCardUser>();
            if (cardUser != null)
            {
                cardUser.AddCard(card);
                break;
            }
        }
    }

    // ── CLIENT RPC: fecha o canvas ────────────────────────────────

    [ClientRpc]
    private void HideCanvasClientRpc(ClientRpcParams rpcParams = default)
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
            _cardSlots[i].SetSelectionMode(true);
            _cardSlots[i].Refresh();
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

    /// <summary>
    /// Resolve uma carta pelo campo <see cref="ScriptableCard.id"/> usando a base de dados
    /// partilhada. Usado pelo PlayerCardUser no servidor, onde o dicionário local de cartas
    /// do jogador não está preenchido (as cartas só são adicionadas no cliente dono).
    /// </summary>
    public ScriptableCard ResolveCardById(int id)
    {
        if (_cardDatabase == null || _cardDatabase.Cards == null) return null;

        foreach (var card in _cardDatabase.Cards)
            if (card != null && card.id == id) return card;

        return null;
    }

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