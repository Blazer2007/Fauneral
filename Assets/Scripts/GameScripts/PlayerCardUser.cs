using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gere o uso de cartas DURANTE uma ronda.
/// Vive no prefab do jogador, ao lado de PlayerStats e PlayerHealth.
///
/// SUPORTA DOIS TIPOS DE CARTA (via ScriptableCard):
///
///   1. ACESSÓRIO (accessoryPrefab != null)
///      Spawna um prefab na posição do jogador.
///      Ex: boogie bomb, mina, escudo físico.
///
///   2. EFEITO TEMPORÁRIO DE STATS (accessoryPrefab == null)
///      Aplica os buffs/debuffs da carta ao jogador durante card.time segundos.
///      Ex: burst de velocidade, invencibilidade temporária.
///
/// LÓGICA POR CARTA:
///   - Usos restantes: lidos de card.uses (decrementado por uso)
///   - Cooldown entre usos: lido de PlayerStats.CardCooldown (modificável por upgrades)
///   - Carta infinita: card.isinfinite = true ignora o contador de usos
///
/// SETUP:
///   - Adiciona este script ao prefab do jogador
///   - Liga _useCardAction ao Input Action "UseCard" (tecla à tua escolha)
///   - As cartas são adicionadas via AddCard() chamado pelo CardSelectionManager
///     após o jogador escolher um upgrade
/// </summary>
public class PlayerCardUser : NetworkBehaviour
{
    // ── INSPECTOR ─────────────────────────────────────────────────

    [Header("Input")]
    [Tooltip("Input Action para usar a carta activa (ex: tecla E ou Q)")]
    [SerializeField] private InputActionReference _useCardAction;

    [Header("Spawn")]
    [Tooltip("Offset relativamente ao jogador onde o acessório spawna")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.5f, 0f);

    // ── ESTADO ────────────────────────────────────────────────────

    // Cartas que o jogador tem disponíveis para usar durante a ronda
    // Chave: ScriptableCard, Valor: usos restantes (-1 = infinito)
    private Dictionary<ScriptableCard, int> _cards = new();

    // Carta actualmente seleccionada (a que é usada ao premir a tecla)
    private ScriptableCard _activeCard;

    // Cooldown actual (tempo que falta para poder usar outra vez)
    private float _cooldownRemaining = 0f;

    private PlayerStats _stats;

    // ── UNITY ─────────────────────────────────────────────────────

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    public override void OnNetworkSpawn()
    {
        // Só o dono do objecto gere o input
        if (!IsOwner) return;

        if (_useCardAction != null)
            _useCardAction.action.performed += OnUseCardInput;

        _useCardAction?.action.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (_useCardAction != null)
            _useCardAction.action.performed -= OnUseCardInput;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (_cooldownRemaining > 0f)
            _cooldownRemaining -= Time.deltaTime;
    }

    // ── INPUT ─────────────────────────────────────────────────────

    private void OnUseCardInput(InputAction.CallbackContext ctx)
    {
        if (_activeCard == null)
        {
            Debug.Log("[PlayerCardUser] Nenhuma carta activa.");
            return;
        }

        if (_cooldownRemaining > 0f)
        {
            Debug.Log($"[PlayerCardUser] Cooldown: {_cooldownRemaining:F1}s restantes.");
            return;
        }

        if (!HasUsesLeft(_activeCard))
        {
            Debug.Log($"[PlayerCardUser] Carta '{_activeCard.name}' sem usos.");
            return;
        }

        // Envia para o servidor executar o efeito
        UseCardServerRpc(_activeCard.cardID);
    }

    // ── SERVER RPC ────────────────────────────────────────────────

    [ServerRpc]
    private void UseCardServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Encontra a carta pelo ID
        ScriptableCard card = FindCardById(cardId);
        if (card == null)
        {
            Debug.LogWarning($"[PlayerCardUser] Servidor: cardId={cardId} não encontrado.");
            return;
        }

        // Decrementa usos no servidor
        ConsumeUseServerSide(senderId, card);

        if (card.accessoryPrefab != null)
        {
            // TIPO 1 — Spawna acessório
            SpawnAccessory(card, senderId);
        }
        else
        {
            // TIPO 2 — Efeito temporário de stats
            ApplyTemporaryEffectClientRpc(cardId, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
            });
        }

        // Notifica o cliente para iniciar o cooldown
        StartCooldownClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
        });
    }

    // ── SPAWN DE ACESSÓRIO (servidor) ─────────────────────────────

    private void SpawnAccessory(ScriptableCard card, ulong ownerId)
    {
        // Determina posição de spawn junto ao jogador dono
        Vector3 spawnPos = transform.position + _spawnOffset;

        var go = Instantiate(card.accessoryPrefab, spawnPos, Quaternion.identity);
        var netObj = go.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogWarning($"[PlayerCardUser] O prefab '{card.accessoryPrefab.name}' " +
                             "não tem NetworkObject — adiciona um para funcionar em rede.");
            Destroy(go);
            return;
        }

        netObj.Spawn();

        // Passa o ownerId ao acessório se ele tiver um componente que o aceite
        var accessory = go.GetComponent<AccessoryBase>();
        if (accessory != null)
            accessory.Init(ownerId, card);
    }

    // ── EFEITO TEMPORÁRIO DE STATS (cliente dono) ─────────────────

    [ClientRpc]
    private void ApplyTemporaryEffectClientRpc(int cardId, ClientRpcParams rpcParams = default)
    {
        ScriptableCard card = FindCardById(cardId);
        if (card == null) return;

        float duration = card.time > 0f ? card.time : 3f; // fallback 3s se time não definido

        _stats?.ApplyAll(card.buffs, duration);
        _stats?.ApplyAll(card.debuffs, duration);

        Debug.Log($"[PlayerCardUser] Efeito '{card.name}' activo por {duration}s.");
    }

    // ── COOLDOWN (cliente dono) ───────────────────────────────────

    [ClientRpc]
    private void StartCooldownClientRpc(ClientRpcParams rpcParams = default)
    {
        // Cooldown vem do PlayerStats — pode ser modificado por upgrades
        float cooldown = _stats != null ? _stats.CardCooldown : 3f;
        _cooldownRemaining = cooldown;

        Debug.Log($"[PlayerCardUser] Cooldown iniciado: {cooldown}s.");
    }

    // ── API PÚBLICA ───────────────────────────────────────────────

    /// <summary>
    /// Adiciona uma carta ao inventário do jogador.
    /// Chamado pelo CardSelectionManager após o jogador escolher um upgrade.
    /// </summary>
    public void AddCard(ScriptableCard card)
    {
        if (card == null) return;

        if (_cards.ContainsKey(card))
        {
            // Já tem esta carta — incrementa usos (stack)
            if (!card.isinfinite)
                _cards[card] += card.uses;
        }
        else
        {
            _cards[card] = card.isinfinite ? -1 : card.uses;
        }

        // Se não tem carta activa, activa esta automaticamente
        if (_activeCard == null)
            _activeCard = card;

        Debug.Log($"[PlayerCardUser] Carta adicionada: '{card.name}' " +
                  $"({(card.isinfinite ? "∞" : _cards[card].ToString())} usos)");
    }

    /// <summary>
    /// Muda a carta activa (útil se o jogador tiver múltiplas cartas).
    /// Pode ser chamado por um selector de cartas na UI.
    /// </summary>
    public void SetActiveCard(ScriptableCard card)
    {
        if (_cards.ContainsKey(card))
            _activeCard = card;
    }

    /// <summary>
    /// Devolve todas as cartas do jogador e os usos restantes.
    /// Útil para popular uma UI de inventário.
    /// </summary>
    public Dictionary<ScriptableCard, int> GetCards() => _cards;

    /// <summary>
    /// Remove todas as cartas (ex: ao reiniciar o match).
    /// </summary>
    public void ClearCards()
    {
        _cards.Clear();
        _activeCard = null;
    }

    // ── HELPERS ───────────────────────────────────────────────────

    private bool HasUsesLeft(ScriptableCard card)
    {
        if (!_cards.TryGetValue(card, out int uses)) return false;
        return uses == -1 || uses > 0; // -1 = infinito
    }

    private void ConsumeUseServerSide(ulong clientId, ScriptableCard card)
    {
        // O servidor valida e consome — o cliente reflecte via RPC
        ConsumeUseClientRpc(card.id, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });
    }

    [ClientRpc]
    private void ConsumeUseClientRpc(int cardId, ClientRpcParams rpcParams = default)
    {
        ScriptableCard card = FindCardById(cardId);
        if (card == null) return;

        if (_cards.TryGetValue(card, out int uses) && uses > 0)
        {
            _cards[card] = uses - 1;

            if (_cards[card] == 0 && !card.isinfinite)
            {
                Debug.Log($"[PlayerCardUser] Carta '{card.name}' esgotada.");
                // Se era a carta activa, tenta activar outra
                if (_activeCard == card)
                    SelectNextAvailableCard();
            }
        }
    }

    private void SelectNextAvailableCard()
    {
        _activeCard = null;
        foreach (var kvp in _cards)
        {
            if (HasUsesLeft(kvp.Key))
            {
                _activeCard = kvp.Key;
                break;
            }
        }

        if (_activeCard == null)
            Debug.Log("[PlayerCardUser] Sem cartas disponíveis.");
    }

    /// <summary>
    /// Procura uma carta pelo cardID em todas as cartas do inventário.
    /// Como fallback usa a CardDatabase se o CardSelectionManager estiver presente.
    /// </summary>
    private ScriptableCard FindCardById(int cardId)
    {
        foreach (var card in _cards.Keys)
            if (card.id == cardId) return card;

        // Fallback: procura na CardDatabase
        var db = FindFirstObjectByType<CardDataBase>();
        return db != null ? db.Get(cardId) : null;
    }
}