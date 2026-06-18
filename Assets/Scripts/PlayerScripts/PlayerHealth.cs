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

    [Header("Respawn")]
    [Tooltip("The position this player respawns at")]
    [SerializeField] private Transform _spawnPoint;

    [field: Header("Runtime State (Networked)")]
    public NetworkVariable<float> NetCurrentHP = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> NetIsAlive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> NetPlayerIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float CurrentHP => NetCurrentHP.Value;
    public bool IsAlive => NetIsAlive.Value;
    public int PlayerIndex => NetPlayerIndex.Value;

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

        // Regista esta barra de vida na UI (em todos os clientes)
        if (GameUI.Instance != null)
            GameUI.Instance.RegisterPlayer(this);

        // Reorganiza as barras quando o índice do jogador é atribuído pelo servidor
        NetPlayerIndex.OnValueChanged += OnPlayerIndexChanged;
    }

    public override void OnNetworkDespawn()
    {
        NetPlayerIndex.OnValueChanged -= OnPlayerIndexChanged;

        if (GameUI.Instance != null)
            GameUI.Instance.UnregisterPlayer(this);
    }

    private void OnPlayerIndexChanged(int oldVal, int newVal)
    {
        if (GameUI.Instance != null)
            GameUI.Instance.RefreshBars();
    }

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
        {
            NetIsAlive.Value = false;
            DieClientRpc();
            _roundManager?.OnPlayerDied(this);
        }
    }

    public void FallDeath()
    {
        if (!IsServer) return;
        if (!IsAlive) return;
        NetCurrentHP.Value = 0;
        NetIsAlive.Value = false;
        DieClientRpc();
        _roundManager?.OnPlayerDied(this);
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        Debug.Log($"[PlayerHealth] Player {PlayerIndex} morreu.");
        SetPlayerState(false);
    }

    public void ResetHP()
    {
        if (!IsServer) return;
        
        NetCurrentHP.Value = MaxHP;
        NetIsAlive.Value = true;
        SetPlayerState(true);

        if (_spawnPoint != null)
        {
            transform.position = _spawnPoint.position;
        }
    }

    public void SetPlayerState(bool active)
    {
        SetPlayerStateClientRpc(active);
    }

    [ClientRpc]
    private void SetPlayerStateClientRpc(bool active)
    {
        foreach (var r in _renderers) r.enabled = active;
        foreach (var c in _colliders) c.enabled = active;
        
        var anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = active;
        
        var ctrl = GetComponent<PlayerController>();
        if (ctrl != null) ctrl.enabled = active;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "KillZone")
        {
            FallDeath();
        }
    }

    public void SetPlayerIndex(int index)
    {
        if (IsServer) NetPlayerIndex.Value = index;
    }
}