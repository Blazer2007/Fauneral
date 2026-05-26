using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI da cena JoinRoom.
/// Mostra um campo de PIN e a lista de salas públicas.
///
/// Setup no Inspector:
///   PinInputField    → TMP_InputField para o PIN
///   JoinByPinButton  → Button "Entrar com PIN"
///   PublicLobbyList  → Transform pai onde os itens da lista são instanciados
///   LobbyItemPrefab  → Prefab de um item da lista (ver comentário abaixo)
///   ErrorText        → TMP_Text para erros
///   RefreshButton    → Button "Actualizar lista"
///
/// O LobbyItemPrefab precisa de ter um LobbyListItem component.
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
        // Pede logo a lista de salas públicas ao entrar na cena
        LobbyClientManager.Instance?.RequestPublicLobbies();
    }

    // ── BOTÕES ─────────────────────────────────────────────────────

    public void OnJoinByPinButton()
    {
        HideError();
        string pin = _pinInputField != null ? _pinInputField.text : "";

        if (_joinByPinButton != null) _joinByPinButton.interactable = false;
        LobbyClientManager.Instance?.JoinLobby(pin);
    }

    public void OnRefreshButton()
    {
        LobbyClientManager.Instance?.RequestPublicLobbies();
    }

    public void OnBackButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMenu");
    }

    // ── CHAMADO PELO LobbyClientManager ───────────────────────────

    /// <summary>
    /// Preenche a lista com as salas públicas recebidas do servidor.
    /// </summary>
    public void PopulatePublicLobbies(List<PublicLobbyEntry> lobbies)
    {
        // Limpa a lista actual
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

        // Re-activa o botão de PIN se estava desactivado por erro anterior
        if (_joinByPinButton != null) _joinByPinButton.interactable = true;
    }

    // ── HELPERS ───────────────────────────────────────────────────

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
