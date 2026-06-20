using UnityEngine;
using TarodevController;
using Unity.Netcode;

/// <summary>
/// Manages the player's HP, death, and respawn.
///
/// ALTERAÇÕES NESTA VERSÃO (face à tua):
///   - TakeDamage tem agora uma sobrecarga com attackerId (ulong). A versão sem
///     attackerId continua a existir e chama a nova passando ulong.MaxValue
///     (= "sem atacante", ex: dano ambiental, FallDeath, drenagem do pacto Devil).
///     Nada que já chamava TakeDamage(amount) precisa de mudar.
///   - NetCurrentHP/NetIsAlive ficam exactamente iguais (Server Authoritative,
///     já estava correcto).
///   - Adicionado evento OnDamaged(ulong attackerId, float amount), disparado no
///     SERVIDOR sempre que dano é aplicado com sucesso. PlayerAbilityHandler usa isto
///     para Vampirism (cura o ATACANTE, não a vítima) — ver OnDealtDamage().
///   - Adicionado Heal(float amount), usado pela Vampirism.
///   - TakeDamageServerRpc tem agora uma sobrecarga que aceita attackerId, para que o
///     cliente que ataca possa identificar-se ao servidor (necessário para Vampirism
///     funcionar correctamente quando o ataque é despoletado a partir do cliente).
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    [Header("Stats")]
    [Tooltip("Referência ao PlayerStats do mesmo GameObject - MaxHP vem daqui")]
    private PlayerStats _playerStats;

    [Header("Respawn")]
    [Tooltip("The position this player respawns at")]
    [SerializeField] private Transform _spawnPoint;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip _deathSound;
    [SerializeField] private AudioClip[] _lightHitSounds;
    [SerializeField] private AudioClip[] _heavyHitSounds;
    private AudioSource _audioSource;

    [field: Header("Runtime State (Networked)")]
    public NetworkVariable<float> NetCurrentHP = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> NetIsAlive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> NetPlayerIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float CurrentHP => NetCurrentHP.Value;
    public bool IsAlive => NetIsAlive.Value;
    public int PlayerIndex => NetPlayerIndex.Value;

    // MaxHP vem do PlayerStats - cartas de upgrade alteram este valor correctamente
    public float MaxHP => _playerStats != null ? _playerStats.MaxHP : 100f;

    /// <summary>
    /// Disparado no SERVIDOR sempre que este jogador sofre dano com sucesso.
    /// Parâmetros: (attackerId, amount aplicado depois de armor/redução).
    /// attackerId = ulong.MaxValue quando não há atacante identificável (ambiental, Devil drain).
    /// Usado por PlayerAbilityHandler.OnDealtDamage (Vampirism) e por dano em área.
    /// </summary>
    public event System.Action<ulong, float> OnDamaged;

    private RoundManager _roundManager;
    private SpriteRenderer[] _renderers;
    private Collider2D[] _colliders;

    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();
        _roundManager = FindFirstObjectByType<RoundManager>();
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _colliders = GetComponentsInChildren<Collider2D>();
        _audioSource = GetComponentInChildren<AudioSource>();

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

    // Custo das cartas Devil: drena HP por segundo enquanto a ronda está activa.
    private void Update()
    {
        if (!IsServer || !IsAlive || _playerStats == null) return;

        if (_roundManager == null) _roundManager = RoundManager.Instance;
        if (_roundManager == null || !_roundManager.RoundActive) return;

        float drain = _playerStats.HpDrainPerSecond;
        if (drain <= 0f) return;

        NetCurrentHP.Value = Mathf.Max(0f, NetCurrentHP.Value - drain * Time.deltaTime);

        if (NetCurrentHP.Value <= 0f)
        {
            Debug.Log($"[PlayerHealth] Player {PlayerIndex} morreu pelo custo do pacto (Devil).");
            NetIsAlive.Value = false;
            DieClientRpc();
            _roundManager.OnPlayerDied(this);
        }
    }

    // ── TAKE DAMAGE (server RPC) ───────────────────────────────────

    /// <summary>
    /// Sobrecarga original — mantida por compatibilidade com chamadas existentes.
    /// Sem atacante identificado (ex: trap genérica, projétil sem owner exposto).
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount)
    {
        TakeDamage(amount);
    }

    /// <summary>
    /// Sobrecarga com atacante identificado — usa esta quando o ataque tem um dono
    /// claro (ex: hitbox de PlayerController, minion, poison trail) para permitir
    /// que efeitos como Vampirism curem correctamente quem causou o dano.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount, ulong attackerId)
    {
        TakeDamage(amount, attackerId);
    }

    /// <summary>
    /// Aplica dano sem atacante identificável (dano ambiental, drenagem de pacto, etc).
    /// </summary>
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, ulong.MaxValue);
    }

    /// <summary>
    /// Aplica dano identificando quem o causou. Toda a lógica de armor/redução
    /// e morte mantém-se exactamente igual à tua versão — a única adição é o
    /// disparo de OnDamaged no fim, com o attackerId propagado.
    /// </summary>
    public void TakeDamage(float amount, ulong attackerId, bool isHeavy = false)
    {
        if (!IsServer) return;
        if (!IsAlive) return;

        float armor = _playerStats != null ? _playerStats.Armor : 0f;
        float dmgRedPct = _playerStats != null ? _playerStats.DamageReduction : 0f;

        float mitigated = Mathf.Max(0f, amount - armor);
        mitigated *= (1f - Mathf.Clamp01(dmgRedPct));

        NetCurrentHP.Value -= mitigated;
        NetCurrentHP.Value = Mathf.Clamp(NetCurrentHP.Value, 0, MaxHP);

        OnDamaged?.Invoke(attackerId, mitigated);

        if (NetCurrentHP.Value <= 0)
        {
            NetIsAlive.Value = false;
            DieClientRpc();
            _roundManager?.OnPlayerDied(this);
        }
        else
        {
            PlayHitSoundClientRpc(isHeavy);
        }
    }

    [ClientRpc]
    private void PlayHitSoundClientRpc(bool isHeavy)
    {
        if (_audioSource == null) return;
        AudioClip[] clips = isHeavy ? _heavyHitSounds : _lightHitSounds;
        if (clips != null && clips.Length > 0)
        {
            AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
            _audioSource.PlayOneShot(randomClip);
        }
    }

    /// <summary>
    /// Cura o jogador, sem exceder MaxHP. Só corre no servidor (autoridade de HP).
    /// Usado por Vampirism (PlayerAbilityHandler.OnDealtDamage).
    /// </summary>
    public void Heal(float amount)
    {
        if (!IsServer) return;
        if (!IsAlive) return;

        NetCurrentHP.Value = Mathf.Clamp(NetCurrentHP.Value + amount, 0, MaxHP);
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
        if (_audioSource != null && _deathSound != null)
        {
            _audioSource.PlayOneShot(_deathSound);
        }
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