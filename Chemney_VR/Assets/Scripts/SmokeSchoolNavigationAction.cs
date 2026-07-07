using TMPro;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class SmokeSchoolNavigationAction : MonoBehaviour
{
    [Header("Button")]
    public Button actionButton;
    public CanvasGroup actionCanvasGroup;
    public TMP_Text actionLabel;
    public string returnHomeLabel = "Return to Home";
    public string exitApplicationLabel = "Exit Application";

    [Header("Panels")]
    public GameObject homePanel;
    public GameObject loginPanel;
    public bool hideOnLoginPanel = true;

    [Header("Media")]
    public VideoPlayer[] videoPlayers;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SmokeSchoolExitWebXR();
#endif

    private void Awake()
    {
        if (actionButton == null)
        {
            actionButton = GetComponent<Button>();
        }

        if (actionCanvasGroup == null)
        {
            actionCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleAction);
            actionButton.onClick.AddListener(HandleAction);
        }

        UpdatePresentation();
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleAction);
        }
    }

    private void Update()
    {
        UpdatePresentation();
    }

    public void HandleAction()
    {
        if (IsHomeVisible())
        {
            ExitApplication();
            return;
        }

        ReturnToHome();
    }

    private void UpdatePresentation()
    {
        bool homeVisible = IsHomeVisible();
        bool loginOnly = hideOnLoginPanel && loginPanel != null && loginPanel.activeInHierarchy && !homeVisible;

        if (actionLabel != null)
        {
            actionLabel.text = homeVisible ? exitApplicationLabel : returnHomeLabel;
        }

        if (actionButton != null)
        {
            actionButton.interactable = !loginOnly;
        }

        if (actionCanvasGroup != null)
        {
            actionCanvasGroup.alpha = loginOnly ? 0f : 1f;
            actionCanvasGroup.interactable = !loginOnly;
            actionCanvasGroup.blocksRaycasts = !loginOnly;
        }
    }

    private bool IsHomeVisible()
    {
        return homePanel != null && homePanel.activeInHierarchy;
    }

    private void ReturnToHome()
    {
        StopMedia();
        SmokeSchoolAppState.ResetCertificationState();
        DataInput_Fields.checkSceneReload = 1;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void StopMedia()
    {
        if (videoPlayers == null)
        {
            return;
        }

        foreach (VideoPlayer player in videoPlayers)
        {
            if (player == null)
            {
                continue;
            }

            player.Stop();
        }
    }

    private void ExitApplication()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SmokeSchoolExitWebXR();
        return;
#endif

        Application.Quit();
    }
}
