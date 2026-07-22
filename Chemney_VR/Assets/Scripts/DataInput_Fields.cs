using System;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DataInput_Fields : MonoBehaviour
{
    [Serializable]
    private class ApprovedProfile
    {
        public string email;
        public string displayName;
        public string company;
        public string expiresAt;
    }

    [Header("Input Fields")]
    public static string playerEmail;
    public static string studentname;
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


    private const string EMAIL_KEY = "PLAYER_EMAIL";
    private const string NAME_KEY = "STUDENT_NAME";

    void Start()
    {
        welcomePannel.SetActive(false);
        LoginPannel.SetActive(true);
        goButton.gameObject.SetActive(true);  // Keep it always active (optional UX)

        warningText.gameObject.SetActive(false);

        inputStudentID.onValueChanged.AddListener(delegate { HideWarningIfValid(); });
        inputEmailID.onValueChanged.AddListener(delegate { HideWarningIfValid(); });

#if !UNITY_WEBGL || UNITY_EDITOR
        goButton.onClick.AddListener(OnGoButtonClicked);
#endif
        playerEmail = PlayerPrefs.GetString(EMAIL_KEY);
        studentname = PlayerPrefs.GetString(NAME_KEY);
        if (checkSceneReload == 1)
        {
            Emailsent.text = playerEmail;
            Username.text = studentname;

            EmailsentF.text = playerEmail;
            UsernameF.text = studentname;
            LoginPannel.SetActive(false);
            welcomePannel.SetActive(true);
        }
        else
        {
            LoginPannel.SetActive(true);
            welcomePannel.SetActive(false);
        }
    }

    void HideWarningIfValid()
    {

        if (!string.IsNullOrEmpty(inputStudentID.text) && !string.IsNullOrEmpty(inputEmailID.text))
        {
            warningText.gameObject.SetActive(false);
        }
    }

    void OnGoButtonClicked()
    {
        string studentID = inputStudentID.text.Trim();
        string emailID = inputEmailID.text.Trim();

//checking fields
        if (string.IsNullOrEmpty(studentID) || string.IsNullOrEmpty(emailID))
        {
            warningText.text = "All fields are required.";
            warningText.gameObject.SetActive(true);
            return;
        }

        if (!IsValidEmail(emailID))
        {
            warningText.text = "Enter a valid email address.";
            warningText.gameObject.SetActive(true);
            return;
        }

        CompleteLogin(studentID, emailID);
    }

    // Called by the WebGL template only after /api/vr/login approves access.
    public void CompleteApprovedLogin(string profileJson)
    {
        ApprovedProfile profile;
        try
        {
            profile = JsonUtility.FromJson<ApprovedProfile>(profileJson);
        }
        catch (Exception)
        {
            ShowLoginError("Approved profile could not be loaded.");
            return;
        }

        if (profile == null || string.IsNullOrWhiteSpace(profile.email))
        {
            ShowLoginError("Approved profile is missing an email address.");
            return;
        }

        string approvedName = string.IsNullOrWhiteSpace(profile.displayName)
            ? profile.email
            : profile.displayName.Trim();
        CompleteLogin(approvedName, profile.email.Trim());
    }

    private void CompleteLogin(string approvedName, string approvedEmail)
    {
        playerEmail = approvedEmail;
        studentname = approvedName;

        PlayerPrefs.SetString(EMAIL_KEY, playerEmail);
        PlayerPrefs.SetString(NAME_KEY, studentname);
        PlayerPrefs.Save();

        Emailsent.text = playerEmail;
        Username.text = studentname;
        EmailsentF.text = playerEmail;
        UsernameF.text = studentname;
        ScreenshotSender.messageToSend = $"Student: {studentname}\nEmail: {playerEmail}";

        warningText.gameObject.SetActive(false);
        LoginPannel.SetActive(false);
        welcomePannel.SetActive(true);
    }

    private void ShowLoginError(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        LoginPannel.SetActive(true);
        welcomePannel.SetActive(false);
    }

    bool IsValidEmail(string email)
    {
        try
        {
            MailAddress mailAddress = new MailAddress(email);
            return mailAddress.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
