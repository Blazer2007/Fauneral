using UnityEngine;
using TarodevController;

public class PlayerAttack : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerStats _playerstats;
    private IPlayerController _controller;

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

        if (isHeavy)
        {
            DoHeavyAttack();
        }
        else
        {
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
                health.TakeDamage(_playerstats.Damage);
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
                health.TakeDamage(_playerstats.Damage * 2);
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

