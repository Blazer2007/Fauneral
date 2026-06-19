using UnityEngine;
using TarodevController;
using Unity.Netcode;

/// <summary>
/// Manages the player's HP, death, and respawn.
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    [Header("Stats")]
    [Tooltip("Referência ao PlayerStats do mesmo GameObject - MaxHP vem daqui")]
    private PlayerStats _playerStats;

    [Header("Player Settings")]
    [Tooltip("Player index: 0 = P1, 1 = P2, 2 = P3, 3 = P4")]
    [SerializeField] private int _playerIndex = 0;

    [Header("Respawn")]
    [Tooltip("The position this player respawns at")]
    [SerializeField] private Transform _spawnPoint;

    [field: Header("Runtime State (Networked)")]
    public NetworkVariable<float> NetCurrentHP = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> NetIsAlive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float CurrentHP => NetCurrentHP.Value;
    public bool IsAlive => NetIsAlive.Value;

    public int PlayerIndex => _playerIndex;

    // MaxHP vem do PlayerStats - cartas de upgrade alteram este valor correctamente
    public float MaxHP => _playerStats != null ? _playerStats.MaxHP : 100f;



    private RoundManager _roundManager;
    private SpriteRenderer[] _renderers;
    private Collider2D[] _colliders;

    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();
        _roundManager = FindFirstObjectByType<RoundManager>();
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _colliders = GetComponentsInChildren<Collider2D>();

        if (_playerStats == null)
            Debug.LogWarning($"[PlayerHealth] PlayerStats não encontrado em {gameObject.name}!", this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ResetHP();
        }
    }
    //private void Update()
    //{
    //    UpdateHP();
    //}

    /// <summary>
    /// Apply damage to this player. Should be called on server.
    /// </summary>

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount)
    {
        TakeDamage(amount);
    }


    public void TakeDamage(float amount)
    {
        if (!IsServer) return;
        if (!IsAlive) return;

        float armor = _playerStats != null ? _playerStats.Armor : 0f;
        float dmgRedPct = _playerStats != null ? _playerStats.DamageReduction : 0f;
        float mitigated = Mathf.Max(0f, amount - armor);
        mitigated *= (1f - Mathf.Clamp01(dmgRedPct));

        NetCurrentHP.Value -= mitigated;
        NetCurrentHP.Value = Mathf.Clamp(NetCurrentHP.Value, 0, MaxHP);

        if (NetCurrentHP.Value <= 0)
            Die();
    }

    public void FallDeath()
    {
        if (!IsServer) return;
        if (!IsAlive) return;
        NetCurrentHP.Value = 0;
        Die();
    }

    private void Die()
    {
        // Tudo no servidor
        NetIsAlive.Value = false;
        DisablePlayerClientRpc();
        _roundManager?.OnPlayerDied(this);
    }

    [ClientRpc]
    private void DisablePlayerClientRpc()
    {
        Debug.Log($"[PlayerHealth] Player {PlayerIndex} morreu.");
        foreach (var r in _renderers) r.enabled = false;
        foreach (var c in _colliders) c.enabled = false;
        var ctrl = GetComponent<PlayerController>();
        if (ctrl != null) ctrl.enabled = false;
    }

    public void ResetHP()
    {
        if (!IsServer) return;
        NetCurrentHP.Value = MaxHP;
        NetIsAlive.Value = true;
        EnablePlayerClientRpc();
    }

    [ClientRpc]
    private void EnablePlayerClientRpc()
    {
        foreach (var r in _renderers) r.enabled = true;
        foreach (var c in _colliders) c.enabled = true;
        var ctrl = GetComponent<PlayerController>();
        if (ctrl != null) ctrl.enabled = true;
    }

    public void SetPlayerIndex(int index) => _playerIndex = index;

    public void TeleportTo(Vector3 position)
    {
        if (!IsServer) return;
        transform.position = position;
        TeleportClientRpc(position);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 position)
    {
        transform.position = position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (collision.CompareTag("KillZone"))
            FallDeath();
    }
}    