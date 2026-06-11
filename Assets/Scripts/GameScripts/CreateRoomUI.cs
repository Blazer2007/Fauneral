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
    [SerializeField] private TMP_Dropdown _maxPlayersDropdown; // Seleção de 2, 3 ou 4 jogadores

    [Header("Feedback")]
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private UnityEngine.UI.Button _createButton;

    [Header("Button Colors")]
    [SerializeField] private Color _selectedColor = Color.white;
    [SerializeField] private Color _deselectedColor= new Color(0.55f, 0.55f, 0.55f); 

    [HideInInspector] public bool _isPublic = false; // Estado de visibilidade da sala

    private void Awake()
    {
        HideError();
        SetButtonSelected(_privateBtn, true); // Privado por padrão
        SetButtonSelected(_publicBtn, false);
    }

    // Chamado pelo clique no botão Público
    public void ActivatePublicButton()
    {
        _isPublic = true;
        SetButtonSelected(_publicBtn, true);
        SetButtonSelected(_privateBtn, false);
    }

    // Chamado pelo clique no botão Privado
    public void ActivatePrivateButton()
    {
        _isPublic = false;
        SetButtonSelected(_privateBtn, true);
        SetButtonSelected(_publicBtn, false);
    }

    // Muda a cor visual dos botões de seleção
    private void SetButtonSelected(UnityEngine.UI.Button btn, bool selected)
    {
        if (btn == null) return;
        ColorBlock cb = btn.colors;
        cb.normalColor = selected ? _selectedColor : _deselectedColor;
        cb.highlightedColor = selected ? _selectedColor : _selectedColor;
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