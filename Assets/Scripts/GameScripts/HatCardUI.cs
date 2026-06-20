using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class HatCardUI : MonoBehaviour, IPointerClickHandler
{
    public Image hatIcon;
    public TMP_Text nameText;
    public GameObject lockOverlay;
    public GameObject selectedIndicator;

    private HatData _data;
    private bool _isUnlocked;
    private ProfileUI _profileUI;

    public void Setup(HatData data, bool isUnlocked, ProfileUI profileUI)
    {
        _data = data;
        _isUnlocked = isUnlocked;
        _profileUI = profileUI;

        if (hatIcon != null) hatIcon.sprite = data.previewSprite;
        if (nameText != null) nameText.text = data.displayName;
        if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);
        if (selectedIndicator != null) selectedIndicator.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isUnlocked)
        {
            Debug.Log("Hat is locked!");
            return;
        }
        
        _profileUI.OnHatSelected(this, _data.hatID);
    }
}
