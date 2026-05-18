using UnityEngine;
using UnityEngine.SceneManagement;

public class UiManager : MonoBehaviour
{

    public void PlayButton()
    {
        SceneManager.LoadScene("PlayMenu");
    }

    public void QuitButton()
    {
        Application.Quit();
    }

    public void CreateRoomButton()
    {
        SceneManager.LoadScene("CreateRoom");
    }

    public void JoinRoomButton()
    {
        SceneManager.LoadScene("JoinRoom");
    }

    public void BackFromPlayButton()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void BackFromCreateRoomButton()
    {
        SceneManager.LoadScene("PlayMenu");
    }

    public void BackFromJoinRoomButton()
    {
        SceneManager.LoadScene("PlayMenu");
    }

    public void CreateRoomAndJoinButton()
    {
        SceneManager.LoadScene("LobbyMenu");
    }

    public void JoinRoomAndJoinButton()
    {
        SceneManager.LoadScene("LobbyMenu");
    }

    public void BackFromLobbyButton()
    {
        SceneManager.LoadScene("PlayMenu");
    }

    public void StartGameButton ()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OptionsButton ()
    {
        SceneManager.LoadScene("OptionsMenu");
    }

    public void BackFromOptionsButton ()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void CardGuideButton ()
    {
        SceneManager.LoadScene("CardGuide");
    }

    public void BackFromCardGuideButton ()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void CratesButton () 
    {
        SceneManager.LoadScene("CratesMenu");
    }

    public void BackFromCratesButton ()
    {
        SceneManager.LoadScene("MainMenu");
    }



    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
