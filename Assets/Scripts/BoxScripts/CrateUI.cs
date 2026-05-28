using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI de abertura de caixas estilo CS:GO.
///
/// HIERARQUIA DO CANVAS esperada:
///   Canvas
///   └── CrateUI [este script]
///       ├── OpenButton         → OnClick: CrateUI.OnOpenCrateButtonPressed()
///       ├── SpinPanel
///       │   ├── StripMask (com Mask component)
///       │   │   └── StripContent (RectTransform que anima)
///       │   └── CenterMarker (Image — linha/seta no centro)
///       └── ResultPanel
///           ├── ResultImage
///           ├── ResultName (TMP)
///           ├── ResultRarity (TMP)
///           ├── IsNewBadge (desactivado por defeito)
///           └── CloseButton
///
/// ItemCard Prefab (filho do StripContent):
///   ItemCard (120x120)
///   ├── Background (Image)
///   ├── Border (Image — cor de raridade)
///   └── HatImage (Image — sprite do chapéu)
/// </summary>
public class CrateUI : MonoBehaviour
{
    public const float SPIN_DURATION = 4.5f;

    [Header("Panels")]
    public GameObject spinPanel;
    public GameObject resultPanel;

    [Header("Spin Strip")]
    public RectTransform stripContent;
    public GameObject itemCardPrefab;
    public Image centerMarkerImage;

    [Header("Result Panel")]
    public Image resultImage;
    public TextMeshProUGUI resultNameText;
    public TextMeshProUGUI resultRarityText;
    public GameObject resultIsNewBadge;
    public Button closeResultButton;
    public ParticleSystem rarityParticles;

    [Header("Spin Settings")]
    [Range(10, 50)] public int dummyItemCount = 30;
    public float cardWidth   = 120f;
    public float cardSpacing = 10f;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (CrateSystem.Instance != null)
        {
            CrateSystem.Instance.OnCrateOpenStart  += HandleOpenStart;
            CrateSystem.Instance.OnCrateOpenResult += HandleOpenResult;
        }
    }

    private void OnDisable()
    {
        if (CrateSystem.Instance != null)
        {
            CrateSystem.Instance.OnCrateOpenStart  -= HandleOpenStart;
            CrateSystem.Instance.OnCrateOpenResult -= HandleOpenResult;
        }
    }

    private void Start()
    {
        spinPanel.SetActive(false);
        resultPanel.SetActive(false);
        closeResultButton.onClick.AddListener(OnCloseResult);
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Event Handlers

    private void HandleOpenStart()
    {
        spinPanel.SetActive(true);
        resultPanel.SetActive(false);
        StartCoroutine(SpinAnimation());
    }

    private void HandleOpenResult(HatData hat, bool isNew)
    {
        spinPanel.SetActive(false);
        resultPanel.SetActive(true);
        PopulateResultPanel(hat, isNew);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Spin Animation

    private IEnumerator SpinAnimation()
    {
        // 1. Sorteia o vencedor para o posicionar no strip
        HatData winner = CrateSystem.Instance.RollHat();

        // 2. Limpa cards anteriores
        foreach (Transform child in stripContent)
            Destroy(child.gameObject);

        // 3. Monta a lista: N aleatórios + winner + alguns depois
        var allItems = new List<HatData>();
        for (int i = 0; i < dummyItemCount; i++)
            allItems.Add(CrateSystem.Instance.RollHat());

        int winnerIndex = allItems.Count;
        allItems.Add(winner);

        for (int i = 0; i < 8; i++)
            allItems.Add(CrateSystem.Instance.RollHat());

        foreach (var hat in allItems)
            CreateItemCard(hat);

        // 4. Força rebuild antes de animar
        LayoutRebuilder.ForceRebuildLayoutImmediate(stripContent);
        yield return null;

        // 5. Posição target: winner centrado na janela
        float step   = cardWidth + cardSpacing;
        float viewW  = ((RectTransform)stripContent.parent).rect.width;
        float target = -(winnerIndex * step) + (viewW / 2f) - (cardWidth / 2f);

        stripContent.anchoredPosition = new Vector2(0f, stripContent.anchoredPosition.y);

        // 6. Anima com EaseOutQuart (rápido → trava suave, igual ao CS:GO)
        float elapsed = 0f;
        float startX  = stripContent.anchoredPosition.x;

        while (elapsed < SPIN_DURATION)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / SPIN_DURATION);
            float eased = 1f - Mathf.Pow(1f - t, 4f);
            stripContent.anchoredPosition = new Vector2(Mathf.Lerp(startX, target, eased),
                                                        stripContent.anchoredPosition.y);
            yield return null;
        }

        stripContent.anchoredPosition = new Vector2(target, stripContent.anchoredPosition.y);
    }

    private void CreateItemCard(HatData hat)
    {
        GameObject card = Instantiate(itemCardPrefab, stripContent);

        Image hatImg = card.transform.Find("HatImage")?.GetComponent<Image>();
        if (hatImg != null && hat.previewSprite != null)
            hatImg.sprite = hat.previewSprite;

        Image border = card.transform.Find("Border")?.GetComponent<Image>();
        if (border != null)
            border.color = hat.rarityColor;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Result Panel

    private void PopulateResultPanel(HatData hat, bool isNew)
    {
        if (hat == null) return;

        resultImage.sprite     = hat.previewSprite;
        resultImage.color      = hat.previewSprite != null ? Color.white : hat.rarityColor;
        resultNameText.text    = hat.displayName;
        resultRarityText.text  = hat.rarity.ToString();  // "C", "B", "A" ou "S"
        resultRarityText.color = hat.rarityColor;
        resultIsNewBadge.SetActive(isNew);

        if (rarityParticles != null)
        {
            var main = rarityParticles.main;
            main.startColor = hat.rarityColor;
            rarityParticles.Play();
        }
    }

    private void OnCloseResult()
    {
        if (rarityParticles != null) rarityParticles.Stop();
        resultPanel.SetActive(false);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Liga ao botão "Abrir Caixa" no Inspector.</summary>
    public void OnOpenCrateButtonPressed()
    {
        CrateSystem.Instance.OpenCrate();
    }
}
