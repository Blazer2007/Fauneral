using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : NetworkBehaviour
{
    public static GameUI Instance { get; private set; }

    [Header("HP Bars — P1, P2, P3, P4")]
    public List<Slider> HPBars = new List<Slider>();

    [Header("Wins Text — P1, P2, P3, P4")]
    public List<TMP_Text> WinsTexts = new List<TMP_Text>();

    [Header("End Screen")]
    public GameObject EndScreen;
    public TMP_Text EndText;
    public Button RestartButton;

    private readonly List<PlayerHealth> _playerHPs = new List<PlayerHealth>();

    [Tooltip("Intervalo (segundos) entre varreduras automáticas por jogadores na cena.")]
    [SerializeField] private float _rescanInterval = 0.5f;
    private float _rescanTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Esconde todas as barras até um jogador existir
        foreach (var bar in HPBars)
            if (bar != null) bar.gameObject.SetActive(false);

        HideEndScreen();
    }

    private void Update()
    {
        // Varredura periódica: deteta jogadores sem depender de timing de rede
        _rescanTimer -= Time.deltaTime;
        if (_rescanTimer <= 0f)
        {
            _rescanTimer = _rescanInterval;
            RescanPlayers();
        }

        // Atualiza o valor de cada barra com base na vida atual de cada jogador
        for (int i = 0; i < _playerHPs.Count && i < HPBars.Count; i++)
        {
            var hp = _playerHPs[i];
            var bar = HPBars[i];
            if (hp == null || bar == null) continue;

            bar.maxValue = hp.MaxHP;
            bar.value = hp.CurrentHP;
        }
    }

    /// <summary>Procura todos os PlayerHealth na cena e atualiza a lista se algo mudou.</summary>
    private void RescanPlayers()
    {
        var found = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        bool changed = false;

        int before = _playerHPs.Count;
        _playerHPs.RemoveAll(p => p == null);
        if (_playerHPs.Count != before) changed = true;

        foreach (var hp in found)
        {
            if (!_playerHPs.Contains(hp))
            {
                _playerHPs.Add(hp);
                changed = true;
            }
        }

        if (changed) RefreshBars();
    }

    /// <summary>Chamado por cada PlayerHealth quando faz spawn na rede.</summary>
    public void RegisterPlayer(PlayerHealth hp)
    {
        if (hp == null || _playerHPs.Contains(hp)) return;

        _playerHPs.Add(hp);
        RefreshBars();
    }

    /// <summary>Remove um jogador (ex: ao desconectar) e reorganiza as barras.</summary>
    public void UnregisterPlayer(PlayerHealth hp)
    {
        if (hp == null || !_playerHPs.Contains(hp)) return;

        _playerHPs.Remove(hp);
        RefreshBars();
    }

    /// <summary>Reordena por PlayerIndex e ativa apenas as barras necessárias.</summary>
    public void RefreshBars()
    {
        // Limpa entradas nulas (jogadores destruídos)
        _playerHPs.RemoveAll(p => p == null);
        _playerHPs.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

        for (int i = 0; i < HPBars.Count; i++)
        {
            if (HPBars[i] == null) continue;

            if (i < _playerHPs.Count)
            {
                HPBars[i].minValue = 0;
                HPBars[i].maxValue = _playerHPs[i].MaxHP;
                HPBars[i].value = _playerHPs[i].CurrentHP;
                HPBars[i].gameObject.SetActive(true);
            }
            else
            {
                HPBars[i].gameObject.SetActive(false);
            }
        }
    }

    [ClientRpc]
    public void InitialiseHPBarsClientRpc()
    {
        RescanPlayers();
    }

    public void UpdateRoundWins(Dictionary<int, int> roundWins)
    {
        foreach (var kvp in roundWins)
        {
            int index = kvp.Key;
            if (index < WinsTexts.Count && WinsTexts[index] != null)
                WinsTexts[index].text = $"Wins: {kvp.Value}";
        }
    }

    public void ShowRoundWinner(int playerIndex, Dictionary<int, int> roundWins)
    {
        UpdateRoundWins(roundWins);
        ShowEndScreen($"Player {playerIndex + 1} wins the round!");
    }

    public void ShowMatchWinner(int playerIndex)
    {
        ShowEndScreen($"Player {playerIndex + 1} wins the MATCH!\n\nPress Restart to play again.");
        if (RestartButton != null)
            RestartButton.gameObject.SetActive(true);
    }

    public void ShowDraw()
    {
        ShowEndScreen("Draw! No points awarded.");
    }

    public void HideEndScreen()
    {
        if (EndScreen != null) EndScreen.SetActive(false);
        if (RestartButton != null) RestartButton.gameObject.SetActive(false);
    }

    private void ShowEndScreen(string message)
    {
        if (EndScreen != null) EndScreen.SetActive(true);
        if (EndText != null) EndText.text = message;
    }
}