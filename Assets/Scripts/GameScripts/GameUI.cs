using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles all in-game UI: HP bars, round win counters, and end screens.
/// All elements are placeholder hook up in the Inspector.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("HP Bars — one Slider per player, in order P1, P2, ...")]
    [Tooltip("Drag the HP Slider for each player here, in player order")]
    public List<Slider> HPBars = new List<Slider>();

    [Header("Wins Text — one TMP_Text per player, in order P1, P2, ...")]
    [Tooltip("Drag the Wins TMP_Text for each player here, in player order")]
    public List<TMP_Text> WinsTexts = new List<TMP_Text>();

    [Header("End Screen")]
    [Tooltip("The Panel that shows when a round or match ends")]
    public GameObject EndScreen;

    [Tooltip("Text inside EndScreen that shows the result message")]
    public TMP_Text EndText;

    [Tooltip("Button to restart the match — wire OnClick to RoundManager.RestartMatch")]
    public Button RestartButton;

    // Reference to players so we can read HP every frame
    private List<PlayerHealth> _players = new List<PlayerHealth>();

    private void Start()
    {
        // Find all players in the scene automatically
        _players.AddRange(FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None));
        // Sort by PlayerIndex so P1 maps to HPBars[0], P2 to HPBars[1], etc.
        _players.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

        // Initialise HP bars
        for (int i = 0; i < _players.Count; i++)
        {
            if (i < HPBars.Count && HPBars[i] != null)
            {
                HPBars[i].minValue = 0;
                HPBars[i].maxValue = _players[i].MaxHP;
                HPBars[i].value = _players[i].CurrentHP;
            }
        }

        HideEndScreen();
    }

    private void Update()
    {
        // Update HP bars every frame
        for (int i = 0; i < _players.Count; i++)
        {
            if (i < HPBars.Count && HPBars[i] != null)
                HPBars[i].value = _players[i].CurrentHP;
        }
    }

    /// <summary>
    /// Updates the round win counters displayed on screen.
    /// </summary>
    public void UpdateRoundWins(Dictionary<int, int> roundWins)
    {
        foreach (var kvp in roundWins)
        {
            int index = kvp.Key; // PlayerIndex
            if (index < WinsTexts.Count && WinsTexts[index] != null)
                WinsTexts[index].text = $"Wins: {kvp.Value}";
        }
    }

    /// <summary>
    /// Shows a "Player X wins the round!" message briefly.
    /// </summary>
    public void ShowRoundWinner(int playerIndex, Dictionary<int, int> roundWins)
    {
        UpdateRoundWins(roundWins);
        ShowEndScreen($"Player {playerIndex + 1} wins the round!");
    }

    /// <summary>
    /// Shows the match winner screen with a restart option.
    /// </summary>
    public void ShowMatchWinner(int playerIndex)
    {
        ShowEndScreen($"Player {playerIndex + 1} wins the MATCH!\n\nPress Restart to play again.");
        if (RestartButton != null)
            RestartButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Shows a draw message.
    /// </summary>
    public void ShowDraw()
    {
        ShowEndScreen("Draw! No points awarded.");
    }

    public void HideEndScreen()
    {
        if (EndScreen != null)
            EndScreen.SetActive(false);

        if (RestartButton != null)
            RestartButton.gameObject.SetActive(false);
    }

    private void ShowEndScreen(string message)
    {
        if (EndScreen != null)
            EndScreen.SetActive(true);

        if (EndText != null)
            EndText.text = message;
    }
}