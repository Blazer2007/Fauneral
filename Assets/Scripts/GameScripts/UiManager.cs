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

    public void BackButton(string destination)
    {
        SceneManager.LoadScene(destination);
    }

    public void StartGameButton ()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OptionsButton ()
    {
        SceneManager.LoadScene("OptionsMenu");
    }

    public void CardGuideButton ()
    {
        SceneManager.LoadScene("CardGuide");
    }

    public void CratesButton () 
    {
        SceneManager.LoadScene("CratesMenu");
    }
}
