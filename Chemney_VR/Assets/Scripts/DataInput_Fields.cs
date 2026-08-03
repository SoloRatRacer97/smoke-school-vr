using System;
using System.Collections;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DataInput_Fields : MonoBehaviour
{
    [Serializable]
    private class LoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    private class LoginResponse
    {
        public bool approved;
        public string reason;
        public string sessionReference;
        public string resultToken;
        public ApprovedProfile student;
    }

    [Serializable]
    private class ApprovedProfile
    {
        public string certificationNumber;
        public string userId;
        public string email;
        public string displayName;
        public string company;
        public string expiresAt;
    }

    [Header("Input Fields")]
    public static string playerEmail;
    public static string studentname;
    public static string approvedCertificationNumber;
    public static string approvedSessionReference;
    public static string approvedResultToken;
    private static string approvedAuthenticationUrl;
    public InputField inputStudentID;
    public InputField inputEmailID;

    [Header("Buttons")]
    public Button goButton;

    [Header("Screens")]
    public GameObject LoginPannel;
    public GameObject welcomePannel;
    [SerializeField] private GameObject whitePracticeIntroPanel;
    [SerializeField] private GameObject testingPanel;

    [Header("Warning Text")]
    public TextMeshProUGUI warningText;

    public TMP_Text Emailsent;
    public TMP_Text Username;
    public TMP_Text EmailsentF;
    public TMP_Text UsernameF;

    public static int checkSceneReload;

    private const string EMAIL_KEY = "PLAYER_EMAIL";
    private const string NAME_KEY = "STUDENT_NAME";
    private const string FALLBACK_AUTH_URL = "https://smokeschool-dashboard.vercel.app/api/vr/login";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string SmokeSchoolGetAuthApi();

    [DllImport("__Internal")]
    private static extern void SmokeSchoolSetLoginOverlayVisible(bool visible);

    [DllImport("__Internal")]
    private static extern void SmokeSchoolSetAuthenticationLoading(bool loading);
#endif

    void Start()
    {
        welcomePannel.SetActive(false);
        LoginPannel.SetActive(true);
        goButton.gameObject.SetActive(true);
        goButton.onClick.AddListener(OnGoButtonClicked);

        inputStudentID.contentType = InputField.ContentType.Password;
        inputStudentID.ForceLabelUpdate();
        inputEmailID.contentType = InputField.ContentType.EmailAddress;
        inputEmailID.ForceLabelUpdate();
        warningText.gameObject.SetActive(false);

        inputStudentID.onValueChanged.AddListener(delegate { HideWarningIfValid(); });
        inputEmailID.onValueChanged.AddListener(delegate { HideWarningIfValid(); });

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
            SetBrowserLoginVisible(false);
        }
        else
        {
            SetBrowserLoginVisible(true);
        }

        ApplyPostReloadRoute();
    }

    private void ApplyPostReloadRoute()
    {
        if (!ApplyPostReloadPanelRoute())
        {
            return;
        }

        SimpleVideoPlayer introPlayer = whitePracticeIntroPanel.GetComponent<SimpleVideoPlayer>();
        if (introPlayer != null)
        {
            introPlayer.playVideoURL(0);
        }
    }

    private bool ApplyPostReloadPanelRoute()
    {
        if (!ManagerTesting.restartAtWhitePracticeIntro)
        {
            return false;
        }

        ManagerTesting.restartAtWhitePracticeIntro = false;
        LoginPannel.SetActive(false);
        welcomePannel.SetActive(false);
        testingPanel.SetActive(false);
        whitePracticeIntroPanel.SetActive(true);
        SetBrowserLoginVisible(false);
        return true;
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
        string password = inputStudentID.text;
        string email = inputEmailID.text.Trim();

        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
        {
            ShowLoginError("Email and password are required.");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowLoginError("Enter a valid email address.");
            return;
        }

        StartCoroutine(Authenticate(email, password));
    }

    public void ReceiveBrowserLogin(string loginJson)
    {
        LoginRequest login;
        try
        {
            login = JsonUtility.FromJson<LoginRequest>(loginJson);
        }
        catch (Exception)
        {
            ShowLoginError("Login details could not be read.");
            return;
        }

        if (login == null)
        {
            ShowLoginError("Email and password are required.");
            return;
        }

        inputStudentID.text = login.password;
        inputEmailID.text = login.email;
        OnGoButtonClicked();
    }

    private IEnumerator Authenticate(string email, string password)
    {
        string authUrl = GetAuthenticationUrl();
        if (string.IsNullOrWhiteSpace(authUrl))
        {
            inputStudentID.text = string.Empty;
            ShowLoginError("Authentication service is not configured.");
            yield break;
        }

        goButton.interactable = false;
        warningText.gameObject.SetActive(false);
        SetAuthenticationLoading(true);

        string json = JsonUtility.ToJson(new LoginRequest { email = email, password = password });
        using (UnityWebRequest request = new UnityWebRequest(authUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            inputStudentID.text = string.Empty;
            goButton.interactable = true;
            SetAuthenticationLoading(false);

            LoginResponse response = null;
            try
            {
                response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            }
            catch (Exception)
            {
                // The status-specific fallback below handles invalid service responses.
            }

            if (request.responseCode >= 200 && request.responseCode < 300 && response != null && response.approved && response.student != null)
            {
                approvedCertificationNumber = string.IsNullOrWhiteSpace(response.student.certificationNumber)
                    ? response.student.userId
                    : response.student.certificationNumber;
                approvedSessionReference = response.sessionReference;
                approvedResultToken = response.resultToken;
                approvedAuthenticationUrl = authUrl;
                CompleteApprovedLogin(JsonUtility.ToJson(response.student));
                yield break;
            }

            if (request.responseCode == 429)
            {
                ShowLoginError("Too many attempts. Try again later.");
            }
            else if (response != null && response.reason == "access_expired")
            {
                ShowLoginError("This training access has expired.");
            }
            else if (response != null && response.reason == "access_inactive")
            {
                ShowLoginError("This training access is inactive.");
            }
            else if (request.responseCode == 401)
            {
                ShowLoginError("The email or password is incorrect.");
            }
            else
            {
                ShowLoginError("Authentication is temporarily unavailable.");
            }
        }
    }

    private string GetAuthenticationUrl()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string configuredUrl = SmokeSchoolGetAuthApi();
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                return configuredUrl;
            }
        }
        catch (Exception)
        {
            // Fall through to the production default.
        }
#endif
        return FALLBACK_AUTH_URL;
    }

    public static string GetCertificationResultUrl()
    {
        if (string.IsNullOrWhiteSpace(approvedAuthenticationUrl) ||
            !Uri.TryCreate(approvedAuthenticationUrl, UriKind.Absolute, out Uri authenticationUri) ||
            (authenticationUri.Scheme != Uri.UriSchemeHttps && authenticationUri.Scheme != Uri.UriSchemeHttp) ||
            (authenticationUri.AbsolutePath != "/api/vr/login" && authenticationUri.AbsolutePath != "/api/vr/login/"))
        {
            return string.Empty;
        }

        UriBuilder resultUri = new UriBuilder(authenticationUri)
        {
            Path = "/api/vr/certification-attempts",
            Query = string.Empty,
            Fragment = string.Empty
        };
        return resultUri.Uri.AbsoluteUri;
    }

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

        approvedCertificationNumber = string.IsNullOrWhiteSpace(profile.certificationNumber)
            ? profile.userId
            : profile.certificationNumber;

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
        SetAuthenticationLoading(false);
        SetBrowserLoginVisible(false);
    }

    private void ShowLoginError(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        LoginPannel.SetActive(true);
        welcomePannel.SetActive(false);
        SetAuthenticationLoading(false);
        SetBrowserLoginVisible(true);
    }

    private void SetBrowserLoginVisible(bool visible)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            SmokeSchoolSetLoginOverlayVisible(visible);
        }
        catch (Exception)
        {
            // Unity remains usable if the optional browser input bridge is unavailable.
        }
#endif
    }

    private void SetAuthenticationLoading(bool loading)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            SmokeSchoolSetAuthenticationLoading(loading);
        }
        catch (Exception)
        {
            // The disabled Unity button still prevents duplicate requests.
        }
#endif
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
