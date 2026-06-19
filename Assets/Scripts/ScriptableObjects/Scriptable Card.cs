using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Substitui o ScriptableCard existente.
/// A �nica mudan�a � buffs[] e debuffs[] passarem de string[] para StatModifier[].
/// Todos os outros campos (name, rarity, description, image, time, uses, isinfinite)
/// mant�m-se iguais � os assets existentes migram automaticamente ao reimportar.
///
/// Criar via: Right Click > Create > Fauneral > Scriptable Card
/// </summary>
/// <summary>
/// Tipo de "pacto" da carta:
///   Neutral = carta normal, aparece sempre no pool base.
///   Angel   = só buffs, dada aos perdedores. Sempre passiva.
///   Devil   = buffs fortes + custo, dada ao vencedor. Sempre passiva.
/// </summary>
public enum CardDealType
{
    Neutral,
    Angel,
    Devil
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Fauneral/Scriptable Card")]
public class ScriptableCard : ScriptableObject
{
    [Header("Identidade")]
    public string rarity;
    public string description;
    public Sprite image;
    public int id;

    [Header("Ability (opcional)")]
    [Tooltip("Identificador da habilidade ligada directamente ao jogador via " +
                "PlayerAbilityHandler. Deixa vazio se a carta usar accessoryPrefab " +
                "ou apenas buffs/debuffs de stats. Ex: ")]
    public string abilityId;

    [Header("Tipo de Carta")]
    [Tooltip("Neutral = pool base | Angel = perdedores (só buffs) | Devil = vencedor (buff forte + custo)")]
    public CardDealType dealType = CardDealType.Neutral;

    [Tooltip("Se true, a carta precisa de ser activada com tecla durante a ronda (limite 3 por jogador). " +
             "Se false, é um buff passivo permanente. Angel e Devil são SEMPRE passivas (false).")]
    public bool isClickable = false;

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

    [Tooltip("Prefab do acessório que se spawna com o poder da carta")]
    public GameObject accessoryPrefab;
}