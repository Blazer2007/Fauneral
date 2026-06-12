using UnityEngine;
using TMPro;

public class PointsHUD : MonoBehaviour
{
    public ScriptableCredits credits;
    public TextMeshProUGUI label;
    public string prefix = "credits: ";


    private void Update()
    {
        OnEnable();
        OnDisable();
        UpdateLabel(credits != null ? credits.Credits : 0);
    }

    private void OnEnable()
    {
        if (credits != null)
        {
            credits.OnCreditsChanged += UpdateLabel;
        }

        UpdateLabel(credits != null ? credits.Credits : 0);
    }

    private void OnDisable()
    {
        if (credits != null) 
        {
            credits.OnCreditsChanged -= UpdateLabel;
        }
    }

    private void UpdateLabel(int value)
    {
        if (label != null)
        {
            label.text = prefix + value;
        }
    }

}
