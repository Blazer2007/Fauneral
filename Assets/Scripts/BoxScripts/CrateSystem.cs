using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class CrateSystem : MonoBehaviour
{
    public static CrateSystem Instance { get; private set; }

    [Header("A caixa do jogo")]
    public CrateData crate;

    // Passa o vencedor à UI no início para a animação o colocar no centro
    public event System.Action<HatData> OnCrateOpenStart;
    public event System.Action<HatData, bool> OnCrateOpenResult;

    private bool _isOpening = false;

    public ScriptableCredits credits;
    public Action OnInsufficientCredits;

    private void Awake()
    {
        Instance = this;
    }

    public bool OpenCrate()
    {
        if (credits != null && !credits.TrySpend(crate.cost))
        {
            OnInsufficientCredits?.Invoke();
            return false;
        }

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

    public HatData RollHat()
    {
        if (crate == null || crate.hats.Count == 0) return null;

        HatRarity rolledTier = RollTier();
        var pool = crate.hats.FindAll(h => h != null && h.rarity == rolledTier);

        if (pool.Count == 0)
        {
            Debug.LogWarning($"[CrateSystem] Tier {rolledTier} sem chapéus — fallback.");
            pool = crate.hats.FindAll(h => h != null);
        }

        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

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

        return HatRarity.C;
    }

    private IEnumerator OpenCrateRoutine()
    {
        _isOpening = true;

        // Sorteia UMA vez e passa à UI — a animação usa este mesmo chapéu
        HatData result = RollHat();
        OnCrateOpenStart?.Invoke(result);

        yield return new WaitForSeconds(CrateUI.SPIN_DURATION);

        bool isNew = false;
        if (result != null && HatInventory.Instance != null)
            isNew = HatInventory.Instance.UnlockHat(result);

        OnCrateOpenResult?.Invoke(result, isNew);
        _isOpening = false;
    }
}
