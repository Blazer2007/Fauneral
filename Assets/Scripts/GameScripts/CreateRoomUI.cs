using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Networking;

/// <summary>
/// UI da cena CreateRoom.
/// Liga os campos ao LobbyClientManager e mostra erros do servidor.
/// </summary>
public class CreateRoomUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private UnityEngine.UI.Button _publicBtn;
    [SerializeField] private UnityEngine.UI.Button _privateBtn;
    [SerializeField] private TMP_Dropdown _maxPlayersDropdown;

    [Header("Feedback")]
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private UnityEngine.UI.Button _createButton;

    [Header("Button Colors")]
    [SerializeField] private Color _selectedColor = Color.white;
    [SerializeField] private Color _deselectedColor= new Color(0.55f, 0.55f, 0.55f); // cinzento escuro
    private Color _highlightedColor = new Color(0.55f, 0.55f, 0.55f);

    [HideInInspector] public bool _isPublic = false;

    private void Awake()
    {
        HideError();
        SetButtonSelected(_privateBtn, true);
        SetButtonSelected(_publicBtn, false);
    }
    public void ActivatePublicButton()
    {
        _isPublic = true;
        SetButtonSelected(_publicBtn, true);
        SetButtonSelected(_privateBtn, false);
    }

    public void ActivatePrivateButton()
    {
        _isPublic = false;
        SetButtonSelected(_privateBtn, true);
        SetButtonSelected(_publicBtn, false);
    }

    private void SetButtonSelected(UnityEngine.UI.Button btn, bool selected)
    {
        if (btn == null) return;
        ColorBlock cb = btn.colors;
        cb.normalColor = selected ? _selectedColor : _deselectedColor;
        cb.highlightedColor = selected ? _selectedColor : _highlightedColor;
        btn.colors = cb;

        Debug.Log($"Button '{btn.name}' set to {(selected ? "selected" : "deselected")}.");
    }

    private void OnEnable()
    {
        LobbyClientManager.OnServerError += ShowError;
    }

    private void OnDisable()
    {
        LobbyClientManager.OnServerError -= ShowError;
    }

    public async void OnCreateButton()
    {
        HideError();

        string roomName = _roomNameInput != null ? _roomNameInput.text : "";
        int maxPlayers = _maxPlayersDropdown != null ? _maxPlayersDropdown.value + 2 : 4; // Dropdown index + offset if needed
        bool isPublic = _isPublic;

        if (string.IsNullOrWhiteSpace(roomName))
        {
            ShowError("Dá um nome à sala.");
            return;
        }

        if (_createButton != null) _createButton.interactable = false;

        ShowError("Connecting to Relay...");
        string nodeJsCode = await MatchmakingController.Instance.StartHostOnline(maxPlayers);

        if (string.IsNullOrEmpty(nodeJsCode))
        {
            ShowError("Failed to create online room. Check connection.");
            if (_createButton != null) _createButton.interactable = true;
            return;
        }

        float timeout = 10f; // Aumentado para 10s para maior estabilidade online
        while ((LobbyClientManager.Instance == null || !LobbyClientManager.Instance.IsSpawned) && timeout > 0)
        {
            await System.Threading.Tasks.Task.Delay(100);
            timeout -= 0.1f;
        }

        if (LobbyClientManager.Instance != null && LobbyClientManager.Instance.IsSpawned)
        {
            LobbyClientManager.Instance.CreateLobby(roomName, isPublic, maxPlayers, nodeJsCode);
        }
        else
        {
            ShowError("Lobby system failed to spawn. Check network.");
            if (_createButton != null) _createButton.interactable = true;
        }
    }

    public void OnBackButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMenu");
    }

    private void ShowError(string message)
    {
        if (_errorText != null)
        {
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
        }
        if (_createButton != null) _createButton.interactable = true;
    }

    private void HideError()
    {
        if (_errorText != null)
            _errorText.gameObject.SetActive(false);
    }
}