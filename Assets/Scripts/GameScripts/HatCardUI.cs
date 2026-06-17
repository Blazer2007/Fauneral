using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.EventSystems;

public class HatCardUI : MonoBehaviour, IPointerClickHandler
{
    public Image hatIcon;
    public TMP_Text nameText;
    public TMP_Text rarityText;
    public GameObject checkBadge;
    public GameObject lockOverlay;

    private HatData _data;
    private bool _isUnlocked;
    private HatsScrollerUI _parent;

    public void Setup(HatData data, bool isUnlocked, HatsScrollerUI parent)
    {
        _data = data;
        _isUnlocked = isUnlocked;
        _parent = parent;

        hatIcon.sprite = data.previewSprite;
        nameText.text = data.displayName;
        rarityText.text = data.rarity.ToString();
        lockOverlay.SetActive(!isUnlocked);
        checkBadge.SetActive(false);
    }

    public string GetDisplayName() => _data.displayName;

    public void SetSelected(bool selected)
    {
        checkBadge.SetActive(selected);
        // muda a cor da borda via outline ou troca de material
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isUnlocked) return;
        _parent.OnCardSelected(this, _data.hatID);
    }
}