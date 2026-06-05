using System;
using UnityEngine;

/// <summary>
/// ScriptableObject que define os pesos de raridade por ronda.
/// Criar via: Right Click > Create > Fauneral > Rarity Table
///
/// Cada RarityTier define a partir de que ronda os pesos entram em vigor.
/// O sistema usa o tier cujo MinRound seja o mais alto sem ultrapassar a ronda actual.
/// </summary>
[CreateAssetMenu(fileName = "RarityTable", menuName = "Fauneral/Rarity Table")]
public class RarityTable : ScriptableObject
{
    [Serializable]
    public class RarityWeights
    {
        [Tooltip("Esta configuração entra em vigor a partir desta ronda (inclusive)")]
        public int MinRound = 1;

        [Range(0, 100)] public int CommonWeight = 80;
        [Range(0, 100)] public int UncommonWeight = 20;
        [Range(0, 100)] public int RareWeight = 0;
        [Range(0, 100)] public int LegendaryWeight = 0;
    }

    [Tooltip("Define os pesos por fase do jogo. Ordena por MinRound ascendente.")]
    public RarityWeights[] Tiers;

    /// <summary>
    /// Devolve os pesos activos para a ronda indicada.
    /// </summary>
    public RarityWeights GetWeights(int roundNumber)
    {
        RarityWeights active = Tiers != null && Tiers.Length > 0 ? Tiers[0] : null;
        if (Tiers == null) return active;

        foreach (var tier in Tiers)
        {
            if (roundNumber >= tier.MinRound)
                active = tier;
        }
        return active;
    }

    /// <summary>
    /// Sorteia uma raridade com base nos pesos activos para a ronda dada.
    /// Devolve a string que deve corresponder ao campo ScriptableCard.rarity.
    /// </summary>
    public string RollRarity(int roundNumber)
    {
        var w = GetWeights(roundNumber);
        if (w == null) return "Common";

        int total = w.CommonWeight + w.UncommonWeight + w.RareWeight + w.LegendaryWeight;
        if (total <= 0) return "Common";

        int roll = UnityEngine.Random.Range(0, total);

        if (roll < w.CommonWeight) return "Common";
        if (roll < w.CommonWeight + w.UncommonWeight) return "Uncommon";
        if (roll < w.CommonWeight + w.UncommonWeight + w.RareWeight) return "Rare";
        return "Legendary";
    }
}