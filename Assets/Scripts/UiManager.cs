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

    public void startgamebutton ()
    {
        SceneManager.LoadScene("GameScene");
    }



    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
