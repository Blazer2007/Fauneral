using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    public int playerId = 1; // Default ID, should be managed by a login system eventually
    public string playerName = "Player";
    public string selectedAnimal = "Wolf";
    public string equippedHatId = "";

    private WebRequestManager _webRequest;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLocal();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _webRequest = GetComponent<WebRequestManager>();
    }

    public void LoadLocal()
    {
        playerId = PlayerPrefs.GetInt("Profile_ID", 1);
        playerName = PlayerPrefs.GetString("Profile_Name", "Player");
        selectedAnimal = PlayerPrefs.GetString("Profile_Animal", "Wolf");
        equippedHatId = PlayerPrefs.GetString("Profile_Hat", "");
    }

    public void SavePreferences(string newName, string animal, string hatId)
    {
        playerName = newName;
        selectedAnimal = animal;
        equippedHatId = hatId;

        PlayerPrefs.SetInt("Profile_ID", playerId);
        PlayerPrefs.SetString("Profile_Name", playerName);
        PlayerPrefs.SetString("Profile_Animal", selectedAnimal);
        PlayerPrefs.SetString("Profile_Hat", equippedHatId);
        PlayerPrefs.Save();

        // Sync with HatInventory
        if (HatInventory.Instance != null)
        {
            // If the user selects a hat in UI, it should be equipped in the inventory too
            // We find the hat data from the container if needed, but here we just need the ID for the server
        }

        if (_webRequest == null) _webRequest = Object.FindFirstObjectByType<WebRequestManager>();

        if (_webRequest != null)
        {
            List<string> unlocked = new List<string>();
            if (HatInventory.Instance != null)
            {
                unlocked.AddRange(HatInventory.Instance.GetAllUnlocked());
            }

            PlayerData.PlayerPreferences prefs = new PlayerData.PlayerPreferences
            {
                playerId = this.playerId,
                characterId = animal,
                cosmeticId = hatId,
                unlockedHats = unlocked.ToArray()
            };
            StartCoroutine(_webRequest.SavePreferences(prefs));
        }
    }
}