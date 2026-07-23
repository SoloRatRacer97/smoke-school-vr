using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DataInput_Fields : MonoBehaviour
{
    [Serializable]
    private class ApprovedPayload
    {
        public bool approved;
        public string reason;
        public string sessionReference;
        public ApprovedProfile student;
    }

    [Serializable]
    private class ApprovedProfile
    {
        public string userId;
        public string email;
        public string displayName;
        public string company;
        public string expiresAt;
    }

    [Header("Input Fields")]
    public static string playerEmail;
    public static string studentname;
    public static string approvedUserId;
    public static string approvedSessionReference;
    public InputField inputStudentID;
    public InputField inputEmailID;

    [Header("Buttons")]
    public Button goButton;

    [Header("Screens")]
    public GameObject LoginPannel;
    public GameObject welcomePannel;

    [Header("Warning Text")]
    public TextMeshProUGUI warningText;

    public TMP_Text Emailsent;
    public TMP_Text Username;
    public TMP_Text EmailsentF;
    public TMP_Text UsernameF;

    public static int checkSceneReload;

    void Start()
    {
        warningText.gameObject.SetActive(false);
        goButton.gameObject.SetActive(false);

        bool hasInMemoryApproval = checkSceneReload == 1 &&
            !string.IsNullOrWhiteSpace(playerEmail) &&
            !string.IsNullOrWhiteSpace(approvedUserId) &&
            !string.IsNullOrWhiteSpace(approvedSessionReference);

        LoginPannel.SetActive(!hasInMemoryApproval);
        welcomePannel.SetActive(hasInMemoryApproval);
        if (hasInMemoryApproval)
        {
            ApplyApprovedIdentityToUi();
        }
    }

    // Called by the WebGL template only after the browser receives signed approval.
    public void CompleteApprovedLogin(string approvedJson)
    {
        ApprovedPayload payload;
        try
        {
            payload = JsonUtility.FromJson<ApprovedPayload>(approvedJson);
        }
        catch (Exception)
        {
            ShowLoginError("Approved access details could not be loaded.");
            return;
        }

        if (payload == null || !payload.approved || payload.reason != null || payload.student == null ||
            string.IsNullOrWhiteSpace(payload.sessionReference) ||
            string.IsNullOrWhiteSpace(payload.student.userId) ||
            string.IsNullOrWhiteSpace(payload.student.email))
        {
            ShowLoginError("Approved access details are incomplete.");
            return;
        }

        playerEmail = payload.student.email.Trim();
        studentname = string.IsNullOrWhiteSpace(payload.student.displayName)
            ? playerEmail
            : payload.student.displayName.Trim();
        approvedUserId = payload.student.userId.Trim();
        approvedSessionReference = payload.sessionReference.Trim();

        ApplyApprovedIdentityToUi();
        warningText.gameObject.SetActive(false);
        LoginPannel.SetActive(false);
        welcomePannel.SetActive(true);
    }

    private void ApplyApprovedIdentityToUi()
    {
        Emailsent.text = playerEmail;
        Username.text = studentname;
        EmailsentF.text = playerEmail;
        UsernameF.text = studentname;
        ScreenshotSender.messageToSend = $"Student: {studentname}\nEmail: {playerEmail}";
    }

    private void ShowLoginError(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        LoginPannel.SetActive(true);
        welcomePannel.SetActive(false);
    }
}
