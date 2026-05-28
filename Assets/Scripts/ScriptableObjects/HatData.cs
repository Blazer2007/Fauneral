using UnityEngine;

/// <summary>
/// ScriptableObject que representa um chapéu desbloqueável.
/// Cria um asset por chapéu em: Assets > Create > HatCrateSystem > Hat Data
/// </summary>
[CreateAssetMenu(fileName = "NewHat", menuName = "HatCrateSystem/Hat Data")]
public class HatData : ScriptableObject
{
    [Header("Identificação")]
    public string hatID;           // ID único, ex: "hat_cowboy"
    public string displayName;     // Nome visível ao jogador
    [TextArea] public string description;

    [Header("Raridade")]
    [Tooltip("Apenas visual/cosmético — não afecta a probabilidade de sair da caixa")]
    public HatRarity rarity;

    [Header("Visual")]
    public Sprite previewSprite;
    public GameObject hatPrefab;

    [Header("Cor da Raridade (auto-preenchida)")]
    public Color rarityColor => RarityColor();

    private Color RarityColor()
    {
        return rarity switch
        {
            HatRarity.C => new Color(0.75f, 0.75f, 0.75f), // Cinzento
            HatRarity.B => new Color(0.20f, 0.70f, 1.00f), // Azul
            HatRarity.A => new Color(0.65f, 0.20f, 0.90f), // Roxo
            HatRarity.S => new Color(1.00f, 0.70f, 0.10f), // Dourado
            _ => Color.white
        };
    }
}

public enum HatRarity
{
    C,  // Comum
    B,  // Incomum
    A,  // Raro
    S   // Excepcional
}
