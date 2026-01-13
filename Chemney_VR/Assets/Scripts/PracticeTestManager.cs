using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class PracticeTestManager : MonoBehaviour
{
    [Header("Video Playback")]
    public VideoPlayer videoPlayer;

    [Header("Selection Buttons (0-100%)")]
    public List<Button> selectionButtons;

    [Header("Question Buttons")]
    public List<Button> questionButtons;

    [Header("UI Fields for Feedback")]
    public List<TextMeshProUGUI> answerTexts;
    public List<TextMeshProUGUI> remarksSelectedTexts;
    public List<TextMeshProUGUI> remarksFeedbackTexts;

    [Header("Per-Question Result Panel")]
    public GameObject resultPanel;
    public TextMeshProUGUI targetOpacityText;
    public TextMeshProUGUI yourReadingText;
    public TextMeshProUGUI resultSummaryText;

    [Header("Question Info")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI testTypeText;

    [Header("Panels")]
    public GameObject practicePanel;
    public GameObject actualTestPanel;
    public GameObject practiceCompletePanel;

    private List<int> correctAnswers = new List<int>();
    private List<int> userSelections = new List<int>();
    private List<string> feedbackResults = new List<string>();

    private Dictionary<int, List<string>> videoGroups = new Dictionary<int, List<string>>();

    private int currentQuestion = -1;
    private int currentSelection = -1;
    private bool[] questionAnswered;
    private const int totalQuestions = 25;

    void Start()
    {
        LoadAllVideos();

        List<int> keys = new List<int>(videoGroups.Keys);
        for (int i = 0; i < totalQuestions; i++)
        {
            int percent = keys[Random.Range(0, keys.Count)];
            correctAnswers.Add(percent);
        }

        questionAnswered = new bool[totalQuestions];
        AssignButtonListeners();

        resultPanel.SetActive(false);
        actualTestPanel.SetActive(false);
        practicePanel.SetActive(true);
        practiceCompletePanel.SetActive(false);

        SetTestType("Practice White Smoke");

        currentQuestion = 0;
        LoadVideo(currentQuestion);
        UpdateQuestionButtonInteractivity();

        StartCoroutine(LoadFile());
    }

    void LoadAllVideos()
    {
        // Initialize the dictionary
        for (int i = 0; i <= 100; i += 5)
        {
            videoGroups[i] = new List<string>();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // For WebGL builds, pre-define your video files
        string[] videoFiles = {
            "Point0.mp4", "Point5.mp4", "Point10.mp4", "Point15.mp4", "Point20.mp4", "Point25.mp4",
            "Point30.mp4", "Point35.mp4", "Point40.mp4", "Point45.mp4", "Point50.mp4", "Point55.mp4",
            "Point60.mp4", "Point65.mp4", "Point70.mp4", "Point75.mp4", "Point80.mp4", "Point85.mp4",
            "Point90.mp4", "Point95.mp4", "Point100.mp4"
        };
        
        // Group videos by percentage
        foreach (string filename in videoFiles)
        {
            int percent = ExtractPercentageFromFilename(filename);
            if (videoGroups.ContainsKey(percent))
            {
                videoGroups[percent].Add(filename);
            }
        }
        
        Debug.Log($"WebGL: Loaded {videoFiles.Length} videos into groups");
#else
        // For Editor and Desktop builds, scan directory
        string folderPath = Application.streamingAssetsPath;

        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, "*.mp4");

            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                int percent = ExtractPercentageFromFilename(filename);
                if (videoGroups.ContainsKey(percent))
                {
                    videoGroups[percent].Add(filename);
                }
            }

            Debug.Log($"Desktop: Loaded {files.Length} videos into groups");
        }
        else
        {
            Debug.LogError("StreamingAssets directory not found: " + folderPath);
        }
#endif
    }

    void AssignButtonListeners()
    {
        foreach (Button btn in selectionButtons)
        {
            int value = ExtractValueFromButtonName(btn.name);
            btn.onClick.AddListener(() => OnUserSelected(value));
        }

        for (int i = 0; i < questionButtons.Count; i++)
        {
            int index = i;
            questionButtons[i].onClick.AddListener(() => OnQuestionClicked(index));
        }
    }

    void OnQuestionClicked(int index)
    {
        if (index != currentQuestion + 1) return;
        currentQuestion = index;
        currentSelection = -1;
        LoadVideo(index);
    }

    void LoadVideo(int index)
    {
        int targetOpacity = correctAnswers[index];
        List<string> options = videoGroups.ContainsKey(targetOpacity) ? videoGroups[targetOpacity] : null;

        if (options == null || options.Count == 0)
        {
            Debug.LogError($" No video found for {targetOpacity}%");
            return;
        }

        string filename = options[Random.Range(0, options.Count)];
        StartCoroutine(LoadVideoFromStreamingAssets(filename));

        questionText.text = $"Question: {index + 1}";
        targetOpacityText.text = "";
        yourReadingText.text = "";
        resultSummaryText.text = "";

        resultPanel.SetActive(false);
    }

    void OnUserSelected(int selection)
    {
        if (currentSelection != -1) return;

        currentSelection = selection;
        int correct = correctAnswers[currentQuestion];
        int diff = Mathf.Abs(selection - correct);

        if (userSelections.Count <= currentQuestion)
            userSelections.Add(selection);
        else
            userSelections[currentQuestion] = selection;

        string feedback = selection == correct
            ? "Your Reading was Perfect!"
            : diff <= 15
                ? $"Your Reading was {diff}% a bit {(selection > correct ? "high" : "low")}"
                : $"Your Reading was {diff}% too {(selection > correct ? "high" : "low")}";

        if (feedbackResults.Count <= currentQuestion)
            feedbackResults.Add(feedback);
        else
            feedbackResults[currentQuestion] = feedback;

        if (answerTexts.Count > currentQuestion)
            answerTexts[currentQuestion].text = $"{selection}";

        if (remarksSelectedTexts.Count > currentQuestion)
            remarksSelectedTexts[currentQuestion].text = $"{selection}%";

        if (remarksFeedbackTexts.Count > currentQuestion)
            remarksFeedbackTexts[currentQuestion].text = feedback;

        videoPlayer.Pause();
        videoPlayer.gameObject.SetActive(false);

        resultPanel.SetActive(true);
        targetOpacityText.text = correct.ToString();
        yourReadingText.text = selection.ToString();
        resultSummaryText.text = feedback;

        questionAnswered[currentQuestion] = true;
        UpdateQuestionButtonInteractivity();

        //  Check if all questions are answered
        bool allAnswered = true;
        for (int i = 0; i < totalQuestions; i++)
        {
            if (!questionAnswered[i])
            {
                allAnswered = false;
                break;
            }
        }

        if (allAnswered)
        {
            Invoke(nameof(EndPracticeTest), 1.5f); // optional delay
        }
    }

    void UpdateQuestionButtonInteractivity()
    {
        for (int i = 0; i < questionButtons.Count; i++)
        {
            questionButtons[i].interactable = (i == currentQuestion + 1 && questionAnswered[currentQuestion]);
        }

        if (currentQuestion >= 0 && !questionAnswered[currentQuestion])
        {
            questionButtons[currentQuestion].interactable = true;
        }
    }

    void EndPracticeTest()
    {
        practicePanel.SetActive(false);
        resultPanel.SetActive(false);
        practiceCompletePanel.SetActive(true);
    }

    void SetTestType(string testName)
    {
        if (testTypeText != null)
            testTypeText.text = testName;
    }

    int ExtractPercentageFromFilename(string filename)
    {
        string nameOnly = Path.GetFileNameWithoutExtension(filename);
        string digits = Regex.Match(nameOnly, @"\d+").Value;
        return int.TryParse(digits, out int value) ? value : -1;
    }

    int ExtractValueFromButtonName(string name)
    {
        if (name.StartsWith("Point "))
        {
            string valueStr = name.Substring(6);
            if (int.TryParse(valueStr, out int value))
                return value;
        }
        return -1;
    }

    IEnumerator LoadVideoFromStreamingAssets(string filename)
    {
        string fullPath = "";

#if UNITY_WEBGL && !UNITY_EDITOR
        // For WebGL, use relative path to StreamingAssets
        fullPath = "StreamingAssets/" + filename;
        
        Debug.Log("WebGL: Loading video from relative path: " + fullPath);
        
        // For WebGL, directly set the VideoPlayer URL without UnityWebRequest
        videoPlayer.url = fullPath;
        videoPlayer.gameObject.SetActive(true);
        videoPlayer.isLooping = true;
        videoPlayer.Play();
        
        Debug.Log("WebGL: Video URL set successfully: " + filename);
        yield break;
#else
        // For Editor and Desktop builds
        fullPath = "file://" + Path.Combine(Application.streamingAssetsPath, filename);

        Debug.Log("Desktop: Loading video from: " + fullPath);

        UnityWebRequest www = UnityWebRequest.Get(fullPath);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to load video '{filename}' - {www.error}");
            yield break;
        }

        // Set the VideoPlayer's URL and play
        videoPlayer.url = fullPath;
        videoPlayer.gameObject.SetActive(true);
        videoPlayer.isLooping = true;
        videoPlayer.Play();

        Debug.Log("Desktop: Video loaded successfully: " + filename);
#endif
    }

    IEnumerator LoadFile()
    {
        string jsonData = "";
        string filePath = "";

#if UNITY_WEBGL && !UNITY_EDITOR
    // For WebGL, use the correct relative path to StreamingAssets
    filePath = Application.streamingAssetsPath + "/config.json";
    
    UnityWebRequest request = UnityWebRequest.Get(filePath);
    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        jsonData = request.downloadHandler.text;
        Debug.Log("WebGL: Loaded JSON Data: " + jsonData);
        
        // Process your JSON data here
        ProcessJsonData(jsonData);
    }
    else
    {
        Debug.LogError("WebGL: Failed to load config.json from: " + filePath + " - Error: " + request.error);
    }
#else
        // For Editor and Desktop builds
        filePath = Path.Combine(Application.streamingAssetsPath, "config.json");

        if (File.Exists(filePath))
        {
            try
            {
                jsonData = File.ReadAllText(filePath);
                Debug.Log("Desktop: Loaded JSON Data: " + jsonData);

                // Process your JSON data here
                ProcessJsonData(jsonData);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Desktop: Error reading config.json: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("Desktop: config.json not found at: " + filePath);
        }

        // Add yield return null to ensure all code paths return a value
        yield return null;
#endif
    }

    // Add this method to process your JSON data
    void ProcessJsonData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogWarning("JSON data is empty or null");
            return;
        }

        try
        {
            // Parse and process your JSON here
            // Example: var data = JsonUtility.FromJson<YourDataClass>(jsonData);
            Debug.Log("Processing JSON data...");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error processing JSON data: " + e.Message);
        }
    }
}