using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Guarda e gere todos os stats do jogador.
/// Vive no mesmo GameObject que PlayerHealth e o controller.
///
/// Outros scripts lêem os valores via propriedades:
///   float speed  = GetComponent<PlayerStats>().MoveSpeed;
///
/// O CardEffectManager chama Apply() / Remove() para modificadores temporários.
///
/// Setup no Inspector:
///   Preenche os valores base em "Base Stats".
///   Os valores actuais são calculados automaticamente (base + modificadores).
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats — valores sem nenhum modificador activo")]
    [SerializeField] private float _baseMoveSpeed = 8f;
    [SerializeField] private float _baseJumpHeight = 12f;
    [SerializeField] private float _baseGravity = -30f;
    [SerializeField] private float _baseDamage = 10f;
    [SerializeField] private float _baseAttackSpeed = 1f;
    [SerializeField] private float _baseKnockback = 5f;
    [SerializeField] private float _baseMaxHP = 100f;
    [SerializeField] private float _baseArmor = 0f;
    [SerializeField] private float _baseDmgReduction = 0f;   // 0–1 (percentagem)
    [SerializeField] private float _baseCardCooldown = 3f;
    [SerializeField] private float _baseCardUses = 1f;

    // Modificadores activos: chave = StatType, lista de modificadores empilhados
    private Dictionary<StatType, List<StatModifier>> _active
        = new Dictionary<StatType, List<StatModifier>>();

    // ── PROPRIEDADES PÚBLICAS ─────────────────────────────────────
    // O controller e outros scripts lêem daqui — nunca de variáveis locais.

    public float MoveSpeed => Compute(StatType.MoveSpeed, _baseMoveSpeed);
    public float JumpHeight => Compute(StatType.JumpHeight, _baseJumpHeight);
    public float Gravity => Compute(StatType.Gravity, _baseGravity);
    public float Damage => Compute(StatType.Damage, _baseDamage);
    public float AttackSpeed => Compute(StatType.AttackSpeed, _baseAttackSpeed);
    public float Knockback => Compute(StatType.Knockback, _baseKnockback);
    public float MaxHP => Compute(StatType.MaxHP, _baseMaxHP);
    public float Armor => Compute(StatType.Armor, _baseArmor);
    public float DamageReduction => Compute(StatType.DamageReduction, _baseDmgReduction);
    public float CardCooldown => Compute(StatType.CardCooldown, _baseCardCooldown);
    public float CardUses => Compute(StatType.CardUses, _baseCardUses);
    public float Vampirism => Compute(StatType.Vampirism, 0f);  // Não tem base, só modificadores
    public float FreezeDuration => Compute(StatType.freezeDuration, 0f); // Duração do congelamento (segundos)

    // ── API PÚBLICA ───────────────────────────────────────────────

    /// <summary>
    /// Aplica um modificador permanentemente (sem duração).
    /// </summary>
    public void Apply(StatModifier mod)
    {
        AddModifier(mod);
    }

    /// <summary>
    /// Aplica um modificador temporário e remove-o após 'duration' segundos.
    /// Se duration <= 0, aplica permanentemente.
    /// </summary>
    public void Apply(StatModifier mod, float duration)
    {
        if (duration <= 0f)
        {
            Apply(mod);
            return;
        }
        StartCoroutine(ApplyTemporary(mod, duration));
    }

    /// <summary>
    /// Aplica um array de modificadores (buffs ou debuffs de uma carta) com duração.
    /// </summary>
    public void ApplyAll(StatModifier[] mods, float duration)
    {
        foreach (var mod in mods)
            Apply(mod, duration);
    }

    /// <summary>
    /// Remove manualmente um modificador activo (útil para cancelar efeitos).
    /// </summary>
    public void Remove(StatModifier mod)
    {
        RemoveModifier(mod);
    }

    /// <summary>
    /// Remove todos os modificadores activos (ex: ao fim do round).
    /// </summary>
    public void ClearAll()
    {
        _active.Clear();
    }

    /// <summary>
    /// Devolve o valor actual de qualquer stat pelo tipo.
    /// Útil para inspecção genérica (ex: UI de stats).
    /// </summary>
    public float Get(StatType stat)
    {
        return stat switch
        {
            StatType.MoveSpeed => MoveSpeed,
            StatType.JumpHeight => JumpHeight,
            StatType.Gravity => Gravity,
            StatType.Damage => Damage,
            StatType.AttackSpeed => AttackSpeed,
            StatType.Knockback => Knockback,
            StatType.MaxHP => MaxHP,
            StatType.Armor => Armor,
            StatType.DamageReduction => DamageReduction,
            StatType.CardCooldown => CardCooldown,
            StatType.CardUses => CardUses,
            StatType.Vampirism => Vampirism,
            StatType.freezeDuration => FreezeDuration,
            _ => 0f
        };
    }

    // ── CÁLCULO ───────────────────────────────────────────────────

    /// <summary>
    /// Calcula o valor final de um stat:
    ///   1. Soma todos os modificadores flat
    ///   2. Aplica todos os modificadores percentuais sobre o base
    ///   resultado = (base + flat_total) * (1 + percent_total / 100)
    /// </summary>
    private float Compute(StatType stat, float baseValue)
    {
        if (!_active.TryGetValue(stat, out var mods))
            return baseValue;

        float flat = 0f;
        float percent = 0f;

        foreach (var m in mods)
        {
            if (m.IsPercent) percent += m.Value;
            else flat += m.Value;
        }

        return (baseValue + flat) * (1f + percent / 100f);
    }

    // ── INTERNOS ──────────────────────────────────────────────────

    private void AddModifier(StatModifier mod)
    {
        if (!_active.ContainsKey(mod.Stat))
            _active[mod.Stat] = new List<StatModifier>();
        _active[mod.Stat].Add(mod);
    }

    private void RemoveModifier(StatModifier mod)
    {
        if (!_active.TryGetValue(mod.Stat, out var list)) return;
        list.Remove(mod);
        if (list.Count == 0) _active.Remove(mod.Stat);
    }

    private IEnumerator ApplyTemporary(StatModifier mod, float duration)
    {
        AddModifier(mod);
        yield return new WaitForSeconds(duration);
        RemoveModifier(mod);
    }
}