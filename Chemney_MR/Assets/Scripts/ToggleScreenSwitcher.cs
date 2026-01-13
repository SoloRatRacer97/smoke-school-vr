using UnityEngine;
using UnityEngine.UI;

public class ToggleScreenSwitcher : MonoBehaviour
{
    [Header("UI Components")]
    public Toggle skipTutorialToggle;

    
    [Header("Panels")]
    public GameObject welcomePanel;
    public GameObject tutorialPanel;
    public GameObject testPanel;

   public  void OnGoButtonClicked()
    {
        welcomePanel.SetActive(false); 
        tutorialPanel.SetActive(false);
        testPanel.SetActive(false);
        (skipTutorialToggle.isOn ? testPanel : tutorialPanel).SetActive(true) ;
    }
}
