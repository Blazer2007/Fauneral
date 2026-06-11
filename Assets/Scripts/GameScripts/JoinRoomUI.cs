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

    [Header("Entrada por PIN")]
    [SerializeField] private TMP_InputField _pinInputField; // Campo onde o jogador digita o PIN
    [SerializeField] private Button _joinByPinButton;

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
    }

    // Chamado pelo botão de entrar manual
    public void OnJoinByPinButton()
    {
        string pin = _pinInputField != null ? _pinInputField.text : "";
        if (string.IsNullOrWhiteSpace(pin))
        {
            ShowError("Introduz um PIN válido.");
            return;
        }
        JoinRoom(pin.Trim());
    }

    // Processo de conexão unificado (Relay + Netcode + Lobby)
    public async void JoinRoom(string pin)
    {
        HideError();
        if (_joinByPinButton != null) _joinByPinButton.interactable = false;

        ShowError($"Conectando à sala {pin}...");
        // 1. Busca código Relay no Node.js e conecta
        bool success = await MatchmakingController.Instance.StartClientOnline(pin);

        if (success)
        {
            ShowError("Sincronizando rede...");
            
            // 2. Aguarda o sistema de rede sincronizar os objetos do Host
            float timeout = 10f;
            while ((LobbyClientManager.Instance == null || !LobbyClientManager.Instance.IsSpawned) && timeout > 0)
            {
                await System.Threading.Tasks.Task.Delay(250);
                timeout -= 0.25f;
            }

            if (LobbyClientManager.Instance != null && LobbyClientManager.Instance.IsSpawned)
            {
                ShowError("Entrando no Lobby...");
                // 3. Solicita entrada no lobby via RPC
                LobbyClientManager.Instance.JoinLobby(pin);
            }
            else
            {
                ShowError("Falha na sincronização. Tente novamente.");
                if (_joinByPinButton != null) _joinByPinButton.interactable = true;
            }
        }
        else
        {
            ShowError("Sala não encontrada ou conexão falhou.");
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
        
        // Chama o DiscoveryManager para buscar dados via HTTP
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
