using Unity.VisualScripting;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{

    [SerializeField]
    private WebRequestManager webRequestManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void OnButtonEventClick(string buttonType)
    {
        Debug.Log($"Button {buttonType} Clicked!");
        webRequestManager.ButtonClicked(buttonType);
        
    }
}
