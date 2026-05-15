using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public ScriptableCard _Card;
    public Sprite sprite;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI buffsText;
    public TextMeshProUGUI debuffsText;
    public TextMeshProUGUI usesText;
    public TextMeshProUGUI timeText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sprite = _Card.sprite;
        nameText.text = _Card.name;
        descriptionText.text = _Card.description;
        rarityText.text = _Card.rarity;
        for(int i = 0; i < _Card.buffs.Length; i++) 
        {
            string buffs = _Card.buffs[i].ToString();
            if(buffsText.text == null)
                buffsText.text = buffs;
            else
                buffsText.text = buffsText.text.Trim() + "\n" + buffs;
            Debug.Log("line"+ i +": "+ buffs);
        }
        for(int i = 0; i < _Card.debuffs.Length; i++) 
        {
            string debuffs = _Card.debuffs[i].ToString();
            if (debuffsText.text == null)
                debuffsText.text = debuffs;
            else
                debuffsText.text = debuffsText.text.Trim() + "\n" + debuffs;
            Debug.Log("line" + i + ": " + debuffs);
        }
        usesText.text = _Card.uses.ToString() + " uses";
        timeText.text = _Card.time.ToString() + " seconds";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
