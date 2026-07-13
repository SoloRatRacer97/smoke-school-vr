using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Mail;
using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

public class DataInput_Fields : MonoBehaviour
{
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


    [Header("Authentication")]
    [SerializeField] private bool authenticationEnabled = false;
    [SerializeField] private string editorAuthBaseUrl = "http://localhost:8888";
    [SerializeField] private string authMePath = "/api/auth/me";
    [SerializeField] private string authLoginPath = "/api/auth/login";


    private const string EMAIL_KEY = "PLAYER_EMAIL";
    private const string NAME_KEY = "STUDENT_NAME";

    [Serializable]
    private class AuthLoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    private class AuthUser
    {
        public string id;
        public string email;
        public string displayName;
        public string studentId;
    }

    [Serializable]
    private class AuthResponse
    {
        public bool ok;
        public string error;
        public AuthUser user;
    }

    void Start()
    {
        ConfigureLoginFields();
        ShowLoginPanel();

        SetWarning("Checking saved login...");

        if (inputStudentID != null)
        {
            inputStudentID.onValueChanged.AddListener(delegate { HideWarningIfValid(); });
        }

        if (inputEmailID != null)
        {
            inputEmailID.onValueChanged.AddListener(delegate { HideWarningIfValid(); });
        }

        if (goButton != null && !HasPersistentLoginClick())
        {
            goButton.onClick.AddListener(OnGoButtonClicked);
        }

        playerEmail = string.Empty;
        studentname = string.Empty;

        if (!authenticationEnabled)
        {
            if (checkSceneReload == 1)
            {
                string savedEmail = PlayerPrefs.GetString(EMAIL_KEY, string.Empty);
                string savedName = PlayerPrefs.GetString(NAME_KEY, string.Empty);
                if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedName))
                {
                    CompleteLocalLogin(savedName, savedEmail);
                    return;
                }
            }

            HideWarning();
            SetLoginInteractable(true);
            return;
        }

        StartCoroutine(CheckExistingSession());
    }

    void HideWarningIfValid()
    {

        if (inputStudentID != null
            && inputEmailID != null
            && !string.IsNullOrEmpty(inputStudentID.text)
            && !string.IsNullOrEmpty(inputEmailID.text))
        {
            HideWarning();
        }
    }

    public void OnGoButtonClicked()
    {
        if (inputStudentID == null || inputEmailID == null)
        {
            SetWarning("Sign in form is not configured.");
            return;
        }

        if (!authenticationEnabled)
        {
            CompleteLocalLoginFromForm();
            return;
        }

        string email = inputStudentID.text.Trim();
        string password = inputEmailID.text;

        // Check fields.
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SetWarning("Email and password are required.");
            return;
        }

        if (!IsValidEmail(email))
        {
            SetWarning("Enter a valid email address.");
            return;
        }

        StartCoroutine(Login(email, password));

    }

    private IEnumerator CheckExistingSession()
    {
        SetLoginInteractable(false);

        using (UnityWebRequest request = UnityWebRequest.Get(BuildAuthUrl(authMePath)))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "application/json");
            yield return request.SendWebRequest();

            if (IsSuccess(request, out AuthResponse authResponse) && authResponse.user != null)
            {
                CompleteAuthenticatedLogin(authResponse.user, authResponse.user.email);
                yield break;
            }
        }

        HideWarning();
        ShowLoginPanel();
        SetLoginInteractable(true);
    }

    private IEnumerator Login(string email, string password)
    {
        SetLoginInteractable(false);
        SetWarning("Signing in...");

        AuthLoginRequest payload = new AuthLoginRequest
        {
            email = email,
            password = password
        };

        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        using (UnityWebRequest request = new UnityWebRequest(BuildAuthUrl(authLoginPath), UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (IsSuccess(request, out AuthResponse authResponse) && authResponse.user != null)
            {
                inputEmailID.text = string.Empty;
                CompleteAuthenticatedLogin(authResponse.user, email);
                yield break;
            }

            SetWarning(BuildLoginError(request));
            SetLoginInteractable(true);
        }
    }

    private void CompleteAuthenticatedLogin(AuthUser user, string fallbackEmail)
    {
        playerEmail = string.IsNullOrEmpty(user.email) ? fallbackEmail : user.email;
        studentname = ResolveStudentName(user, playerEmail);

        PlayerPrefs.SetString(EMAIL_KEY, playerEmail);
        PlayerPrefs.SetString(NAME_KEY, studentname);
        PlayerPrefs.Save();

        Debug.Log("Authenticated Mail : " + playerEmail);
        Debug.Log("Authenticated Name : " + studentname);

        // End screen.
        SetIdentityText(Emailsent, playerEmail);
        SetIdentityText(Username, studentname);

        SetIdentityText(EmailsentF, playerEmail);
        SetIdentityText(UsernameF, studentname);

        ScreenshotSender.messageToSend = $"Student ID: {studentname}\nEmail ID: {playerEmail}";

        Debug.Log($"Student ID: {studentname}, Email ID: {playerEmail}");

        HideWarning();
        ShowWelcomePanel();
        SetLoginInteractable(true);
        Debug.Log("ID Name : " + studentname);
        Debug.Log("Mail : " + playerEmail);
    }

    private void CompleteLocalLoginFromForm()
    {
        string studentID = inputStudentID.text.Trim();
        string emailID = inputEmailID.text.Trim();

        if (string.IsNullOrEmpty(studentID) || string.IsNullOrEmpty(emailID))
        {
            SetWarning("All fields are required.");
            return;
        }

        if (!IsValidEmail(emailID))
        {
            SetWarning("Enter a valid email address.");
            return;
        }

        CompleteLocalLogin(studentID, emailID);
    }

    private void CompleteLocalLogin(string studentID, string emailID)
    {
        playerEmail = emailID;
        studentname = studentID;

        PlayerPrefs.SetString(EMAIL_KEY, playerEmail);
        PlayerPrefs.SetString(NAME_KEY, studentname);
        PlayerPrefs.Save();

        Debug.Log("Local test login Mail : " + playerEmail);
        Debug.Log("Local test login Name : " + studentname);

        SetIdentityText(Emailsent, playerEmail);
        SetIdentityText(Username, studentname);

        SetIdentityText(EmailsentF, playerEmail);
        SetIdentityText(UsernameF, studentname);

        ScreenshotSender.messageToSend = $"Student ID: {studentname}\nEmail ID: {playerEmail}";

        HideWarning();
        ShowWelcomePanel();
        SetLoginInteractable(true);
    }

    private void ConfigureLoginFields()
    {
        if (inputStudentID != null)
        {
            inputStudentID.contentType = authenticationEnabled
                ? InputField.ContentType.EmailAddress
                : InputField.ContentType.Standard;
            inputStudentID.text = PlayerPrefs.GetString(authenticationEnabled ? EMAIL_KEY : NAME_KEY, string.Empty);
            SetPlaceholder(inputStudentID, authenticationEnabled ? "Email" : "Student ID");
        }

        if (inputEmailID != null)
        {
            inputEmailID.contentType = authenticationEnabled
                ? InputField.ContentType.Password
                : InputField.ContentType.EmailAddress;
            inputEmailID.text = authenticationEnabled ? string.Empty : PlayerPrefs.GetString(EMAIL_KEY, string.Empty);
            SetPlaceholder(inputEmailID, authenticationEnabled ? "Password" : "Email");
        }

        SetLoginInteractable(true);
    }

    private void SetPlaceholder(InputField inputField, string placeholderText)
    {
        if (inputField.placeholder is Text textPlaceholder)
        {
            textPlaceholder.text = placeholderText;
        }

        inputField.ForceLabelUpdate();
    }

    private string BuildAuthUrl(string path)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return path;
#else
        return editorAuthBaseUrl.TrimEnd('/') + path;
#endif
    }

    private bool IsSuccess(UnityWebRequest request, out AuthResponse authResponse)
    {
        authResponse = null;

        bool httpSuccess = request.responseCode >= 200 && request.responseCode < 300;
        if (!httpSuccess || string.IsNullOrEmpty(request.downloadHandler.text))
        {
            return false;
        }

        try
        {
            authResponse = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
            return authResponse != null && authResponse.ok;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not parse auth response: " + exception.Message);
            return false;
        }
    }

    private string BuildLoginError(UnityWebRequest request)
    {
        if (request.responseCode == 401)
        {
            return "Invalid email or password.";
        }

        if (request.responseCode >= 500)
        {
            return "Sign in is unavailable. Try again later.";
        }

        return "Could not sign in. Check your connection and try again.";
    }

    private string ResolveStudentName(AuthUser user, string email)
    {
        if (!string.IsNullOrEmpty(user.studentId))
        {
            return user.studentId;
        }

        if (!string.IsNullOrEmpty(user.displayName))
        {
            return user.displayName;
        }

        int atIndex = email.IndexOf('@');
        return atIndex > 0 ? email.Substring(0, atIndex) : email;
    }

    private void SetIdentityText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void ShowLoginPanel()
    {
        if (welcomePannel != null)
        {
            welcomePannel.SetActive(false);
        }

        if (LoginPannel != null)
        {
            LoginPannel.SetActive(true);
        }

        if (goButton != null)
        {
            goButton.gameObject.SetActive(true);
        }
    }

    private void ShowWelcomePanel()
    {
        if (LoginPannel != null)
        {
            LoginPannel.SetActive(false);
        }

        if (welcomePannel != null)
        {
            welcomePannel.SetActive(true);
        }
    }

    private void SetLoginInteractable(bool isInteractable)
    {
        if (goButton != null)
        {
            goButton.interactable = isInteractable;
        }

        if (inputStudentID != null)
        {
            inputStudentID.interactable = isInteractable;
        }

        if (inputEmailID != null)
        {
            inputEmailID.interactable = isInteractable;
        }
    }

    private void SetWarning(string message)
    {
        if (warningText == null)
        {
            return;
        }

        warningText.text = message;
        warningText.gameObject.SetActive(true);
    }

    private void HideWarning()
    {
        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }

    private bool HasPersistentLoginClick()
    {
        int eventCount = goButton.onClick.GetPersistentEventCount();
        for (int index = 0; index < eventCount; index++)
        {
            if (goButton.onClick.GetPersistentTarget(index) == this
                && goButton.onClick.GetPersistentMethodName(index) == nameof(OnGoButtonClicked))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidEmail(string email)
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
 
