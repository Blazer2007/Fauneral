/// <summary>
/// Todos os stats modific�veis de um jogador.
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
    freezeDuration, // dura��o do congelamento (segundos)

    // Defesa / Vida
    MaxHP,
    HP,
    Armor,          // redu��o de dano (flat)
    DamageReduction, // redu��o de dano (%)
    SpikeDamage,     // dano causado por espinhos (flat)

    // Utilit�rio
    CardCooldown,
    CardUses,

    //Diferenciados
    RepulsionForce,
    PoisonDamage,

    // Custo de cartas Devil — HP perdido por segundo durante a ronda
    HpDrainPerSecond
}