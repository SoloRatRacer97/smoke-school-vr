using UnityEngine;
using UnityEngine.UI;
using TMPro;

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


    private const string EMAIL_KEY = "PLAYER_EMAIL";
    private const string NAME_KEY = "STUDENT_NAME";

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Skip login entirely in WebGL builds — keyboard input freezes the app
        playerEmail = "webgl@test.com";
        studentname = "WebGL Tester";
        PlayerPrefs.SetString(EMAIL_KEY, playerEmail);
        PlayerPrefs.SetString(NAME_KEY, studentname);
        PlayerPrefs.Save();
        if (Emailsent != null) Emailsent.text = playerEmail;
        if (Username != null) Username.text = studentname;
        if (EmailsentF != null) EmailsentF.text = playerEmail;
        if (UsernameF != null) UsernameF.text = studentname;
        LoginPannel.SetActive(false);
        welcomePannel.SetActive(true);
        return;
#endif
        welcomePannel.SetActive(false);
        LoginPannel.SetActive(true);
        goButton.gameObject.SetActive(true);  // Keep it always active (optional UX)

        warningText.gameObject.SetActive(false);

        inputStudentID.onValueChanged.AddListener(delegate { HideWarningIfValid(); });
        inputEmailID.onValueChanged.AddListener(delegate { HideWarningIfValid(); });

        goButton.onClick.AddListener(OnGoButtonClicked);
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
        string studentID = inputStudentID.text;
        string emailID = inputEmailID.text;

//checking fields
        if (string.IsNullOrEmpty(studentID) || string.IsNullOrEmpty(emailID))
        {
            warningText.text = "All fields are required.";
            warningText.gameObject.SetActive(true);
            return;
        }

        playerEmail = emailID;
        studentname = studentID;

        PlayerPrefs.SetString(EMAIL_KEY, playerEmail);
        PlayerPrefs.SetString(NAME_KEY, studentname);
        PlayerPrefs.Save();

        Debug.Log("before Mail : " + emailID);
        Debug.Log("before Name : " + studentID);

        //end screen 
        Emailsent.text = playerEmail;
        Username.text = studentname;

        EmailsentF.text = playerEmail;
        UsernameF.text = studentname;

        ScreenshotSender.messageToSend = $"Student ID: {studentID}\nEmail ID: {emailID}";

        Debug.Log($"Student ID: {studentID}, Email ID: {emailID}");

        warningText.gameObject.SetActive(false);
        LoginPannel.SetActive(false);
        welcomePannel.SetActive(true);
        Debug.Log("ID Name : " + studentname);
        Debug.Log("Mail : " + playerEmail);

    }
}
 