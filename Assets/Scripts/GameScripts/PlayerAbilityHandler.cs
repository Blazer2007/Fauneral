using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// Gere habilidades ligadas DIRECTAMENTE ao jogador (sem spawn de acessório próprio).
    /// Vive no prefab do jogador, ao lado de PlayerController, PlayerStats e PlayerHealth.
    ///
    /// Cartas implementadas aqui:
    ///   - ExplosiveDash : próximos N dashes deixam uma explosão na posição inicial
    ///   - Vampirism     : cura uma % do dano causado (já existe como StatType.Vampirism,
    ///                     este handler é o que efectivamente aplica a cura ao acertar)
    ///
    /// IMPORTANTE — toda a lógica corre no SERVIDOR. O PlayerController já só processa
    /// física/eventos (Dashed, Attacked) no servidor, e PlayerHealth.TakeDamage só deve
    /// ser chamado no servidor (autoridade de combate) — por isso este handler assume
    /// que está sempre a correr lado a lado com lógica server-authoritative.
    ///
    /// SETUP (Inspector):
    ///   - Adiciona ao prefab do jogador
    ///   - _explosionPrefab → prefab da explosão (com NetworkObject + ExplosionEffect)
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerStats), typeof(PlayerHealth))]
    public class PlayerAbilityHandler : NetworkBehaviour
    {
        [Header("Explosive Dash")]
        [Tooltip("Prefab da explosão deixada no início de cada dash (precisa de NetworkObject)")]
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private float _explosionDamage = 15f;
        [SerializeField] private float _explosionRadius = 2.5f;
        [Tooltip("Delay antes da explosão detonar, em segundos")]
        [SerializeField] private float _explosionFuse = 0.4f;

        private PlayerController _controller;
        private PlayerStats _stats;
        private PlayerHealth _health;

        // Contador de dashes explosivos restantes (0 = habilidade inactiva)
        private int _explosiveDashesRemaining = 0;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _health = GetComponent<PlayerHealth>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return; // Toda a lógica de habilidades corre só no servidor

            _controller.Dashed += OnDashed;
            _health.OnDamaged += OnSelfDamaged; // não usado directamente, mas mantido por simetria/futuro
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            _controller.Dashed -= OnDashed;
            _health.OnDamaged -= OnSelfDamaged;
        }

        // ── API PÚBLICA — chamada quando o jogador escolhe a carta ────

        /// <summary>
        /// Activa N usos de Explosive Dash. Chamado pelo CardSelectionManager/PlayerCardUser
        /// quando a carta "Explosive Dash" é seleccionada/usada.
        /// </summary>
        public void GrantExplosiveDash(int uses)
        {
            if (!IsServer) return;
            _explosiveDashesRemaining += uses;
            Debug.Log($"[PlayerAbilityHandler] Explosive Dash concedido: {_explosiveDashesRemaining} usos.");
        }

        // ── EXPLOSIVE DASH ─────────────────────────────────────────────

        private void OnDashed(bool isDashing)
        {
            // O evento Dashed dispara também com 'false' (estado neutro) — só nos interessa
            // o momento em que o dash É iniciado. PlayerController invoca Dashed(true) em Dash().
            if (!isDashing) return;
            if (_explosiveDashesRemaining <= 0) return;

            _explosiveDashesRemaining--;
            Vector3 explosionPos = transform.position;

            StartCoroutine(SpawnExplosionAfterFuse(explosionPos));

            Debug.Log($"[PlayerAbilityHandler] Dash explosivo usado. Restam {_explosiveDashesRemaining}.");
        }

        private IEnumerator SpawnExplosionAfterFuse(Vector3 position)
        {
            yield return new WaitForSeconds(_explosionFuse);
            DetonateExplosion(position);
        }

        private void DetonateExplosion(Vector3 position)
        {
            // Feedback visual em todos os clientes
            PlayExplosionVfxClientRpc(position);

            // Dano em área — servidor aplica directamente, sem depender do prefab visual
            var hits = Physics2D.OverlapCircleAll(position, _explosionRadius);
            foreach (var hit in hits)
            {
                var otherHealth = hit.GetComponent<PlayerHealth>();
                if (otherHealth == null || otherHealth == _health) continue; // não te atinges a ti próprio
                if (!otherHealth.IsAlive) continue;

                otherHealth.TakeDamage(_explosionDamage, OwnerClientId);
            }

            // Se houver prefab físico de explosão (partículas/colisão visual), instancia-o também
            if (_explosionPrefab != null)
            {
                var go = Instantiate(_explosionPrefab, position, Quaternion.identity);
                var netObj = go.GetComponent<NetworkObject>();
                if (netObj != null) netObj.Spawn();
                else Destroy(go, 2f); // fallback se não for um objecto de rede
            }
        }

        [ClientRpc]
        private void PlayExplosionVfxClientRpc(Vector3 position)
        {
            // Gancho para partículas/som locais sem custo de rede de um objecto completo.
            // Liga aqui o teu sistema de VFX (ex: ParticleSystem.Play() num pool local).
            Debug.Log($"[PlayerAbilityHandler] Explosão em {position}");
        }

        // ── VAMPIRISM ────────────────────────────────────────────────

        /// <summary>
        /// Chamado pelo sistema de combate (PlayerController/arma) sempre que este jogador
        /// causa dano com sucesso a outro jogador. O valor de cura vem de PlayerStats.Vampirism
        /// (StatType.Vampirism), que é incrementado pela carta via buffs no ScriptableCard —
        /// não precisa de nenhuma lógica extra aqui além de aplicar a cura.
        /// </summary>
        public void OnDealtDamage(float damageDealt)
        {
            if (!IsServer) return;

            float vampPercent = _stats.Vampirism; // ex: 5 = 5%
            if (vampPercent <= 0f) return;

            float healAmount = damageDealt * (vampPercent / 100f);
            _health.Heal(healAmount);
        }

        // Placeholder simétrico — não usado pela Vampirism (que cura no ATACANTE, não na vítima),
        // mas mantido para futuras cartas que reajam a "levar dano".
        private void OnSelfDamaged(ulong attackerId, float amount) { }
    }
}
