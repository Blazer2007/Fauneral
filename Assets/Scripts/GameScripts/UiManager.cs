using UnityEngine;
using UnityEngine.SceneManagement;
using Networking;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [SerializeField] public AudioSource _buttonClicked;

    private int _lastPlayFrame = -1;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Garantir que o volume do botão é consistente com as configurações salvas
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        if (_buttonClicked != null)
        {
            _buttonClicked.volume = isMuted ? 0f : savedVolume;
        }

        // Registrar automaticamente o som em todos os botões da cena
        UnityEngine.UI.Button[] buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
        foreach (UnityEngine.UI.Button btn in buttons)
        {
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    public void PlayClickSound()
    {
        if (Time.frameCount == _lastPlayFrame) return;
        _lastPlayFrame = Time.frameCount;

        if (_buttonClicked != null && _buttonClicked.clip != null)
        {
            GameObject tempAudio = new GameObject("TempClickSound");
            AudioSource source = tempAudio.AddComponent<AudioSource>();
            source.clip = _buttonClicked.clip;

            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
            bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
            source.volume = isMuted ? 0f : savedVolume;

            DontDestroyOnLoad(tempAudio);
            source.Play();
            Destroy(tempAudio, _buttonClicked.clip.length);
        }
    }

    public void SetOnlineMode(bool online)
    {
        NetworkSessionSettings.IsOnlineMode = online; 
    }

    public void PlayButton()
    {
        SceneManager.LoadScene("PlayMenu");
        PlayClickSound();
    }

    public void OnlineModeButton()
    {
        SetOnlineMode(true);
        SceneManager.LoadScene("CreateRoom");
        PlayClickSound();
    }

    public void LocalModeButton()
    {
        SetOnlineMode(false);
        SceneManager.LoadScene("CreateRoom");
        PlayClickSound();
    }

    public void QuitButton()
    {
        PlayClickSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CreateRoomButton()
    {
        SceneManager.LoadScene("CreateRoom");
        PlayClickSound();
    }

    public void JoinRoomButton()
    {
        SceneManager.LoadScene("JoinRoom");
        PlayClickSound();
    }

    public void BackButton(string destination)
    {     
        SceneManager.LoadScene(destination);
        PlayClickSound();
    }

    public void StartGameButton ()
    {
        SceneManager.LoadScene("GameScene");
        PlayClickSound();
    }

    public void OptionsButton ()
    {
        SceneManager.LoadScene("OptionsMenu");
        PlayClickSound();
    }

    public void CardGuideButton ()
    {
        SceneManager.LoadScene("CardGuide");
        PlayClickSound();
    }

    public void CratesButton () 
    {
        SceneManager.LoadScene("CratesMenu");
        PlayClickSound();
    }

    public void ProfileButton()
    {
        SceneManager.LoadScene("ProfileMenu");
        PlayClickSound();
    }
}