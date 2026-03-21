using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ToggleScreenSwitcher : MonoBehaviour
{
    [Header("UI Components")]
    public Toggle skipTutorialToggle;

    
    [Header("Panels")]
    public GameObject welcomePanel;
    public GameObject tutorialPanel;
    public GameObject testPanel;

    [Header("Tutorial Video")]
    public VideoPlayer tutorialVideoPlayer;
    public string tutorialVideoUrl;

   public  void OnGoButtonClicked()
    {
        welcomePanel.SetActive(false); 
        tutorialPanel.SetActive(false);
        testPanel.SetActive(false);

        bool skipTutorial = skipTutorialToggle.isOn;
        (skipTutorial ? testPanel : tutorialPanel).SetActive(true);

        if (skipTutorial)
        {
            StopTutorialVideo();
        }
        else
        {
            PlayTutorialVideo();
        }
    }

    void OnDisable()
    {
        StopTutorialVideo();
    }

    void PlayTutorialVideo()
    {
        if (tutorialVideoPlayer == null || string.IsNullOrWhiteSpace(tutorialVideoUrl))
        {
            return;
        }

        if (tutorialVideoPlayer.url != tutorialVideoUrl)
        {
            tutorialVideoPlayer.url = tutorialVideoUrl;
        }

        tutorialVideoPlayer.isLooping = true;
        tutorialVideoPlayer.Stop();
        tutorialVideoPlayer.Play();
    }

    void StopTutorialVideo()
    {
        if (tutorialVideoPlayer != null && tutorialVideoPlayer.isPlaying)
        {
            tutorialVideoPlayer.Stop();
        }
    }
}
