using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lógica central do sistema de caixas.
///
/// Sorteio em dois passos:
///   1. Sorteia o TIER (C/B/A/S) com base nos pesos definidos no CrateData.
///   2. Dentro do tier sorteado, escolhe um chapéu aleatório com igual probabilidade.
///
/// Isto garante que os pesos reflectem a chance de cada tier independentemente
/// de quantos chapéus existem em cada um.
/// </summary>
public class CrateSystem : MonoBehaviour
{
    public static CrateSystem Instance { get; private set; }

    [Header("A caixa do jogo")]
    public CrateData crate;

    public event System.Action OnCrateOpenStart;
    public event System.Action<HatData, bool> OnCrateOpenResult; // hat, isNew

    private bool _isOpening = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────

    public bool OpenCrate()
    {
        if (_isOpening)
        {
            Debug.LogWarning("[CrateSystem] Já está a abrir uma caixa!");
            return false;
        }
        if (crate == null || crate.hats.Count == 0)
        {
            Debug.LogError("[CrateSystem] Caixa inválida ou sem chapéus.");
            return false;
        }

        StartCoroutine(OpenCrateRoutine());
        return true;
    }

    /// <summary>
    /// Sorteio em dois passos:
    ///   Passo 1 — sorteia o tier pelo peso (ex: C=60, B=25, A=12, S=3)
    ///   Passo 2 — dentro do tier, escolhe um chapéu aleatoriamente
    /// </summary>
    public HatData RollHat()
    {
        if (crate == null || crate.hats.Count == 0) return null;

        // Passo 1: sorteia o tier
        HatRarity rolledTier = RollTier();

        // Passo 2: filtra chapéus desse tier e escolhe um
        var pool = crate.hats.FindAll(h => h != null && h.rarity == rolledTier);

        // Fallback: se o tier sorteado não tiver chapéus, tenta os outros por ordem
        if (pool.Count == 0)
        {
            Debug.LogWarning($"[CrateSystem] Tier {rolledTier} sorteado mas sem chapéus — a fazer fallback.");
            pool = crate.hats.FindAll(h => h != null);
        }

        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Weighted random só entre os 4 tiers.
    /// </summary>
    private HatRarity RollTier()
    {
        float total = crate.TotalWeight();
        float roll  = Random.Range(0f, total);
        float acc   = 0f;

        foreach (HatRarity tier in new[] { HatRarity.C, HatRarity.B, HatRarity.A, HatRarity.S })
        {
            acc += crate.GetWeight(tier);
            if (roll <= acc) return tier;
        }

        return HatRarity.C; // fallback
    }

    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator OpenCrateRoutine()
    {
        _isOpening = true;
        OnCrateOpenStart?.Invoke();

        HatData result = RollHat();

        yield return new WaitForSeconds(CrateUI.SPIN_DURATION);

        bool isNew = false;
        if (result != null && HatInventory.Instance != null)
            isNew = HatInventory.Instance.UnlockHat(result);

        OnCrateOpenResult?.Invoke(result, isNew);
        _isOpening = false;
    }
}
