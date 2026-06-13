/// <summary>
/// Todos os stats modificáveis de um jogador.
/// Adiciona aqui novos stats conforme o jogo crescer.
/// </summary>
public enum StatType
{
    // Movimento
    MoveSpeed,
    JumpHeight,
    Gravity,
    DashCooldown,   // Cooldown do dash (segundos)

    // Combate
    Damage,
    AttackSpeed,
    Knockback,
    Vampirism,      // % de dano convertido em cura
    burnDamage,    // dano causado por queimadura (flat)
    freezeDuration, // duração do congelamento (segundos)

    // Defesa / Vida
    MaxHP,
    Armor,          // redução de dano (flat)
    DamageReduction, // redução de dano (%)
    SpikeDamage,     // dano causado por espinhos (flat)

    // Utilitário
    CardCooldown,
    CardUses,

    //Diferenciados
    RepulsionForce,
    PoisonDamage
}