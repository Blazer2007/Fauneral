using TMPro;
using TarodevController;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A carta é clicável directamente — sem Button filho.
/// Requer na cena: EventSystem (já existe).
/// Requer no GameObject da carta: qualquer Graphic com Raycast Target = true
/// (a Image principal já serve, desde que Raycast Target esteja activo).
/// </summary>
public class CardDisplay : MonoBehaviour, IPointerClickHandler
{
    public ScriptableCard _Card;

    [Tooltip("Índice desta carta no CardDatabase. Definido por quem instancia o CardDisplay.")]
    public int CardId;

    public Image image;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI buffsText;
    public TextMeshProUGUI debuffsText;
    public TextMeshProUGUI usesText;
    public TextMeshProUGUI timeText;

    void OnValidate()
    {
        if (_Card == null) return;
        if (nameText != null) nameText.text = _Card.name;
        if (rarityText != null) rarityText.text = _Card.rarity;
        if (descriptionText != null) descriptionText.text = _Card.description;
        if (timeText != null) timeText.text = _Card.time.ToString() + " seconds";
        if (image != null) image.sprite = _Card.image.sprite;
    }

    void Start()
    {
        if (_Card != null) Refresh();
    }

    void Refresh()
    {
        image.sprite = _Card.image.sprite;
        nameText.text = _Card.name;
        descriptionText.text = _Card.description;
        rarityText.text = _Card.rarity;

        buffsText.text = string.Empty;
        for (int i = 0; i < _Card.buffs.Length; i++)
        {
            string buff = _Card.buffs[i].ToString();
            buffsText.text = string.IsNullOrEmpty(buffsText.text)
                ? buff : buffsText.text.Trim() + "\n" + buff;
        }

        debuffsText.text = string.Empty;
        for (int i = 0; i < _Card.debuffs.Length; i++)
        {
            string debuff = _Card.debuffs[i].ToString();
            debuffsText.text = string.IsNullOrEmpty(debuffsText.text)
                ? debuff : debuffsText.text.Trim() + "\n" + debuff;
        }

        timeText.text = _Card.time.ToString() + " seconds";
        usesText.text = _Card.isinfinite
            ? _Card.uses + " use (Unlimited)"
            : _Card.uses + (_Card.uses == 1 ? " use" : " uses") + " (Limited)";
    }

    // ── CLIQUE ────────────────────────────────────────────────────

    /// <summary>
    /// Chamado automaticamente pelo EventSystem quando o jogador clica na carta.
    /// Não precisas de ligar nada no Inspector.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_Card == null)
        {
            Debug.LogWarning("[CardDisplay] Nenhuma carta atribuída.");
            return;
        }

        CardEffectManager.Instance?.UseCard(CardId);
    }
}