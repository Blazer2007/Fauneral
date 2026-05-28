using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
/// <summary>
/// UI da cena CreateRoom.
/// Liga os campos ao LobbyClientManager e mostra erros do servidor.
///
/// Setup no Inspector:
///   RoomNameInput    → TMP_InputField com o nome da sala
///   PublicToggle     → Toggle (on = pública, off = privada)
///   MaxPlayersSlider → Slider (min=2, max=16)
///   MaxPlayersLabel  → TMP_Text que mostra o valor actual do slider
///   CreateButton     → Button "Criar Sala"
///   BackButton       → Button "Voltar"
///   ErrorText        → TMP_Text para erros (inicialmente inactivo)
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
    private Color _highlightedColor = new Color(0.55f, 0.55f, 0.55f); // cinzento escuro

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

    /// <summary>Muda a cor de fundo de um botão consoante está seleccionado ou não.</summary>
    private void SetButtonSelected(UnityEngine.UI.Button btn, bool selected)
    {
        if (btn == null) return;

        // Usa o ColorBlock nativo do botão para não perder as cores de Hover/Pressed
        ColorBlock cb = btn.colors;
        cb.normalColor = selected ? _selectedColor : _deselectedColor;
        cb.highlightedColor = selected ? _selectedColor : _highlightedColor;
        btn.colors = cb;
    }

    private void OnEnable()
    {
        LobbyClientManager.OnServerError += ShowError;
    }

    private void OnDisable()
    {
        LobbyClientManager.OnServerError -= ShowError;
    }

    // ── BOTÕES ─────────────────────────────────────────────────────

    public void OnCreateButton()
    {
        HideError();

        string roomName = _roomNameInput != null ? _roomNameInput.text : "";
        int maxPlayers = _maxPlayersDropdown != null ? (int)_maxPlayersDropdown.value : 4;
        bool isPublic = _isPublic;

        if (string.IsNullOrWhiteSpace(roomName))
        {
            ShowError("Dá um nome à sala.");
            return;
        }

        if (_createButton != null) _createButton.interactable = false;

        LobbyClientManager.Instance?.CreateLobby(roomName, isPublic, maxPlayers);
    }

    public void OnBackButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMenu");
    }

    // ── HELPERS ───────────────────────────────────────────────────

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