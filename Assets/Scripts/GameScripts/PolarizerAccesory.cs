using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Acessório da carta "Polarizer". Em vez de spawnar um objecto separado,
/// este componente fica ANEXADO ao próprio jogador (é instanciado uma vez,
/// como filho do jogador, e fica activo permanentemente — carta "Infinite").
///
/// Funciona em ticks: X segundos activo (repele) → Y segundos inactivo → repete.
/// Em vez de um único timer simples, usa um ciclo claro de dois estados para
/// que seja fácil sincronizar visualmente com VFX (ex: "carregar" antes da repulsão).
///
/// IMPORTANTE: Como o Polarizer é "Infinite", NÃO se gere por usos como as outras
/// cartas — activa-se uma vez (ao escolher a carta) e fica ligado para o resto do
/// jogo/ronda. Por isso, em vez de seguir o fluxo normal de accessoryPrefab + Init
/// chamado a cada "uso" do PlayerCardUser, este acessório deve ser instanciado UMA
/// VEZ no momento da escolha da carta (ver nota em CardSelectionManager mais abaixo).
///
/// SETUP (Inspector do prefab "PolarizerField"):
///   - NetworkObject
///   - PolarizerAccessory (este script)
///   - _activeDuration  → 2 a 3 segundos (tempo activo repelindo)
///   - _inactiveDuration → 1 a 2 segundos (tempo inactivo)
///   - _radius e _force → alcance e força da repulsão
/// </summary>
public class PolarizerAccessory : AccessoryBase
{
    [Header("Ciclo de ticks")]
    [SerializeField] private float _activeDuration = 2.5f;
    [SerializeField] private float _inactiveDuration = 1.5f;

    [Header("Repulsão")]
    [SerializeField] private float _radius = 3f;
    [SerializeField] private float _force = 12f;

    private bool _isActive = false;
    private float _phaseTimer = 0f;

    protected override void OnInit()
    {
        if (!IsServer) return;

        // Anexa-se ao jogador dono para se mover sempre com ele
        var ownerTransform = FindOwnerTransform();
        if (ownerTransform != null)
            transform.SetParent(ownerTransform, worldPositionStays: false);

        _isActive = true; // começa no estado activo
        _phaseTimer = 0f;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned) return;

        _phaseTimer += Time.deltaTime;

        if (_isActive)
        {
            ApplyRepulsion();

            if (_phaseTimer >= _activeDuration)
            {
                _isActive = false;
                _phaseTimer = 0f;
                SetVisualStateClientRpc(false);
            }
        }
        else
        {
            if (_phaseTimer >= _inactiveDuration)
            {
                _isActive = true;
                _phaseTimer = 0f;
                SetVisualStateClientRpc(true);
            }
        }
    }

    private void ApplyRepulsion()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, _radius);
        foreach (var hit in hits)
        {
            var rb = hit.GetComponent<Rigidbody2D>();
            var netObj = hit.GetComponent<NetworkObject>();
            if (rb == null || netObj == null) continue;
            if (netObj.OwnerClientId == OwnerId) continue; // não repele o próprio dono

            Vector2 dir = (hit.transform.position - transform.position);
            float dist = dir.magnitude;
            if (dist <= 0.01f) continue;

            dir.Normalize();
            // Força decresce com a distância (campo magnético realista)
            float falloff = 1f - Mathf.Clamp01(dist / _radius);
            rb.AddForce(dir * _force * falloff, ForceMode2D.Force);
        }
    }

    [ClientRpc]
    private void SetVisualStateClientRpc(bool active)
    {
        // Gancho para VFX (ex: activar/desactivar um glow ou partícula em todos os clientes)
        Debug.Log($"[Polarizer] Estado visual: {(active ? "ACTIVO (repelindo)" : "inactivo")}");
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
