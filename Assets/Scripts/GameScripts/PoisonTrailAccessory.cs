using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Acessório da carta "Poison Trail". Ao usar, o jogador fica seguido por uma
/// névoa venenosa durante 10 segundos (card.time). Qualquer jogador inimigo
/// dentro da névoa sofre 5 HP de dano por tick (não o dono).
///
/// Diferente do Polarizer (Infinite, permanente), este acessório É consumido por
/// uso normal (10 usos) e tem duração temporária (10 segundos) — encaixa no fluxo
/// padrão de AccessoryBase + PlayerCardUser sem alterações especiais.
///
/// SETUP (Inspector do prefab "PoisonTrail"):
///   - NetworkObject
///   - Collider2D (Trigger) — define o tamanho da névoa
///   - PoisonTrailAccessory (este script)
///   - _duration  → 10 segundos (deve corresponder a card.time no ScriptableCard)
///   - _tickDamage → 5 HP
///   - _tickInterval → frequência do dano (ex: 1 segundo)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PoisonTrailAccessory : AccessoryBase
{
    [SerializeField] private float _duration = 10f;
    [SerializeField] private float _tickDamage = 5f;
    [SerializeField] private float _tickInterval = 1f;
    [Tooltip("Quão rápido a névoa segue o jogador (0 = instantâneo/anexado)")]
    [SerializeField] private float _followSpeed = 0f;

    // Jogadores actualmente dentro da névoa, com o tempo até ao próximo tick
    private readonly Dictionary<PlayerHealth, float> _playersInside = new();

    private Transform _ownerTransform;
    private float _lifeTimer;

    protected override void OnInit()
    {
        if (!IsServer) return;

        _ownerTransform = FindOwnerTransform();
        _lifeTimer = 0f;

        // Se _followSpeed = 0, anexa directamente (a névoa move-se 1:1 com o jogador)
        if (_followSpeed <= 0f && _ownerTransform != null)
            transform.SetParent(_ownerTransform, worldPositionStays: false);
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned) return;

        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= _duration)
        {
            GetComponent<NetworkObject>().Despawn();
            return;
        }

        // Seguimento suave, caso _followSpeed > 0 (alternativa a SetParent)
        if (_followSpeed > 0f && _ownerTransform != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, _ownerTransform.position, _followSpeed * Time.deltaTime);
        }

        TickDamage();
    }

    private void TickDamage()
    {
        var toRemove = new List<PlayerHealth>();

        foreach (var kvp in new Dictionary<PlayerHealth, float>(_playersInside))
        {
            var health = kvp.Key;
            float timer = kvp.Value + Time.deltaTime;

            if (!health.IsAlive)
            {
                toRemove.Add(health);
                continue;
            }

            if (timer >= _tickInterval)
            {
                timer = 0f;
                health.TakeDamage(_tickDamage, OwnerId);
            }

            _playersInside[health] = timer;
        }

        foreach (var dead in toRemove)
            _playersInside.Remove(dead);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        var netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && netObj.OwnerClientId == OwnerId) return; // não envenena o próprio dono

        if (!_playersInside.ContainsKey(health))
            _playersInside[health] = _tickInterval; // causa dano imediato na entrada
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServer) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
            _playersInside.Remove(health);
    }

    private Transform FindOwnerTransform()
    {
        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (netObj.OwnerClientId == OwnerId && netObj.GetComponent<PlayerHealth>() != null)
                return netObj.transform;
        }
        return null;
    }
}
