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

    private List<PlayerHealth> _playerHPs = new List<PlayerHealth>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        HideEndScreen();
    }

    [ClientRpc]
    public void InitialiseHPBarsClientRpc()
    {
        // Deixado vazio para o colega implementar a sua própria lógica de busca de jogadores
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