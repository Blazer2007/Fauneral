using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Comportamento de um minion individual (spawnado por MinionSpawner).
/// Persegue o jogador inimigo mais próximo (qualquer um que não seja o OwnerId)
/// e causa dano por contacto, com um cooldown entre ataques.
///
/// Toda a lógica de movimento/dano corre apenas no servidor – usa o mesmo
/// NetworkTransform Server Authoritative do resto do projecto para propagar
/// a posição do minion aos clientes.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class MinionUnit : NetworkBehaviour
{
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _attackRange = 0.6f;
    [SerializeField] private float _attackDamage = 8f;
    [SerializeField] private float _attackCooldown = 1.5f;
    [SerializeField] private float _lifetime = 15f; // auto-destruição para não acumular minions

    public ulong OwnerId { get; private set; }

    private float _attackTimer;
    private float _lifeTimer;
    private PlayerHealth _currentTarget;

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;

    public void Init(ulong ownerId)
    {
        OwnerId = ownerId;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_rb != null)
        {
            _rb.gravityScale = 0f; // Vira minion voador/flutuante para não cair no KillZone ou ficar preso sob plataformas
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= _lifetime)
        {
            GetComponent<NetworkObject>().Despawn();
            return;
        }

        _attackTimer += Time.deltaTime;

        FindTarget();
        if (_currentTarget == null)
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        MoveTowardsTarget();
        TryAttack();
    }

    private void FindTarget()
    {
        // Se já tem alvo vivo e ainda dentro de alcance razoável, mantém
        if (_currentTarget != null && _currentTarget.IsAlive) return;

        _currentTarget = null;
        float closestDist = float.MaxValue;

        foreach (var health in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (!health.IsAlive) continue;

            // Não ataca o dono do minion
            var netObj = health.GetComponent<NetworkObject>();
            if (netObj != null && netObj.OwnerClientId == OwnerId) continue;

            float dist = Vector3.Distance(transform.position, health.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                _currentTarget = health;
            }
        }
    }

    private void MoveTowardsTarget()
    {
        Vector3 dir = (_currentTarget.transform.position - transform.position).normalized;

        if (_rb != null)
        {
            _rb.linearVelocity = (Vector2)dir * _moveSpeed;
        }
        else
        {
            transform.position += dir * _moveSpeed * Time.deltaTime;
        }

        if (_spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
        {
            _spriteRenderer.flipX = dir.x < 0f;
        }
    }

    private void TryAttack()
    {
        float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);
        if (dist > _attackRange) return;
        if (_attackTimer < _attackCooldown) return;

        _attackTimer = 0f;
        _currentTarget.TakeDamage(_attackDamage, OwnerId);
    }
}