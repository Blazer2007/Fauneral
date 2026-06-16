using UnityEngine;
using Networking;

public class SessionSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject _modePanel;
    [SerializeField] private GameObject _actionPanel;

    private void Start()
    {
        ShowModeSelection();
    }

    public void SelectOnline()
    {
        NetworkSessionSettings.IsOnlineMode = true;
        ShowActionSelection();
    }

    public void SelectLocal()
    {
        NetworkSessionSettings.IsOnlineMode = false;
        ShowActionSelection();
    }

    public void ShowModeSelection()
    {
        _modePanel.SetActive(true);
        _actionPanel.SetActive(false);
    }

    private void ShowActionSelection()
    {
        _modePanel.SetActive(false);
        _actionPanel.SetActive(true);
    }
}