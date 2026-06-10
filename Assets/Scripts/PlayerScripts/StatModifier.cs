using System;
using UnityEngine;

/// <summary>
/// Um modificador de stat — substitui as strings de buff/debuff no ScriptableCard.
/// Serializável: aparece no Inspector como campos directos.
///
/// Exemplos de uso no Inspector:
///   Buff de velocidade:   StatType=MoveSpeed, Value=50, IsPercent=true  ? +50%
///   Debuff de dano:       StatType=Damage,    Value=-10, IsPercent=false ? -10 flat
///   Redução de armadura:  StatType=Armor,     Value=5,   IsPercent=false ? +5 flat
/// </summary>
[Serializable]
public struct StatModifier
{
    [Tooltip("Qual stat é modificado")]
    public StatType Stat;

    [Tooltip("Valor a adicionar. Negativo = debuff. Positivo = buff.")]
    public float Value;

    [Tooltip("Se true, Value é tratado como percentagem (ex: +20 = +20%). " +
             "Se false, é um valor absoluto (ex: +5 = +5 de dano).")]
    public bool IsPercent;

    public override string ToString() =>
        $"{Stat} {(Value >= 0 ? "+" : "")}{Value}{(IsPercent ? "%" : "")}";
}