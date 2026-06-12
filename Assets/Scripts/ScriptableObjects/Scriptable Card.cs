using UnityEngine;

/// <summary>
/// Substitui o ScriptableCard existente.
/// A �nica mudan�a � buffs[] e debuffs[] passarem de string[] para StatModifier[].
/// Todos os outros campos (name, rarity, description, image, time, uses, isinfinite)
/// mant�m-se iguais � os assets existentes migram automaticamente ao reimportar.
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
    public int id;

    [Header("Efeitos � cada entrada � um modificador de stat")]
    [Tooltip("Modificadores positivos aplicados ao jogador que usa a carta")]
    public StatModifier[] buffs;

    [Tooltip("Modificadores negativos aplicados aos oponentes")]
    public StatModifier[] debuffs;

    [Header("Utiliza��o")]
    [Tooltip("Dura��o do efeito em segundos (0 = instant�neo)")]
    public float time;

    [Tooltip("N�mero de usos")]
    public int uses;

    [Tooltip("Se true, usos s�o infinitos")]
    public bool isinfinite;

    [Tooltip("Prefab do acess�rio que se spawna com o poder da carta")]
    public GameObject accessoryPrefab;
}