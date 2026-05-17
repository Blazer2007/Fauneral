using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public ScriptableCard _Card;
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

        // Atualiza os campos de texto no Editor em tempo real
        if (nameText != null) nameText.text = _Card.name;
        if (rarityText != null) rarityText.text = _Card.rarity;
        if (descriptionText != null) descriptionText.text = _Card.description;
        if (timeText != null) timeText.text = _Card.time.ToString() + " seconds";

        if (image != null) image.sprite = _Card.image.sprite;
    }

    void Start()
    {
        if (_Card != null) Refresh(); // ainda mantém o Refresh no Start para runtime
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
            buffsText.text = string.IsNullOrEmpty(buffsText.text) ? buff: buffsText.text.Trim() + "\n" + buff;
        }

        debuffsText.text = string.Empty;
        for (int i = 0; i < _Card.debuffs.Length; i++)
        {
            string debuff = _Card.debuffs[i].ToString();
            debuffsText.text = string.IsNullOrEmpty(debuffsText.text) ? debuff : debuffsText.text.Trim() + "\n" + debuff;
        }

        timeText.text = _Card.time.ToString() + " seconds";

        if (_Card.isinfinite)
        {
            usesText.text = _Card.uses + " use (Unlimited)";
        }
        else
        {
            usesText.text = _Card.uses + (_Card.uses == 1 ? " use" : " uses") + " (Limited)";
        }
    }
    void Update()
    {
        //Changing cards during gameplay
    }
}
