using UnityEngine;

/// <summary>
/// Substitui o ScriptableCard existente.
/// A única mudança é buffs[] e debuffs[] passarem de string[] para StatModifier[].
/// Todos os outros campos (name, rarity, description, image, time, uses, isinfinite)
/// mantêm-se iguais — os assets existentes migram automaticamente ao reimportar.
///
/// Criar via: Right Click > Create > Fauneral > Scriptable Card
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Fauneral/Scriptable Card")]
public class ScriptableCard : ScriptableObject
{
    [Header("Identidade")]
    public string rarity;
    public string description;
    public SpriteRenderer image;

    [Header("Efeitos — cada entrada é um modificador de stat")]
    [Tooltip("Modificadores positivos aplicados ao jogador que usa a carta")]
    public StatModifier[] buffs;

    [Tooltip("Modificadores negativos aplicados aos oponentes")]
    public StatModifier[] debuffs;

    [Header("Utilização")]
    [Tooltip("Duração do efeito em segundos (0 = instantâneo)")]
    public float time;

    [Tooltip("Número de usos")]
    public int uses;

    [Tooltip("Se true, usos são infinitos")]
    public bool isinfinite;
}