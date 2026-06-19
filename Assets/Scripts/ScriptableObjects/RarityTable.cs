using System;
using UnityEngine;

/// <summary>
/// ScriptableObject that defines rarity weights per round.
/// Create via: Right Click > Create > Fauneral > Rarity Table
/// </summary>
[CreateAssetMenu(fileName = "RarityTable", menuName = "Fauneral/Rarity Table")]
public class RarityTable : ScriptableObject
{
    [Serializable]
    public class RarityWeights
    {
        [Tooltip("This configuration takes effect from this round (inclusive)")]
        public int MinRound = 1;

        [Range(0, 100)] public int CommonWeight = 80;
        [Range(0, 100)] public int RareWeight = 20;
        [Range(0, 100)] public int EpicWeight = 0;
        [Range(0, 100)] public int LegendaryWeight = 0;
    }

    [Tooltip("Defines weights per game phase. Sort by MinRound ascending.")]
    public RarityWeights[] Tiers;

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

    public string RollRarity(int roundNumber)
    {
        var w = GetWeights(roundNumber);
        if (w == null) return "Common";

        int total = w.CommonWeight + w.RareWeight + w.EpicWeight + w.LegendaryWeight;
        if (total <= 0) return "Common";

        int roll = UnityEngine.Random.Range(0, total);

        if (roll < w.CommonWeight) return "Common";
        if (roll < w.CommonWeight + w.RareWeight) return "Rare";
        if (roll < w.CommonWeight + w.RareWeight + w.EpicWeight) return "Epic";
        return "Legendary";
    }
}