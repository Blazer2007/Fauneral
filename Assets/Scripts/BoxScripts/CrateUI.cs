using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrateUI : MonoBehaviour
{
    public const float SPIN_DURATION = 4.5f;

    [Header("Panels")]
    public GameObject idlePanel;
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
    public float cardWidth = 120f;
    public float cardSpacing = 10f;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable_DISABLED()
    {
        if (CrateSystem.Instance != null)
        {
            CrateSystem.Instance.OnCrateOpenStart += HandleOpenStart;
            CrateSystem.Instance.OnCrateOpenResult += HandleOpenResult;
        }
    }

    private void OnDisable_DISABLED()
    {
        if (CrateSystem.Instance != null)
        {
            CrateSystem.Instance.OnCrateOpenStart -= HandleOpenStart;
            CrateSystem.Instance.OnCrateOpenResult -= HandleOpenResult;
        }
    }

    private void Start()
    {
        CrateSystem.Instance.OnCrateOpenStart += HandleOpenStart;
        CrateSystem.Instance.OnCrateOpenResult += HandleOpenResult;
        ShowIdle();
        closeResultButton.onClick.AddListener(OnCloseResult);
    }

    private void OnDestroy()
    {
        if (CrateSystem.Instance != null)
        {
            CrateSystem.Instance.OnCrateOpenStart -= HandleOpenStart;
            CrateSystem.Instance.OnCrateOpenResult -= HandleOpenResult;
        }
    }

    private void Start_DISABLED()
    {
        ShowIdle();
        closeResultButton.onClick.AddListener(OnCloseResult);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void ShowIdle()
    {
        if (idlePanel != null) idlePanel.SetActive(true);
        spinPanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    private void ShowSpin()
    {
        if (idlePanel != null) idlePanel.SetActive(false);
        spinPanel.SetActive(true);
        resultPanel.SetActive(false);
    }

    private void ShowResult()
    {
        if (idlePanel != null) idlePanel.SetActive(false);
        spinPanel.SetActive(false);
        resultPanel.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────────

    // Recebe o vencedor já sorteado pelo CrateSystem
    private void HandleOpenStart(HatData winner)
    {
        ShowSpin();
        StartCoroutine(SpinAnimation(winner));
    }

    private void HandleOpenResult(HatData hat, bool isNew)
    {
        ShowResult();
        PopulateResultPanel(hat, isNew);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator SpinAnimation(HatData winner)
    {
        // Limpa cards anteriores
        foreach (Transform child in stripContent)
            Destroy(child.gameObject);

        // Monta strip: N aleatórios + winner no centro + alguns depois
        var allItems = new List<HatData>();
        for (int i = 0; i < dummyItemCount; i++)
            allItems.Add(CrateSystem.Instance.RollHat()); // só para visual

        int winnerIndex = allItems.Count;
        allItems.Add(winner); // o vencedor real no sítio certo

        for (int i = 0; i < 8; i++)
            allItems.Add(CrateSystem.Instance.RollHat());

        foreach (var hat in allItems)
            CreateItemCard(hat);

        LayoutRebuilder.ForceRebuildLayoutImmediate(stripContent);
        yield return null;

        float step = cardWidth + cardSpacing;
        float viewW = ((RectTransform)stripContent.parent).rect.width;
        float target = -(winnerIndex * step) + (viewW / 2f) - (cardWidth / 2f);

        stripContent.anchoredPosition = new Vector2(0f, stripContent.anchoredPosition.y);

        float elapsed = 0f;
        float startX = stripContent.anchoredPosition.x;

        while (elapsed < SPIN_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SPIN_DURATION);
            float eased = 1f - Mathf.Pow(1f - t, 4f);
            stripContent.anchoredPosition = new Vector2(
                Mathf.Lerp(startX, target, eased),
                stripContent.anchoredPosition.y);
            yield return null;
        }

        stripContent.anchoredPosition = new Vector2(target, stripContent.anchoredPosition.y);
    }

    private void CreateItemCard(HatData hat)
    {
        GameObject card = Instantiate(itemCardPrefab, stripContent);

        Image hatImg = card.transform.Find("HatImage")?.GetComponent<Image>();
        if (hatImg != null && hat != null && hat.previewSprite != null)
            hatImg.sprite = hat.previewSprite;

        Image border = card.transform.Find("Border")?.GetComponent<Image>();
        if (border != null && hat != null)
            border.color = hat.rarityColor;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void PopulateResultPanel(HatData hat, bool isNew)
    {
        if (hat == null) return;

        resultImage.sprite = hat.previewSprite;
        resultImage.color = hat.previewSprite != null ? Color.white : hat.rarityColor;
        resultNameText.text = hat.displayName;
        resultRarityText.text = hat.rarity.ToString();
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
        ShowIdle();
    }

    // ─────────────────────────────────────────────────────────────────────────

    public void OnOpenCrateButtonPressed()
    {
        if (CrateSystem.Instance == null)
        {
            Debug.LogError("[CrateUI] CrateSystem não encontrado na cena!");
            return;
        }
        CrateSystem.Instance.OpenCrate();
    }
}