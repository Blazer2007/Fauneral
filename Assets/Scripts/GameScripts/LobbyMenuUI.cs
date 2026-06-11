using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI da cena LobbyMenu.
///
/// Hierarquia esperada por PlayerSpawn:
///   PlayerSpawn1
///     ConnectedPlayer          ← activo quando slot ocupado
///       ReadyIndicator         ← Image (verde/vermelho)
///       PlayerCharacter        ← Image sprite do jogador
///       ReadyButton            ← Button "Ready" (só visível para o dono do slot)
///     DisconnectedPlayer       ← activo quando slot vazio
///       WaitingForPlayer       ← TMP_Text
///
/// Setup no Inspector deste script (LobbyMenuUI):
///   _playerSpawns[]    → arrastar PlayerSpawn1..4 por ordem
///   _roomNameText      → TMP_Text nome da sala
///   _pinText           → TMP_Text PIN
///   _playerCountText   → TMP_Text "X / Y jogadores"
///   _visibilityText    → TMP_Text "Pública"/"Privada"
///   _startGameButton   → Button "Start Game" (começa inactivo)
///   _readySprite       → Sprite verde
///   _notReadySprite    → Sprite vermelho
///   _playerCharSprite  → Sprite genérico do personagem
/// </summary>
public class LobbyMenuUI : MonoBehaviour
{
    public static LobbyMenuUI Instance { get; private set; }

    [Header("Player Spawns (ordem: Spawn1, Spawn2, etc...)")]
    [SerializeField] private List<PlayerSpawnUI> _playerSpawns = new List<PlayerSpawnUI>();

    [Header("Info da sala")]
    [SerializeField] private TMP_Text _roomNameText;
    [SerializeField] private TMP_Text _pinText;
    [SerializeField] private TMP_Text _playerCountText;
    [SerializeField] private TMP_Text _visibilityText;

    [Header("Botão StartGame (só criador, só quando todos prontos)")]
    [SerializeField] private Button _startGameButton;

    [Header("Sprites")]
    [SerializeField] private Sprite _readySprite;
    [SerializeField] private Sprite _notReadySprite;
    [SerializeField] private Sprite _playerCharSprite;

    // Estado local do botão Ready do cliente local (toggle)
    private bool _iAmReady = false;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (!LobbySessionData.IsInLobby)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMenu");
            return;
        }

        if (_roomNameText != null) _roomNameText.text = LobbySessionData.RoomName;
        if (_pinText != null) _pinText.text = $"PIN: {LobbySessionData.Pin}";
        if (_visibilityText != null) _visibilityText.text = LobbySessionData.IsPublic ? "Pública" : "Privada";

        // StartGame: sempre inactivo no início (servidor activa quando todos prontos)
        SetStartGameVisible(false);

        // Inicializa todos os spawns como vazios
        foreach (var s in _playerSpawns) s?.SetDisconnected();

        // Aplica estado guardado se já existir (ex: refresh de cena)
        if (!string.IsNullOrEmpty(LobbySessionData.SlotsData))
            RefreshFullState(LobbySessionData.SlotsData, LobbySessionData.ReadyData,
                LobbySessionData.CurrentPlayers, LobbySessionData.MaxPlayers);
    }

    // ── API PÚBLICA (chamada pelo LobbyClientManager) ──────────────

    /// <summary>
    /// Chamado quando alguém entra ou sai — atualiza slots E ready states.
    /// Qualquer mudança de slot repõe o ready para false (quem entrou começa não pronto).
    /// </summary>
    public void RefreshFullState(string slotsData, string readyData,
        int currentPlayers, int maxPlayers)
    {
        if (_playerCountText != null)
            _playerCountText.text = $"{currentPlayers} / {maxPlayers} jogadores";

        ulong[] slots = ParseUlongs(slotsData);
        bool[] ready = ParseBools(readyData);
        ulong myId = LobbySessionData.MyClientId;

        for (int i = 0; i < _playerSpawns.Count; i++)
        {
            PlayerSpawnUI spawn = _playerSpawns[i];
            if (spawn == null) continue;

            if (i >= maxPlayers || i >= slots.Length || slots[i] == ulong.MaxValue)
            {
                spawn.SetDisconnected();
                continue;
            }

            bool isMe = slots[i] == myId;
            bool isReady = (ready != null && i < ready.Length) && ready[i];
            Sprite rdySprite = isReady ? _readySprite : _notReadySprite;

            spawn.SetConnected(_playerCharSprite, rdySprite, isMe);

            // Botão Ready: visível só para o dono do slot
            spawn.SetReadyButtonVisible(isMe);

            // Sincroniza o texto do botão com o estado actual
            if (isMe)
            {
                _iAmReady = isReady;
                spawn.SetReadyButtonLabel(_iAmReady ? "Cancelar" : "Ready");
            }
        }
    }

    /// <summary>
    /// Chamado quando só os ready states mudam (ninguém entrou/saiu).
    /// </summary>
    public void RefreshReadyStates(string readyData)
    {
        bool[] ready = ParseBools(readyData);
        ulong myId = LobbySessionData.MyClientId;
        ulong[] slots = ParseUlongs(LobbySessionData.SlotsData);

        for (int i = 0; i < _playerSpawns.Count; i++)
        {
            PlayerSpawnUI spawn = _playerSpawns[i];
            if (spawn == null) continue;
            if (i >= slots.Length || slots[i] == ulong.MaxValue) continue;

            bool isReady = (ready != null && i < ready.Length) && ready[i];
            spawn.SetReadyIndicator(isReady ? _readySprite : _notReadySprite);

            if (slots[i] == myId)
            {
                _iAmReady = isReady;
                spawn.SetReadyButtonLabel(_iAmReady ? "Cancelar" : "Ready");
            }
        }
    }

    /// <summary>Activa/desactiva o botão StartGame (chamado pelo servidor via ClientRpc).</summary>
    public void SetStartGameVisible(bool visible)
    {
        if (_startGameButton != null)
            _startGameButton.gameObject.SetActive(visible && LobbySessionData.IsCreator);
    }

    // ── BOTÕES ─────────────────────────────────────────────────────

    /// <summary>
    /// Chamado pelo botão Ready de qualquer PlayerSpawn.
    /// Como cada botão pertence ao slot do cliente local, não há ambiguidade.
    /// </summary>
    public void OnReadyButton()
    {
        _iAmReady = !_iAmReady;
        LobbyClientManager.Instance?.SetReady(_iAmReady);
        // O visual é actualizado quando o servidor responder com ReadyStateClientRpc
    }

    public void OnStartGameButton()
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    public void OnLeaveButton()
    {
        LobbyClientManager.Instance?.LeaveLobby();
        UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMenu");
    }

    // ── HELPERS ───────────────────────────────────────────────────

    private static ulong[] ParseUlongs(string data)
    {
        if (string.IsNullOrEmpty(data)) return new ulong[0];
        string[] parts = data.Split(',');
        ulong[] r = new ulong[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            r[i] = ulong.TryParse(parts[i], out ulong v) ? v : ulong.MaxValue;
        return r;
    }

    private static bool[] ParseBools(string data)
    {
        if (string.IsNullOrEmpty(data)) return new bool[0];
        string[] parts = data.Split(',');
        bool[] r = new bool[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            r[i] = bool.TryParse(parts[i], out bool v) && v;
        return r;
    }
}

// ═══════════════════════════════════════════════════════════════════
//  PlayerSpawnUI — um por PlayerSpawn, ligado no Inspector
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Dados de UI de um slot de jogador.
/// É uma classe serializada (não MonoBehaviour) — aparece como lista expansível
/// no Inspector do LobbyMenuUI.
///
/// Por cada entrada liga:
///   ConnectedPanel    → GameObject "ConnectedPlayer"
///   DisconnectedPanel → GameObject "DisconnectedPlayer"
///   ReadyIndicator    → Image dentro de ConnectedPlayer
///   PlayerCharacter   → Image dentro de ConnectedPlayer
///   ReadyButton       → Button "Ready" dentro de ConnectedPlayer
///   ReadyButtonLabel  → TMP_Text do texto do botão Ready
/// </summary>
[System.Serializable]
public class PlayerSpawnUI
{
    [Tooltip("GameObject 'ConnectedPlayer'")]
    public GameObject ConnectedPanel;

    [Tooltip("GameObject 'DisconnectedPlayer'")]
    public GameObject DisconnectedPanel;

    [Tooltip("Image 'ReadyIndicator' dentro de ConnectedPlayer")]
    public Image ReadyIndicator;

    [Tooltip("Image 'PlayerCharacter' dentro de ConnectedPlayer")]
    public Image PlayerCharacter;

    [Tooltip("Button 'ReadyButton' dentro de ConnectedPlayer")]
    public Button ReadyButton;

    [Tooltip("TMP_Text do texto dentro do ReadyButton")]
    public TMP_Text ReadyButtonLabel;

    // ── Métodos ───────────────────────────────────────────────────

    public void SetConnected(Sprite charSprite, Sprite readySprite, bool isMe)
    {
        if (ConnectedPanel != null) ConnectedPanel.SetActive(true);
        if (DisconnectedPanel != null) DisconnectedPanel.SetActive(false);
        if (PlayerCharacter != null && charSprite != null) PlayerCharacter.sprite = charSprite;
        if (ReadyIndicator != null && readySprite != null) ReadyIndicator.sprite = readySprite;
    }

    public void SetDisconnected()
    {
        if (ConnectedPanel != null) ConnectedPanel.SetActive(false);
        if (DisconnectedPanel != null) DisconnectedPanel.SetActive(true);
        // Garante que o botão Ready fica escondido quando o slot fica vazio
        if (ReadyButton != null) ReadyButton.gameObject.SetActive(false);
    }

    public void SetReadyIndicator(Sprite sprite)
    {
        if (ReadyIndicator != null && sprite != null)
            ReadyIndicator.sprite = sprite;
    }

    /// <summary>Mostra/esconde o botão Ready (só visível para o dono do slot).</summary>
    public void SetReadyButtonVisible(bool visible)
    {
        if (ReadyButton != null)
            ReadyButton.gameObject.SetActive(visible);
    }

    /// <summary>Muda o texto do botão entre "Ready" e "Cancelar".</summary>
    public void SetReadyButtonLabel(string label)
    {
        if (ReadyButtonLabel != null)
            ReadyButtonLabel.text = label;
    }
}