using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI da cena LobbyMenu.
/// Lê os dados de LobbySessionData no Start() — não precisa de referências directas.
///
/// Setup no Inspector:
///   RoomNameText    → TMP_Text com o nome da sala
///   PinText         → TMP_Text com o PIN (ex: "PIN: 4829")
///   PlayerCountText → TMP_Text com "X / Y jogadores"
///   VisibilityText  → TMP_Text "Pública" / "Privada"
///   StartGameButton → Button visível só para o criador
///   LeaveButton     → Button "Sair do Lobby"
/// </summary>
public class LobbyMenuUI : MonoBehaviour
{
    public static LobbyMenuUI Instance { get; private set; }

    [Header("Informação da sala")]
    [SerializeField] private TMP_Text _roomNameText;
    [SerializeField] private TMP_Text _pinText;
    [SerializeField] private TMP_Text _playerCountText;
    [SerializeField] private TMP_Text _visibilityText;

    [Header("Acções")]
    [SerializeField] private Button _startGameButton;
    [SerializeField] private Button _leaveButton;

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
        // Lê os dados guardados pelo LobbyClientManager antes da mudança de cena
        if (!LobbySessionData.IsInLobby)
        {
            Debug.LogWarning("[LobbyMenuUI] Sem dados de lobby — a voltar ao menu.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMenu");
            return;
        }

        RefreshAll();
    }

    // ── BOTÕES ─────────────────────────────────────────────────────

    public void OnStartGameButton()
    {
        // Só o criador deve poder chamar isto.
        // Aqui podes adicionar um ServerRpc de início de jogo.
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    public void OnLeaveButton()
    {
        LobbyClientManager.Instance?.LeaveLobby();
        UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMenu");
    }

    // ── CHAMADO PELO LobbyClientManager ───────────────────────────

    /// <summary>Actualiza só a contagem de jogadores (chamado quando alguém entra/sai).</summary>
    public void RefreshPlayerCount(int current, int max)
    {
        LobbySessionData.CurrentPlayers = current;
        LobbySessionData.MaxPlayers = max;

        if (_playerCountText != null)
            _playerCountText.text = $"{current} / {max} jogadores";
    }

    // ── HELPERS ───────────────────────────────────────────────────

    private void RefreshAll()
    {
        if (_roomNameText != null)
            _roomNameText.text = LobbySessionData.RoomName;

        if (_pinText != null)
            _pinText.text = $"PIN: {LobbySessionData.Pin}";

        if (_playerCountText != null)
            _playerCountText.text = $"{LobbySessionData.CurrentPlayers} / {LobbySessionData.MaxPlayers} jogadores";

        if (_visibilityText != null)
            _visibilityText.text = LobbySessionData.IsPublic ? "Pública" : "Privada";

        // O botão de iniciar só aparece para o criador
        if (_startGameButton != null)
            _startGameButton.gameObject.SetActive(LobbySessionData.IsCreator);
    }
}