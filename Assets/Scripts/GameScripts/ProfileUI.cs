using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProfileUI : MonoBehaviour
{
    [Header("Input & Text")]
    public TMP_InputField nameInput;
    public TMP_Text animalDisplayText;
    public UnityEngine.UI.Image characterPreview;
    
    [Header("Character Sprites")]
    public Sprite wolfSprite;
    public Sprite bunnySprite;
    
    [Header("Hats Grid")]
    public Transform hatContent;
    public GameObject hatPrefab;
    public CrateData allHatsContainer;

    [Header("Buttons")]
    public Button animalToggleButton;
    public Button saveButton;
    public Button backButton;

    private string _currentAnimal;
    private string _currentHatId;
    private HatCardUI _selectedHatCard;

    private void Start()
    {
        var p = ProfileManager.Instance;
        nameInput.text = p.playerName;
        _currentAnimal = p.selectedAnimal;
        _currentHatId = p.equippedHatId;

        UpdateAnimalDisplay();
        PopulateHats();

        if (animalToggleButton != null) animalToggleButton.onClick.AddListener(ToggleAnimal);
        if (saveButton != null) saveButton.onClick.AddListener(SaveProfile);
        if (backButton != null) backButton.onClick.AddListener(() => Object.FindFirstObjectByType<UiManager>().BackButton("MainMenu"));
    }

    private void ToggleAnimal()
    {
        _currentAnimal = (_currentAnimal == "Wolf") ? "Bunny" : "Wolf";
        UpdateAnimalDisplay();
    }

    private void UpdateAnimalDisplay()
    {
        if (animalDisplayText != null) animalDisplayText.text = "Animal: " + _currentAnimal;
        
        if (characterPreview != null)
        {
            if (_currentAnimal == "Wolf")
                characterPreview.sprite = wolfSprite;
            else
                characterPreview.sprite = bunnySprite;
        }
    }

    private void PopulateHats()
    {
        foreach (Transform child in hatContent) Destroy(child.gameObject);

        if (allHatsContainer == null || hatPrefab == null) return;

        foreach (var hat in allHatsContainer.hats)
        {
            var go = Instantiate(hatPrefab, hatContent);
            var card = go.GetComponent<HatCardUI>();
            bool unlocked = HatInventory.Instance.IsUnlocked(hat);
            card.Setup(hat, unlocked, this);

            if (hat.hatID == _currentHatId)
            {
                OnHatSelected(card, hat.hatID);
            }
        }
    }

    public void OnHatSelected(HatCardUI card, string hatId)
    {
        if (_selectedHatCard != null) _selectedHatCard.SetSelected(false);
        _selectedHatCard = card;
        _selectedHatCard.SetSelected(true);
        _currentHatId = hatId;
    }

    private void SaveProfile()
    {
        ProfileManager.Instance.SavePreferences(nameInput.text, _currentAnimal, _currentHatId);
        Debug.Log("Profile Saved!");
    }
}