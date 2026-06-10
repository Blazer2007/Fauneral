using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Networking;

/// <summary>
/// UI da cena JoinRoom.
/// Mostra um campo de PIN e a lista de salas públicas.
/// </summary>
public class JoinRoomUI : MonoBehaviour
{
    public static JoinRoomUI Instance { get; private set; }

    [Header("Entrada por PIN")]
    [SerializeField] private TMP_InputField _pinInputField;
    [SerializeField] private Button _joinByPinButton;

    [Header("Lista de salas públicas")]
    [SerializeField] private Transform _publicLobbyList;
    [SerializeField] private GameObject _lobbyItemPrefab;

    [Header("Feedback")]
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private TMP_Text _emptyListText;

    private void Awake()
    {
        Instance = this;
        HideError();
    }

    private void OnEnable()
    {
        LobbyClientManager.OnServerError += ShowError;
    }

    private void OnDisable()
    {
        LobbyClientManager.OnServerError -= ShowError;
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // For online, public lobbies might need a different system or just using PIN.
        // For now, let's focus on PIN joining via Node.js.
        // LobbyClientManager.Instance?.RequestPublicLobbies(); 
    }

    public async void OnJoinByPinButton()
    {
        HideError();
        string pin = _pinInputField != null ? _pinInputField.text : "";

        if (string.IsNullOrWhiteSpace(pin))
        {
            ShowError("Introduz um PIN válido.");
            return;
        }

        if (_joinByPinButton != null) _joinByPinButton.interactable = false;

        ShowError("Buscando sala no servidor...");
        bool success = await MatchmakingController.Instance.StartClientOnline(pin.Trim());

        if (success)
        {
            ShowError("Conectando ao Host...");
            
            // Wait for LobbyClientManager to spawn and connect
            float timeout = 10f; // Increase timeout for online connections
            while ((LobbyClientManager.Instance == null || !LobbyClientManager.Instance.IsSpawned) && timeout > 0)
            {
                await System.Threading.Tasks.Task.Delay(250);
                timeout -= 0.25f;
            }

            if (LobbyClientManager.Instance != null && LobbyClientManager.Instance.IsSpawned)
            {
                ShowError("Entrando no Lobby...");
                LobbyClientManager.Instance.JoinLobby(pin.Trim());
            }
            else
            {
                ShowError("Falha na sincronização de rede. Tente novamente.");
                if (_joinByPinButton != null) _joinByPinButton.interactable = true;
            }
        }
        else
        {
            ShowError("Sala não encontrada ou conexão falhou.");
            if (_joinByPinButton != null) _joinByPinButton.interactable = true;
        }
    }

    public void OnRefreshButton()
    {
        // LobbyClientManager.Instance?.RequestPublicLobbies();
    }

    public void OnBackButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMenu");
    }

    public void PopulatePublicLobbies(List<PublicLobbyEntry> lobbies)
    {
        if (_publicLobbyList != null)
        {
            foreach (Transform child in _publicLobbyList)
                Destroy(child.gameObject);
        }

        if (lobbies == null || lobbies.Count == 0)
        {
            if (_emptyListText != null) _emptyListText.gameObject.SetActive(true);
            return;
        }

        if (_emptyListText != null) _emptyListText.gameObject.SetActive(false);

        foreach (PublicLobbyEntry entry in lobbies)
        {
            if (_lobbyItemPrefab == null || _publicLobbyList == null) break;

            GameObject item = Instantiate(_lobbyItemPrefab, _publicLobbyList);
            LobbyListItem listItem = item.GetComponent<LobbyListItem>();
            listItem?.Setup(entry.Pin, entry.RoomName, entry.CurrentPlayers, entry.MaxPlayers);
        }

        if (_joinByPinButton != null) _joinByPinButton.interactable = true;
    }

    private void ShowError(string message)
    {
        if (_errorText != null)
        {
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
        }
        if (_joinByPinButton != null) _joinByPinButton.interactable = true;
    }

    private void HideError()
    {
        if (_errorText != null)
            _errorText.gameObject.SetActive(false);
    }
}
