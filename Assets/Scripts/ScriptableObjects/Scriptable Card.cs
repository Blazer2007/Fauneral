using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Card", menuName = "Scriptable Objects/Card")]
public class ScriptableCard : ScriptableObject
{
    [Header("Card Visuals")]
    [Tooltip("The sprite representing the card's Rarity.")]
    public Image image;

    [Header("Card Information")]
    public new string name;
    public string rarity;
    public string description;
    public int cardindex;
    

    [Header("Card Effects")]
    public string[] buffs;
    public string[] debuffs;

    [Header("Card Usage")]
    [Tooltip("The number of times this card can be used.")][Range(1, 10)]
    public int uses;
    [Tooltip("The duration for which this card's effect lasts.")]
    public int time;
    [Tooltip("Indicates whether this card has infinite uses/is permanent or not.")]
    public bool isinfinite;
}
