using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Toggle _muteToggle;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _fullScreenToggle;

    private Resolution[] _resolutions;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadAudioSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        AudioListener.volume = isMuted ? 0f : savedVolume;
    }

    private void Start()
    {
        // 1. Configura Volume
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        bool savedMute = PlayerPrefs.GetInt("Muted", 0) == 1;

        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.minValue = 0f;
            _masterVolumeSlider.maxValue = 1f;
            _masterVolumeSlider.value = savedVolume;
            _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (_muteToggle != null)
        {
            _muteToggle.isOn = savedMute;
            _muteToggle.onValueChanged.AddListener(SetMute);
        }

        UpdateAudio(savedVolume, savedMute);

        // 2. Configura Resoluções
        _resolutions = Screen.resolutions;
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = _resolutions[i].width + " x " + _resolutions[i].height + " @ " + _resolutions[i].refreshRateRatio.numerator + "Hz";
                options.Add(option);

                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResIndex = i;
                }
            }

            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.value = currentResIndex;
            _resolutionDropdown.RefreshShownValue();
            _resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        // 3. Fullscreen
        if (_fullScreenToggle != null)
        {
            _fullScreenToggle.isOn = Screen.fullScreen;
            _fullScreenToggle.onValueChanged.AddListener(SetFullScreen);
        }
    }

    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat("MasterVolume", volume);
        bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        UpdateAudio(volume, isMuted);
    }

    public void SetMute(bool isMuted)
    {
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        float volume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        UpdateAudio(volume, isMuted);
    }

    private void UpdateAudio(float volume, bool isMuted)
    {
        AudioListener.volume = isMuted ? 0f : volume;
        PlayerPrefs.Save();
    }

    public void SetResolution(int index)
    {
        Resolution res = _resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }

    public void BackToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
