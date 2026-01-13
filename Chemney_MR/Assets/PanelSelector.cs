using UnityEngine;
using UnityEngine.UI;

public class PanelSelector : MonoBehaviour
{
    [Header("UI Elements")]
    public Toggle checkboxToggle;
    public GameObject testPanel;
    public GameObject practicePanel;

    // Call this function on your Start button's OnClick
    public void ProceedBasedOnToggle()
    {
        if (checkboxToggle.isOn)
        {
            // Show Test Panel
            testPanel.SetActive(true);
            practicePanel.SetActive(false);
        }
        else
        {
            // Show Practice Panel
            practicePanel.SetActive(true);
            testPanel.SetActive(false);
        }
    }
}
