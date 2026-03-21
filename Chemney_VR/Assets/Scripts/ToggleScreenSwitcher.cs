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
    public VideoLoadingIndicator1 tutorialVideoLoader;

    public void OnGoButtonClicked()
    {
        bool skipTutorial = skipTutorialToggle != null && skipTutorialToggle.isOn;

        welcomePanel.SetActive(false);
        testPanel.SetActive(false);
        tutorialPanel.SetActive(false);

        if (skipTutorial)
        {
            tutorialVideoLoader?.StopVideo();
            testPanel.SetActive(true);
            return;
        }

        tutorialPanel.SetActive(true);
        tutorialVideoLoader?.ReplayVideo();
    }
}
