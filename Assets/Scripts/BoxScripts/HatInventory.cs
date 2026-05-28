using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gere o inventário de chapéus do jogador.
/// Persiste com PlayerPrefs (fácil de trocar para SaveSystem/cloud depois).
/// Adiciona este script a um GameObject "InventoryManager" na cena principal.
/// </summary>
public class HatInventory : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static HatInventory Instance { get; private set; }

    // ── Estado ───────────────────────────────────────────────────────────────
    private HashSet<string> _unlockedHatIDs = new();
    private string _equippedHatID = "";

    private const string SAVE_KEY_UNLOCKED = "HatInventory_Unlocked";
    private const string SAVE_KEY_EQUIPPED = "HatInventory_Equipped";

    // ── Eventos (outros scripts podem subscrever) ─────────────────────────
    public event System.Action<HatData> OnHatUnlocked;
    public event System.Action<HatData> OnHatEquipped;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>Desbloqueia um chapéu. Retorna false se já estava desbloqueado.</summary>
    public bool UnlockHat(HatData hat)
    {
        if (hat == null) return false;
        if (_unlockedHatIDs.Contains(hat.hatID)) return false; // já tinha

        _unlockedHatIDs.Add(hat.hatID);
        Save();

        Debug.Log($"[Inventory] Desbloqueado: {hat.displayName} ({hat.rarity})");
        OnHatUnlocked?.Invoke(hat);
        return true;
    }

    /// <summary>Equipa um chapéu (tem de estar desbloqueado).</summary>
    public bool EquipHat(HatData hat)
    {
        if (hat == null) return false;
        if (!IsUnlocked(hat))
        {
            Debug.LogWarning($"[Inventory] Tentou equipar '{hat.hatID}' mas não está desbloqueado.");
            return false;
        }

        _equippedHatID = hat.hatID;
        Save();
        OnHatEquipped?.Invoke(hat);
        return true;
    }

    /// <summary>Remove o chapéu equipado.</summary>
    public void UnequipHat()
    {
        _equippedHatID = "";
        Save();
        OnHatEquipped?.Invoke(null);
    }

    public bool IsUnlocked(HatData hat) => hat != null && _unlockedHatIDs.Contains(hat.hatID);
    public bool IsEquipped(HatData hat)  => hat != null && _equippedHatID == hat.hatID;
    public string GetEquippedHatID()     => _equippedHatID;

    /// <summary>Retorna todos os IDs desbloqueados (para UI do inventário).</summary>
    public IReadOnlyCollection<string> GetAllUnlocked() => _unlockedHatIDs;

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Save / Load (PlayerPrefs)

    private void Save()
    {
        // Guarda IDs separados por vírgula
        string joined = string.Join(",", _unlockedHatIDs);
        PlayerPrefs.SetString(SAVE_KEY_UNLOCKED, joined);
        PlayerPrefs.SetString(SAVE_KEY_EQUIPPED, _equippedHatID);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        _unlockedHatIDs.Clear();
        string raw = PlayerPrefs.GetString(SAVE_KEY_UNLOCKED, "");
        if (!string.IsNullOrEmpty(raw))
            foreach (var id in raw.Split(','))
                _unlockedHatIDs.Add(id);

        _equippedHatID = PlayerPrefs.GetString(SAVE_KEY_EQUIPPED, "");
        Debug.Log($"[Inventory] Carregado: {_unlockedHatIDs.Count} chapéu(s) desbloqueado(s).");
    }

    /// <summary>CUIDADO: apaga todo o progresso. Útil para testes.</summary>
    [ContextMenu("Limpar Inventário (DEBUG)")]
    public void ClearInventory()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY_UNLOCKED);
        PlayerPrefs.DeleteKey(SAVE_KEY_EQUIPPED);
        _unlockedHatIDs.Clear();
        _equippedHatID = "";
        Debug.Log("[Inventory] Inventário limpo.");
    }

    #endregion
}
