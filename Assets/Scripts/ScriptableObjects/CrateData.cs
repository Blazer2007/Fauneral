using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject que define a caixa e as probabilidades por raridade.
///
/// O peso é definido POR TIER, não por chapéu individual.
/// Qualquer chapéu novo que adiciones a um tier herda automaticamente a sua probabilidade.
///
/// Exemplo de distribuição:
///   C = 60, B = 25, A = 12, S = 3  → total 100
///   Se houver 3 chapéus C, cada um tem 60/3 = 20% de chance individual.
/// </summary>
[CreateAssetMenu(fileName = "NewCrate", menuName = "HatCrateSystem/Crate Data")]
public class CrateData : ScriptableObject
{
    [Header("Identificação")]
    public string crateID;
    public string displayName;
    [TextArea] public string description;

    [Header("Visual")]
    public Sprite crateSprite;
    public Color crateColor = Color.white;

    [Header("Probabilidade por Raridade (pesos relativos)")]
    [Tooltip("Peso do tier C. Quanto maior, mais provável de sair um chapéu C.")]
    public float weightC = 60f;
    [Tooltip("Peso do tier B.")]
    public float weightB = 25f;
    [Tooltip("Peso do tier A.")]
    public float weightA = 12f;
    [Tooltip("Peso do tier S.")]
    public float weightS = 3f;

    [Header("Chapéus disponíveis")]
    public List<HatData> hats = new();

    [Header("Preço da Caixa")]
    public int cost = 100;

    // ─────────────────────────────────────────────────────────────────────────

    public float TotalWeight() => weightC + weightB + weightA + weightS;

    public float GetWeight(HatRarity rarity) => rarity switch
    {
        HatRarity.C => weightC,
        HatRarity.B => weightB,
        HatRarity.A => weightA,
        HatRarity.S => weightS,
        _ => 0f
    };

    private void OnValidate()
    {
        if (hats.Count == 0)
            Debug.LogWarning($"[CrateData] '{crateID}': sem chapéus.");

        // Avisa se algum tier tem chapéus mas peso 0
        foreach (HatRarity r in System.Enum.GetValues(typeof(HatRarity)))
        {
            bool hasHats = hats.Exists(h => h != null && h.rarity == r);
            if (hasHats && GetWeight(r) <= 0f)
                Debug.LogWarning($"[CrateData] '{crateID}': tier {r} tem chapéus mas peso = 0!");
        }
    }
}
