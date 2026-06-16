using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Networking;

/// <summary>
/// UI da cena CreateRoom.
/// Liga os campos ao LobbyClientManager para criar a sessão online.
/// </summary>
public class CreateRoomUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField _roomNameInput; // Nome da sala digitado pelo usuário
    [SerializeField] private UnityEngine.UI.Button _publicBtn;
    [SerializeField] private UnityEngine.UI.Button _privateBtn;
    [SerializeField] private UnityEngine.UI.Button _lanBtn;
    [SerializeField] private TMP_Dropdown _maxPlayersDropdown; // Seleção de 2, 3 ou 4 jogadores

    [Header("Feedback")]
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private UnityEngine.UI.Button _createButton;

    [Header("Button Colors")]
    [SerializeField] private Color _selectedColor = Color.white;
    [SerializeField] private Color _deselectedColor= new Color(0.55f, 0.55f, 0.55f); 

    [HideInInspector] public bool _isPublic = false; // Estado de visibilidade da sala
    [HideInInspector] public bool _isLan = false;

    private void Awake()
    {
        HideError();
        
        // Auto-configure based on session mode
        if (NetworkSessionSettings.IsOnlineMode)
        {
            if (_lanBtn != null) _lanBtn.gameObject.SetActive(false);
            _publicBtn.gameObject.SetActive(true);
            _privateBtn.gameObject.SetActive(true);
            ActivatePrivateButton();
        }
        else
        {
            if (_lanBtn != null) _lanBtn.gameObject.SetActive(true);
            _publicBtn.gameObject.SetActive(false);
            _privateBtn.gameObject.SetActive(false);
            ActivateLanButton();
        }
        
        UpdateButtonVisuals();
    }

    // Chamado pelo clique no botão Público
    public void ActivatePublicButton()
    {
        _isPublic = true;
        _isLan = false;
        UpdateButtonVisuals();
    }

    // Chamado pelo clique no botão Privado
    public void ActivatePrivateButton()
    {
        _isPublic = false;
        _isLan = false;
        UpdateButtonVisuals();
    }

    public void ActivateLanButton()
    {
        _isPublic = false;
        _isLan = true;
        UpdateButtonVisuals();
        Debug.Log("[CreateRoomUI] LAN Mode Activated");
    }

    private void UpdateButtonVisuals()
    {
        SetButtonSelected(_publicBtn, _isPublic && !_isLan);
        SetButtonSelected(_privateBtn, !_isPublic && !_isLan);
        SetButtonSelected(_lanBtn, _isLan);
    }

    // Muda a cor visual dos botões de seleção
    private void SetButtonSelected(UnityEngine.UI.Button btn, bool selected)
    {
        if (btn == null) return;
        ColorBlock cb = btn.colors;
        cb.normalColor = selected ? _selectedColor : _deselectedColor;
        cb.highlightedColor = selected ? _selectedColor : _selectedColor;
        cb.pressedColor = selected ? _selectedColor : _deselectedColor;
        cb.selectedColor = selected ? _selectedColor : _deselectedColor;
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

    // Fluxo principal de criação ao clicar no botão Criar
    public async void OnCreateButton()
    {
        HideError();

        string roomName = _roomNameInput != null ? _roomNameInput.text : "";
        int maxPlayers = _maxPlayersDropdown != null ? _maxPlayersDropdown.value + 2 : 4; 
        bool isPublic = _isPublic;

        if (string.IsNullOrWhiteSpace(roomName))
        {
            ShowError("Dá um nome à sala.");
            return;
        }

        if (_createButton != null) _createButton.interactable = false;

        if (_isLan)
        {
            ShowError("Iniciando Host LAN...");
            string lanPin = MatchmakingController.Instance.StartHostLAN(roomName, maxPlayers);
            
            if (!string.IsNullOrEmpty(lanPin))
            {
                // Wait for sync
                float lanTimeout = 5f;
                while ((LobbyClientManager.Instance == null || !LobbyClientManager.Instance.IsSpawned) && lanTimeout > 0)
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    lanTimeout -= 0.1f;
                }

                if (LobbyClientManager.Instance != null && LobbyClientManager.Instance.IsSpawned)
                {
                    LobbyClientManager.Instance.CreateLobby(roomName, false, maxPlayers, lanPin);
                }
            }
            else
            {
                ShowError("Falha ao criar sala LAN.");
                if (_createButton != null) _createButton.interactable = true;
            }
            return;
        }

        // 1. Inicia o Host via Relay e Node.js
        ShowError("Conectando ao Relay...");
string nodeJsCode = await MatchmakingController.Instance.StartHostOnline(roomName, isPublic, maxPlayers);

        if (string.IsNullOrEmpty(nodeJsCode))
        {
            ShowError("Erro ao criar sala online. Verifique a conexão.");
            if (_createButton != null) _createButton.interactable = true;
            return;
        }

        // 2. Aguarda a sincronização do sistema de rede
        float timeout = 10f; 
        while ((LobbyClientManager.Instance == null || !LobbyClientManager.Instance.IsSpawned) && timeout > 0)
        {
            await System.Threading.Tasks.Task.Delay(100);
            timeout -= 0.1f;
        }

        if (LobbyClientManager.Instance != null && LobbyClientManager.Instance.IsSpawned)
        {
            // 3. Inicializa o gerenciador de lobby com os dados da sala
            LobbyClientManager.Instance.CreateLobby(roomName, isPublic, maxPlayers, nodeJsCode);
        }
        else
        {
            ShowError("O sistema de lobby falhou ao iniciar.");
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