using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Networking;

/// <summary>
/// UI da cena JoinRoom.
/// Mostra um campo de PIN e a lista de salas públicas obtidas via Node.js.
/// </summary>
public class JoinRoomUI : MonoBehaviour
{
    public static JoinRoomUI Instance { get; private set; }

    [Header("Entrada por PIN (Online)")]
    [SerializeField] private GameObject _pinContainer; // Parent of PIN input field and join button
    [SerializeField] private TMP_InputField _pinInputField; // Campo onde o jogador digita o PIN
    [SerializeField] private Button _joinByPinButton;

    [Header("Entrada por IP (LAN)")]
    [SerializeField] private GameObject _ipContainer; // Container para entrada manual de IP
    [SerializeField] private TMP_InputField _ipInputField; // Campo de IP
    [SerializeField] private Button _joinByIpButton;

    [Header("Lista de salas públicas")]
    [SerializeField] private Transform _publicLobbyList; // Container (Content do ScrollView) para os itens da lista
    [SerializeField] private GameObject _lobbyItemPrefab; // Prefab de cada linha da tabela

    [Header("Feedback")]
    [SerializeField] private TMP_Text _errorText; // Texto para exibir mensagens de erro e status
    [SerializeField] private TMP_Text _emptyListText; // Texto "Nenhuma sala encontrada"

    private void Awake()
    {
        Instance = this;
        HideError();

        // Configura visibilidade baseada no modo
        bool isOnline = NetworkSessionSettings.IsOnlineMode;
        if (_pinContainer != null) _pinContainer.SetActive(isOnline);
        if (_ipContainer != null) _ipContainer.SetActive(!isOnline);
    }

    private void OnEnable()
    {
        // Escuta erros vindos do sistema de rede
        LobbyClientManager.OnServerError += ShowError;
    }

    private void OnDisable()
    {
        LobbyClientManager.OnServerError -= ShowError;
        if (Instance == this) Instance = null;
    }

    private async void Start()
    {
        // Pequeno atraso para garantir inicialização
        await System.Threading.Tasks.Task.Delay(500);
        
        Debug.Log("[JoinRoomUI] Solicitando lista de lobbies públicos ao iniciar.");
        RefreshPublicLobbies(); // Busca a lista automaticamente ao abrir a tela

        // LAN Discovery - Só inicia se não estivermos no modo Online
        if (LANDiscovery.Instance != null && !NetworkSessionSettings.IsOnlineMode)
        {
            LANDiscovery.Instance.OnServerFound += HandleLanServerFound;
            LANDiscovery.Instance.StartSearching();
        }
    }

    private void HandleLanServerFound(string ip, string roomName, string pin)
    {
        // Se estivermos em modo Online, ignoramos anúncios LAN
        if (NetworkSessionSettings.IsOnlineMode) return;

        // Add to the list if not already there
        // For simplicity, we'll just add it as a PublicLobbyEntry with a special marker or just use the IP as the "PIN"
        // But the listItem needs to know it's LAN to call the right join method.

        // Let's create a temporary entry
        var entry = new PublicLobbyEntry {
            Pin = pin,
            RoomName = "[LAN] " + roomName,
            CurrentPlayers = 1, // We don't know the exact count easily from broadcast without more data
            MaxPlayers = 4
        };
        
        // We need to store that this specific PIN/Entry is LAN
        if (!_lanServers.ContainsKey(pin))
        {
            _lanServers[pin] = ip;
            AddLanEntryToList(entry);
        }
    }

    private Dictionary<string, string> _lanServers = new Dictionary<string, string>();

    private void AddLanEntryToList(PublicLobbyEntry entry)
    {
        if (_lobbyItemPrefab == null || _publicLobbyList == null) return;

        GameObject item = Instantiate(_lobbyItemPrefab, _publicLobbyList);
        LobbyListItem listItem = item.GetComponent<LobbyListItem>();
        // We might need to override the button behavior for LAN items
        listItem?.Setup(entry.Pin, entry.RoomName, entry.CurrentPlayers, entry.MaxPlayers);
        
        // Since LobbyListItem probably calls JoinRoomUI.Instance.JoinRoom(pin)
        // we need to make JoinRoom handle LAN pins
    }

    private void OnDestroy()
    {
        if (LANDiscovery.Instance != null)
        {
            LANDiscovery.Instance.OnServerFound -= HandleLanServerFound;
            LANDiscovery.Instance.StopAll();
        }
    }

    public void OnJoinByIpButton()
    {
        if (_ipInputField == null) return;
        string ip = _ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            ShowError("Introduz um IP válido.");
            return;
        }
        JoinLanRoom(ip, "AUTO");
    }

    public void OnJoinByPinButton()
    {
        if (_pinInputField == null) return;
        string pin = _pinInputField.text.Trim();
        if (string.IsNullOrEmpty(pin))
        {
            ShowError("Introduz um PIN.");
            return;
        }
        JoinRoom(pin);
    }

    // Processo de conexão unificado (Relay + Netcode + Lobby)
public async void JoinRoom(string pin)
    {
        HideError();
        if (_joinByPinButton != null) _joinByPinButton.interactable = false;

        // Check if it's a LAN server
        if (_lanServers.TryGetValue(pin, out string ip))
        {
            JoinLanRoom(ip, pin);
            return;
        }

        ShowError($"Conectando à sala {pin}...");
        // 1. Busca código Relay no Node.js e conecta
        bool success = await MatchmakingController.Instance.StartClientOnline(pin);

        if (success)
        {
            ShowError("Entrando no Lobby...");
            // Define o PIN imediatamente para evitar ser expulso da cena de Lobby
            LobbySessionData.Pin = pin; 
            LobbyClientManager.Instance.JoinLobby(pin);
        }
else
        {
            ShowError("Sala não encontrada ou conexão falhou.");
            if (_joinByPinButton != null) _joinByPinButton.interactable = true;
        }
    }

    public async void JoinLanRoom(string ip, string pin)
    {
        ShowError($"Conectando LAN {ip}...");
        bool success = await MatchmakingController.Instance.StartClientLAN(ip);

        if (success)
        {
            ShowError("Entrando no Lobby...");
            // Se for AUTO, usamos o PIN que o servidor provavelmente tem
            LobbySessionData.Pin = pin;
            LobbyClientManager.Instance.JoinLobby(pin);
        }
else
        {
            ShowError("Falha na conexão LAN.");
            if (_joinByPinButton != null) _joinByPinButton.interactable = true;
        }
    }

    // Atualiza a tabela chamando a API do Node.js
    public void OnRefreshButton()
    {
        Debug.Log("[JoinRoomUI] Atualizando lista de lobbies públicos.");
        RefreshPublicLobbies();
    }

    private void RefreshPublicLobbies()
    {
        if (_emptyListText != null) { _emptyListText.text = "Buscando..."; _emptyListText.gameObject.SetActive(true); }
        
        // Limpa a lista visual antes de começar a busca
        if (_publicLobbyList != null)
        {
            foreach (Transform child in _publicLobbyList)
                Destroy(child.gameObject);
        }

        _lanServers.Clear(); // Clear LAN cache on refresh

        // If LAN mode, we don't necessarily call Node.js, but we can call both just in case
        // But for clarity, if NOT online, maybe only show LAN?
        // User wants separation, so let's stick to the mode.

        if (NetworkSessionSettings.IsOnlineMode)
        {
            DiscoveryManager.Instance.GetPublicRooms((rooms) => {
                if (rooms == null)
                {
                    PopulatePublicLobbies(new List<PublicLobbyEntry>());
                    return;
                }

                List<PublicLobbyEntry> entries = new List<PublicLobbyEntry>();
                foreach (var room in rooms)
                {
                    entries.Add(new PublicLobbyEntry {
                        Pin = room.code,
                        RoomName = room.name,
                        CurrentPlayers = room.currentPlayers,
                        MaxPlayers = room.maxPlayers
                    });
                }
                PopulatePublicLobbies(entries); // Preenche a UI
            });
        }
        else
        {
            // Just populate with what LANDiscovery found so far (it keeps running)
            // The items are added via AddLanEntryToList dynamically.
            if (_lanServers.Count == 0)
            {
                if (_emptyListText != null) _emptyListText.text = "Procurando partidas locais...";
            }
            else
            {
                if (_emptyListText != null) _emptyListText.gameObject.SetActive(false);
            }
        }
    }

    public void OnBackButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMenu");
    }

    // Instancia os prefabs na tabela conforme os dados recebidos
    public void PopulatePublicLobbies(List<PublicLobbyEntry> lobbies)
    {
        Debug.Log($"[JoinRoomUI] Populando lista com {lobbies.Count} lobbies.");
        
        if (_publicLobbyList != null)
        {
            foreach (Transform child in _publicLobbyList)
                Destroy(child.gameObject);
        }

        if (lobbies == null || lobbies.Count == 0)
        {
            if (_emptyListText != null) 
            {
                _emptyListText.text = "Nenhuma sala pública disponível.";
                _emptyListText.gameObject.SetActive(true);
            }
            if (_joinByPinButton != null) _joinByPinButton.interactable = true;
            return;
        }

        if (_emptyListText != null) _emptyListText.gameObject.SetActive(false);

        foreach (PublicLobbyEntry entry in lobbies)
        {
            if (_lobbyItemPrefab == null || _publicLobbyList == null) break;

            Debug.Log($"[JoinRoomUI] Sala: {entry.RoomName}, PIN: {entry.Pin}, Jogadores: {entry.CurrentPlayers}/{entry.MaxPlayers}");
            
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
