using UnityEngine;
using TMPro;

public class PointsHUD : MonoBehaviour
{
    public ScriptablePoints points;
    public TextMeshProUGUI label;
    public string prefix = "credits: ";

    private void OnEnable()
    {
        if (points != null)
        {
            points.OnPointsChanged += UpdateLabel;
        }

        UpdateLabel(points != null ? points.Points : 0);
    }

    private void OnDisable()
    {
        if (points != null) 
        {
            points.OnPointsChanged -= UpdateLabel;
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
