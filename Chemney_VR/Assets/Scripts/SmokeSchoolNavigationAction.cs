using TMPro;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class SmokeSchoolNavigationAction : MonoBehaviour
{
    [Header("Return Home Button")]
    public Button actionButton;
    public CanvasGroup actionCanvasGroup;
    public TMP_Text actionLabel;
    public string returnHomeLabel = "Return to Home";
    public float returnHomeTextSize = 20f;
    public float returnHomeAnchorY = 0.025f;
    public float returnHomeScratchControlsAnchorY = -0.06f;
    public float returnHomeAnchorHeight = 0.04f;

    [Header("Exit Button")]
    public Button exitButton;
    public CanvasGroup exitCanvasGroup;
    public TMP_Text exitLabel;
    public string exitApplicationLabel = "Exit Application";

    [Header("Panels")]
    public GameObject homePanel;
    public GameObject loginPanel;
    public bool hideOnLoginPanel = true;

    [Header("Media")]
    public VideoPlayer[] videoPlayers;

    [Header("Practice Controls")]
    public Button scratchButton;

    private static bool creatingExitButtonClone;
    private bool generatedExitButton;
    private bool returnHomeStyled;
    private ManagerTesting managerTesting;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SmokeSchoolExitWebXR();
#endif

    private void Awake()
    {
        if (creatingExitButtonClone)
        {
            enabled = false;
            return;
        }

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
            actionButton.onClick.RemoveListener(ReturnToHome);
            actionButton.onClick.AddListener(ReturnToHome);
        }

        EnsureExitButton();
        StyleReturnHomeButton();

        if (exitButton != null)
        {
            if (generatedExitButton)
            {
                exitButton.onClick.RemoveAllListeners();
            }
            else
            {
                exitButton.onClick.RemoveListener(ExitApplication);
            }

            exitButton.onClick.AddListener(ExitApplication);
        }

        UpdatePresentation();
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleAction);
            actionButton.onClick.RemoveListener(ReturnToHome);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitApplication);
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
        bool loginVisible = hideOnLoginPanel && loginPanel != null && loginPanel.activeInHierarchy;
        bool showReturnHome = !homeVisible && !loginVisible;
        bool showExit = homeVisible;

        if (actionLabel != null)
        {
            actionLabel.text = returnHomeLabel;
        }

        ApplyReturnHomePlacement();

        SetButtonVisible(actionButton, actionCanvasGroup, showReturnHome);

        if (exitLabel != null)
        {
            exitLabel.text = exitApplicationLabel;
        }

        SetButtonVisible(exitButton, exitCanvasGroup, showExit);
    }

    private void StyleReturnHomeButton()
    {
        if (returnHomeStyled || actionButton == null)
        {
            return;
        }

        Graphic targetGraphic = actionButton.targetGraphic;
        if (targetGraphic != null)
        {
            Color transparent = targetGraphic.color;
            transparent.a = 0f;
            targetGraphic.color = transparent;
            targetGraphic.raycastTarget = true;
            actionButton.targetGraphic = null;
        }

        Image backgroundImage = actionButton.GetComponent<Image>();
        if (backgroundImage != null)
        {
            Color transparent = backgroundImage.color;
            transparent.a = 0f;
            backgroundImage.color = transparent;
        }

        if (actionLabel != null)
        {
            actionLabel.color = Color.black;
            actionLabel.fontSize = returnHomeTextSize;
            actionLabel.fontStyle |= FontStyles.Underline;
        }

        ApplyReturnHomePlacement();

        returnHomeStyled = true;
    }

    private void ApplyReturnHomePlacement()
    {
        if (actionButton == null)
        {
            return;
        }

        RectTransform returnRect = actionButton.GetComponent<RectTransform>();
        if (returnRect == null)
        {
            return;
        }

        float anchorY = IsScratchControlVisible() ? returnHomeScratchControlsAnchorY : returnHomeAnchorY;
        returnRect.anchorMin = new Vector2(0.37f, anchorY);
        returnRect.anchorMax = new Vector2(0.63f, anchorY + returnHomeAnchorHeight);
        returnRect.anchoredPosition = Vector2.zero;
        returnRect.sizeDelta = Vector2.zero;
    }

    private bool IsScratchControlVisible()
    {
        if (scratchButton != null)
        {
            return scratchButton.gameObject.activeInHierarchy;
        }

        if (managerTesting == null)
        {
            managerTesting = FindFirstObjectByType<ManagerTesting>(FindObjectsInactive.Include);
        }

        if (managerTesting != null && managerTesting.btn_Scratch != null)
        {
            scratchButton = managerTesting.btn_Scratch;
            return scratchButton.gameObject.activeInHierarchy;
        }

        GameObject scratchObject = GameObject.Find("Scratch_Btn");
        if (scratchObject != null)
        {
            return scratchObject.activeInHierarchy;
        }

        return false;
    }

    private void EnsureExitButton()
    {
        if (exitButton != null || actionButton == null)
        {
            return;
        }

        Button clonedButton = null;
        try
        {
            creatingExitButtonClone = true;
            clonedButton = Instantiate(actionButton, actionButton.transform.parent);
        }
        finally
        {
            creatingExitButtonClone = false;
        }

        if (clonedButton == null)
        {
            return;
        }

        clonedButton.name = "Exit Application Button";

        SmokeSchoolNavigationAction clonedNavigation = clonedButton.GetComponent<SmokeSchoolNavigationAction>();
        if (clonedNavigation != null && clonedNavigation != this)
        {
            Destroy(clonedNavigation);
        }

        exitButton = clonedButton;
        generatedExitButton = true;
        exitCanvasGroup = clonedButton.GetComponent<CanvasGroup>();
        if (exitCanvasGroup == null)
        {
            exitCanvasGroup = clonedButton.gameObject.AddComponent<CanvasGroup>();
        }

        exitLabel = clonedButton.GetComponentInChildren<TMP_Text>(true);

        RectTransform exitRect = clonedButton.GetComponent<RectTransform>();
        RectTransform returnRect = actionButton.GetComponent<RectTransform>();
        if (exitRect != null && returnRect != null)
        {
            exitRect.anchorMin = returnRect.anchorMin;
            exitRect.anchorMax = returnRect.anchorMax;
            exitRect.anchoredPosition = returnRect.anchoredPosition;
            exitRect.sizeDelta = returnRect.sizeDelta;
        }
    }

    private void SetButtonVisible(Button button, CanvasGroup canvasGroup, bool visible)
    {
        if (button != null)
        {
            button.interactable = visible;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    private bool IsHomeVisible()
    {
        return homePanel != null && homePanel.activeInHierarchy;
    }

    public void ReturnToHome()
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

    public void ExitApplication()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SmokeSchoolExitWebXR();
#else
        Application.Quit();
#endif
    }
}
