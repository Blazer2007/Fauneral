using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Handles all in-game UI: HP bars, round win counters, and end screens.
/// All elements are placeholder hook up in the Inspector.
/// </summary>
public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

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
    private List<PlayerHealth> _playerHPs = new List<PlayerHealth>();



    private void Start()
    {
            
    }

    private void Update()
    {
        for (int i = 0; i < _playerHPs.Count; i++)
            if (i < HPBars.Count && HPBars[i] != null)
                HPBars[i].value = _playerHPs[i].CurrentHP;
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

    // This method is called when the players are first spawned,
    // to link the HP bars to the correct players on each client and connect the respective PlayerHealth between them.
    [ClientRpc]
    public void ConnectHPBarsAndPlayerHPWithPlayers(ulong ClientId, ClientRpcParams rpcParams = default)
    {
        UpdateIndividualHPBars(ClientId); 
        PlayerHealth[] playersHP = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        _playerHPs.Clear();
        foreach (PlayerHealth php in playersHP)
        {
            var no = php.GetComponentInParent<NetworkObject>();
            if (no.OwnerClientId == ClientId)
            {
                _playerHPs.Add(php);//
            }
        }
    }
    // This method is called every frame to update the HP bars of the respective players on each client.
    [ClientRpc]
    public void UpdateIndividualHPBars(ulong ClientId, ClientRpcParams rpcParams = default)
    {
        // Update individual player's HP bars considering the ClientId to only update the HP bars of the respective player on each client.
        for (int i = 0; i < _playerHPs.Count; i++)
        {
            if (i < HPBars.Count && HPBars[i] != null)
            {
                var no = _playerHPs[i].GetComponentInParent<NetworkObject>();
                if (no.OwnerClientId == ClientId)
                {
                    HPBars[i].value = _playerHPs[i].CurrentHP;
                }
            }
        }

    }

    public void RefreshPlayerList()
    {
        _playerHPs.Clear();
        _playerHPs.AddRange(FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None));
        _playerHPs.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

        for (int i = 0; i < _playerHPs.Count; i++)
        {
            if (i < HPBars.Count && HPBars[i] != null)
            {
                HPBars[i].minValue = 0;
                HPBars[i].maxValue = _playerHPs[i].MaxHP;
                HPBars[i].value = _playerHPs[i].CurrentHP;
                HPBars[i].gameObject.SetActive(true);
            }
        }

        // Esconde barras sem jogador
        for (int i = _playerHPs.Count; i < HPBars.Count; i++)
            if (HPBars[i] != null)
                HPBars[i].gameObject.SetActive(false);
    }

    [ClientRpc]
    public void RefreshPlayerListClientRpc()
    {
        StartCoroutine(RefreshAfterDelay());
    }

    private IEnumerator RefreshAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        RefreshPlayerList();
    }
}