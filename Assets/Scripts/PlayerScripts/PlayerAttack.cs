using Unity.Netcode;
using UnityEngine;
using TarodevController;

/// <summary>
/// ALTERAÇÕES NESTA VERSÃO:
///   - PlayerAttack passa a herdar de NetworkBehaviour (precisa de OwnerClientId
///     para identificar o atacante perante o PlayerHealth da vítima).
///   - OnAttacked só dispara no servidor (PlayerController.Attacked já só é
///     invocado dentro de blocos "if (!IsServer) return;"), por isso DoLightAttack/
///     DoHeavyAttack chamam agora health.TakeDamage(...) DIRECTAMENTE em vez de
///     TakeDamageServerRpc — já estamos no servidor, um RPC seria só overhead.
///   - Ambas as chamadas passam agora OwnerClientId como attackerId, o que activa
///     correctamente o evento PlayerHealth.OnDamaged → PlayerAbilityHandler.
///     Vampirism (e qualquer efeito futuro "ao causar dano") passam a funcionar.
/// </summary>
public class PlayerAttack : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerStats _playerstats;
    private IPlayerController _controller;
    private TarodevController.PlayerAbilityHandler _abilityHandler;

    [Header("Ataque leve")]
    public Transform _lightAttackHtBx;
    public float _lightAttackRange = 0.5f;

    [Header("Ataque pesado")]
    public Transform _heavyAttackHtBx;
    public float _heavyAttackRange = 0.8f;

    [Header("Layer")]
    public LayerMask _playerLayer;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _playerstats = GetComponent<PlayerStats>();
        _abilityHandler = GetComponent<TarodevController.PlayerAbilityHandler>(); // opcional, pode não existir
    }

    private void OnEnable()
    {
        _controller.Attacked += OnAttacked;
    }

    private void OnDisable()
    {
        _controller.Attacked -= OnAttacked;
    }

    private void OnAttacked(bool attacked, bool isHeavy)
    {
        if (!attacked)
        {
            return;
        }

        // Salvaguarda: Attacked só deve disparar no servidor (ver PlayerController),
        // mas confirmamos aqui também para garantir que TakeDamage nunca é chamado
        // a partir de um cliente sem autoridade.
        if (!IsServer) return;

        if (isHeavy)
        {
            Debug.Log("Heavy attack executed");
            DoHeavyAttack();
        }
        else
        {
            Debug.Log("Light attack executed");
            DoLightAttack();
        }
    }

    private void DoLightAttack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_lightAttackHtBx.position, _lightAttackRange, _playerLayer);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            PlayerHealth health = hit.GetComponent<PlayerHealth>();
            if (health != null)
            {
                float damageDealt = _playerstats.Damage;
                health.TakeDamage(damageDealt, OwnerClientId, false);

                // Vampirism e outros efeitos "ao causar dano" — opcional, só se a
                // carta tiver sido escolhida (PlayerAbilityHandler trata disso internamente)
                _abilityHandler?.OnDealtDamage(damageDealt);
            }
        }
    }

    private void DoHeavyAttack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_heavyAttackHtBx.position, _heavyAttackRange, _playerLayer);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            PlayerHealth health = hit.GetComponent<PlayerHealth>();
            if (health != null)
            {
                float damageDealt = _playerstats.Damage * 2;
                health.TakeDamage(damageDealt, OwnerClientId, true);

                _abilityHandler?.OnDealtDamage(damageDealt);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_lightAttackHtBx != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_lightAttackHtBx.position, _lightAttackRange);
        }

        if (_heavyAttackHtBx != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_heavyAttackHtBx.position, _heavyAttackRange);
        }
    }
}