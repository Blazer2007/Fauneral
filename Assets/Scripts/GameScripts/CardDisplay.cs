using TMPro;
using TarodevController;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Versão actualizada do CardDisplay.
/// Alteração face ao original:
///   - OnPointerClick verifica se estamos em fase de selecção de upgrade
///     (_selectionMode = true) e redireciona para CardSelectionManager.
///   - Em jogo normal (selectionMode = false) comporta-se como antes,
///     chamando CardEffectManager.UseCard().
///
/// O CardSelectionManager activa o modo via CardDisplay.SetSelectionMode(true)
/// ao popular os slots, e desactiva quando o canvas fecha.
/// </summary>
public class CardDisplay : MonoBehaviour, IPointerClickHandler
{
    public ScriptableCard _Card;

    [Tooltip("Índice desta carta no CardDatabase.")]
    public int CardId;

    public Image image;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI buffsText;
    public TextMeshProUGUI debuffsText;
    public TextMeshProUGUI usesText;
    public TextMeshProUGUI timeText;

    // Quando true, o clique vai para o CardSelectionManager (fase de upgrade)
    // Quando false, vai para o CardEffectManager (uso de carta em combate)
    private bool _selectionMode = false;

    public void SetSelectionMode(bool active) => _selectionMode = active;

    // ── UNITY ─────────────────────────────────────────────────────

    void OnValidate()
    {
        if (_Card == null) return;
        if (nameText != null) nameText.text = _Card.name;
        if (rarityText != null) rarityText.text = _Card.rarity;
        if (descriptionText != null) descriptionText.text = _Card.description;
        if (timeText != null) timeText.text = _Card.time.ToString() + " seconds";
        if (image != null) image.sprite = _Card.image != null ? _Card.image.sprite : null;
    }

    void Start()
    {
        if (_Card != null) Refresh();
    }

    // ── REFRESH ───────────────────────────────────────────────────

    public void Refresh()
    {
        if (_Card == null) return;

        if (image != null && _Card.image != null)
            image.sprite = _Card.image.sprite;

        if (nameText != null) nameText.text = _Card.name;
        if (descriptionText != null) descriptionText.text = _Card.description;
        if (rarityText != null) rarityText.text = _Card.rarity;

        if (buffsText != null)
        {
            buffsText.text = string.Empty;
            foreach (var buff in _Card.buffs)
            {
                string line = buff.ToString();
                buffsText.text = string.IsNullOrEmpty(buffsText.text)
                    ? line : buffsText.text.Trim() + "\n" + line;
            }
        }

        if (debuffsText != null)
        {
            debuffsText.text = string.Empty;
            foreach (var debuff in _Card.debuffs)
            {
                string line = debuff.ToString();
                debuffsText.text = string.IsNullOrEmpty(debuffsText.text)
                    ? line : debuffsText.text.Trim() + "\n" + line;
            }
        }

        if (timeText != null)
            timeText.text = _Card.time > 0
                ? _Card.time.ToString() + " seconds"
                : "Permanent";

        if (usesText != null)
            usesText.text = _Card.isinfinite
                ? _Card.uses + " use (Unlimited)"
                : _Card.uses + (_Card.uses == 1 ? " use" : " uses") + " (Limited)";
    }

    // ── CLIQUE ────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_Card == null)
        {
            Debug.LogWarning("[CardDisplay] Nenhuma carta atribuída.");
            return;
        }

        if (_selectionMode)
        {
            // Fase de upgrade entre rondas
            CardSelectionManager.Instance?.PlayerSelectedCard(CardId);
        }
        //else
        //{
        //    // Uso de carta durante o combate
        //    CardEffectManager.Instance?.UseCard(CardId);
        //}
    }
}