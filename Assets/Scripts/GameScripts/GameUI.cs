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
        // Esconde todas as barras até sabermos quantos jogadores há
        //foreach (var bar in HPBars)
        //    if (bar != null) bar.gameObject.SetActive(false);

        HideEndScreen();
    }

    public void Update()
    {
        for (int i = 0; i < _playerHPs.Count; i++)
            if (i < HPBars.Count && HPBars[i] != null)
                HPBars[i].value = _playerHPs[i].CurrentHP;
    }

    /// <summary>
    /// Chamado pelo PlayerSpawner depois de todos os jogadores estarem spawnados.
    /// Corre em todos os clientes e mapeia as barras por PlayerIndex.
    /// </summary>
    [ClientRpc]
    public void InitialiseHPBarsClientRpc()
    {
        StartCoroutine(InitialiseAfterDelay());
    }

    private IEnumerator InitialiseAfterDelay()
    {
        // Espera um frame para garantir que todos os NetworkObjects estão inicializados
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

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
        
        //// Esconde barras sem jogador
        //for (int i = 0; i < HPBars.Count; i++)
        //    if (HPBars[i] != null)
        //       HPBars[i].gameObject.SetActive(false);
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
        ShowEndScreen($"Player {playerIndex + 1} wins the MATCH!.");
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