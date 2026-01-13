using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;

public class SmokeTestManager : MonoBehaviour
{
    [Header("Video and UI References")]
    public VideoPlayer videoPlayer;
    public List<Button> selectionButtons;

    [Header("Reading Panel Fields")]
    public List<TextMeshProUGUI> whiteReadingTexts;

    [Header("Result Panel White Smoke Fields")]
    public List<TextMeshProUGUI> questionNumberTexts;
    public List<TextMeshProUGUI> selectedValueTexts;
    public List<TextMeshProUGUI> actualValueTexts;
    public List<TextMeshProUGUI> scorePerQuestionTexts;

    [Header("Per-Question Result Panel")]
    public TextMeshProUGUI targetOpacityText;
    public TextMeshProUGUI yourReadingText;
    public TextMeshProUGUI resultSummaryText;
    public TextMeshProUGUI questionNumberText;

    [Header("Final Result Panel Fields")]
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI yourScoreText;
    public TextMeshProUGUI maxScoreText;
    public TextMeshProUGUI testTypeText;

    [Header("Panels")]
    public GameObject whiteTestPanel;
    public GameObject finalResultPanel;
    public GameObject resultPanel;
    public GameObject nextPanelAfterTest; // assign in inspector

    [Header("Control Buttons")]
    public Button scratchButton;
    public Button continueButton;
    public List<Button> questionEditButtons;

    [Header("Video Clip Lists")]
    public List<string> whiteSmokeVideos = new List<string>();

    [Header("Test Configuration")]
    [SerializeField] private int totalQuestions = 30;
    [SerializeField] private int scoreThreshold = 37;
    [SerializeField] private float videoLoadDelay = 0.1f;

    // Private fields
    private List<string> currentVideoSet = new List<string>();
    private Dictionary<int, List<string>> videoGroups = new Dictionary<int, List<string>>();
    private List<string> selectedVideos = new List<string>();

    private List<int> correctAnswers = new List<int>();
    private List<int> userSelections = new List<int>();
    private List<string> feedbackResults = new List<string>();
    private List<int> scoreResults = new List<int>();

    public bool isInScratchMode = false;
    public int returnToQuestion = -1;
    public int currentQuestion = 0;
    public int currentSelection = -1;
    public int lastScratchedIndex = -1;

    public enum TestStage { White, Completed }
    private TestStage currentStage = TestStage.White;

    // Events for better decoupling
    public event Action<int> OnQuestionAnswered;
    public event Action<int> OnTestCompleted;

    #region Unity Lifecycle

    void Start()
    {
        InitializeTest();
    }

    void OnDestroy()
    {
        CleanupEventListeners();
    }

    #endregion

    #region Configuration Management

    IEnumerator LoadConfigFile()
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
        
        // Ensure all code paths return a value
        yield return null;
#endif
    }

    // Process JSON data method
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

    #endregion

    #region Initialization

    void InitializeTest()
    {
        LoadAllVideos();
        StartCoroutine(LoadConfigFile());
        StartNewTest(whiteSmokeVideos);
        AssignButtonListeners();
        SetupControlButtons();
        SetupVideoPlayerCallbacks();
    }

    void LoadAllVideos()
    {
        whiteSmokeVideos.Clear();
        
        // Initialize the dictionary for video groups
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
        
        whiteSmokeVideos.AddRange(videoFiles);
        
        // Group videos by percentage
        foreach (string filename in videoFiles)
        {
            int percent = ExtractPercent(filename);
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
                int percent = ExtractPercent(filename);
                
                whiteSmokeVideos.Add(filename);
                
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

void SetupControlButtons()
{
    if (scratchButton != null)
        scratchButton.onClick.AddListener(Scratch);
    
    if (continueButton != null)
        continueButton.onClick.AddListener(ProceedToNextPanel);

    for (int i = 0; i < questionEditButtons.Count; i++)
    {
        int index = i; // Capture for closure
        questionEditButtons[i].onClick.AddListener(() => EditQuestion(index));
    }
}

void ProceedToNextPanel()
{
    if (isInScratchMode && returnToQuestion >= 0)
    {
        // Return to the original question where the scratch button was pressed
        currentQuestion = returnToQuestion;
        returnToQuestion = -1;
        isInScratchMode = false;
        LoadVideo(currentQuestion);
    }
    else if (currentStage == TestStage.Completed)
    {
        // Proceed to the next panel after the test is completed
        if (nextPanelAfterTest != null)
        {
            nextPanelAfterTest.SetActive(true);
        }
    }
}

    void SetupVideoPlayerCallbacks()
    {
        if (videoPlayer != null)
        {
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }
    }

    void CleanupEventListeners()
    {
        if (videoPlayer != null)
        {
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }

    #endregion

    #region Test Management

    void StartNewTest(List<string> videoList)
    {
        if (videoList == null || videoList.Count == 0)
        {
            Debug.LogError("Cannot start test: video list is null or empty");
            return;
        }

        ResetTestData();
        GenerateTestQuestions();
        
        currentVideoSet = new List<string>(selectedVideos);
        currentQuestion = 0;
        TogglePanelVisibility();
        LoadVideo(currentQuestion);
    }

    void ResetTestData()
    {
        selectedVideos.Clear();
        correctAnswers.Clear();
        userSelections.Clear();
        feedbackResults.Clear();
        scoreResults.Clear();
        
        isInScratchMode = false;
        returnToQuestion = -1;
        currentSelection = -1;
        lastScratchedIndex = -1;
    }

    void GenerateTestQuestions()
    {
        if (videoGroups.Count == 0)
        {
            Debug.LogError("No video groups available for test generation");
            return;
        }

        List<int> keys = new List<int>(videoGroups.Keys);
        
        for (int i = 0; i < totalQuestions; i++)
        {
            int percent = keys[UnityEngine.Random.Range(0, keys.Count)];
            var availableVideos = videoGroups[percent];
            
            if (availableVideos.Count > 0)
            {
                string clip = availableVideos[UnityEngine.Random.Range(0, availableVideos.Count)];
                selectedVideos.Add(clip);
            }
            else
            {
                Debug.LogWarning($"No videos available for {percent}% opacity");
            }
        }
    }

    #endregion

    #region Video Management

    void LoadVideo(int index)
    {
        if (index < 0 || index >= currentVideoSet.Count)
        {
            Debug.LogWarning($"Invalid video index: {index}");
            ShowSummary();
            return;
        }

        EnableSelectionButtons(false);
        
        string video = currentVideoSet[index];
        StartCoroutine(LoadVideoFromStreamingAssets(video));

        int correct = ExtractPercent(video);
        UpdateQuestionUI(index, correct);
        ResetSelectionState();
    }

    void UpdateQuestionUI(int index, int correct)
    {
        if (questionNumberText != null)
            questionNumberText.text = $"{index + 1}";

        // Ensure correctAnswers list is properly sized
        while (correctAnswers.Count <= index)
            correctAnswers.Add(0);
        
        correctAnswers[index] = correct;

        // Clear previous results
        if (targetOpacityText != null) targetOpacityText.text = "";
        if (yourReadingText != null) yourReadingText.text = "";
        if (resultSummaryText != null) resultSummaryText.text = "";
    }

    void ResetSelectionState()
    {
        currentSelection = -1;
    }

    // Updated LoadVideoFromStreamingAssets method from PracticeTestManager
    IEnumerator LoadVideoFromStreamingAssets(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            Debug.LogError("Cannot load video: filename is null or empty");
            yield break;
        }

        string fullPath = "";

#if UNITY_WEBGL && !UNITY_EDITOR
        // For WebGL, use relative path to StreamingAssets
        fullPath = "StreamingAssets/" + filename;
        
        Debug.Log("WebGL: Loading video from relative path: " + fullPath);
        
        // For WebGL, directly set the VideoPlayer URL without UnityWebRequest
        if (videoPlayer != null)
        {
            videoPlayer.url = fullPath;
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }
        
        Debug.Log("WebGL: Video URL set successfully: " + filename);
        
        // Enable buttons after a short delay
        StartCoroutine(EnableButtonsAfterDelay());
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
        if (videoPlayer != null)
        {
            videoPlayer.url = fullPath;
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        Debug.Log("Desktop: Video loaded successfully: " + filename);
        
        // Enable buttons after a short delay
        StartCoroutine(EnableButtonsAfterDelay());
#endif
    }

    IEnumerator EnableButtonsAfterDelay()
    {
        yield return new WaitForSeconds(videoLoadDelay);
        EnableSelectionButtons(true);
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        if (vp != null)
        {
            vp.Play();
        }
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"VideoPlayer Error: {message}");
        // Could implement retry logic here
    }

    #endregion

    #region Answer Processing

    void OnAnswerSelected(int selection)
    {
        if (currentSelection != -1 || currentQuestion >= correctAnswers.Count)
        {
            Debug.LogWarning("Invalid answer selection state");
            return;
        }

        currentSelection = selection;
        ProcessAnswer(selection);
        OnQuestionAnswered?.Invoke(currentQuestion);
        
        if (ShouldProceedToNextQuestion())
        {
            ProceedToNextQuestion();
        }
    }

    bool ShouldProceedToNextQuestion()
    {
        return !(isInScratchMode && returnToQuestion >= 0);
    }

    void ProcessAnswer(int selection)
    {
        int correct = correctAnswers[currentQuestion];
        int difference = Mathf.Abs(selection - correct);

        // Ensure lists are properly sized
        EnsureListSize(userSelections, currentQuestion + 1, -1);
        EnsureListSize(feedbackResults, currentQuestion + 1, "");
        EnsureListSize(scoreResults, currentQuestion + 1, 0);

        userSelections[currentQuestion] = selection;
        
        var (feedback, score) = CalculateScore(selection, correct, difference);
        feedbackResults[currentQuestion] = feedback;
        scoreResults[currentQuestion] = score;

        UpdateUIWithAnswer(selection, correct, score);
        lastScratchedIndex = currentQuestion;
        
        // Pause and hide video
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
            videoPlayer.gameObject.SetActive(false);
        }
    }

    (string feedback, int score) CalculateScore(int selection, int correct, int difference)
    {
        string feedback;
        int score = 0;

        if (selection == correct)
        {
            feedback = "<color=green>Your reading was Perfect!</color>";
        }
        else if (difference <= 15)
        {
            string direction = selection > correct ? "High" : "Low";
            feedback = $"<color=yellow>Your reading was {selection}%, {difference}% {direction}</color>";
            score = difference <= 5 ? 1 : difference <= 10 ? 2 : 3;
        }
        else
        {
            string direction = selection > correct ? "High" : "Low";
            feedback = $"<color=red>Your reading was {selection}%, too {direction}</color>";
        }

        return (feedback, score);
    }

    void UpdateUIWithAnswer(int selection, int correct, int score)
    {
        // Update reading texts
        if (IsValidIndex(whiteReadingTexts, currentQuestion))
            whiteReadingTexts[currentQuestion].text = $"{selection}%";

        // Update result panel texts
        if (IsValidIndex(questionNumberTexts, currentQuestion))
            questionNumberTexts[currentQuestion].text = $"{currentQuestion + 1}";
        
        if (IsValidIndex(selectedValueTexts, currentQuestion))
            selectedValueTexts[currentQuestion].text = $"{selection}%";
        
        if (IsValidIndex(actualValueTexts, currentQuestion))
            actualValueTexts[currentQuestion].text = $"{correct}%";
        
        if (IsValidIndex(scorePerQuestionTexts, currentQuestion))
            scorePerQuestionTexts[currentQuestion].text = score.ToString();
    }

    void ProceedToNextQuestion()
    {
        if (isInScratchMode && returnToQuestion >= 0)
        {
            int temp = returnToQuestion;
            returnToQuestion = -1;
            currentQuestion = temp;
            isInScratchMode = false;
        }
        else
        {
            currentQuestion++;
        }

        StartCoroutine(LoadNextVideoDelayed());
    }

    IEnumerator LoadNextVideoDelayed()
    {
        yield return new WaitForSeconds(videoLoadDelay);
        NextVideo();
    }

    void NextVideo()
    {
        if (currentQuestion >= totalQuestions)
        {
            ShowSummary();
        }
        else
        {
            LoadVideo(currentQuestion);
        }
    }

    #endregion

    #region Scratch Mode & Editing

    void Scratch()
    {
        if (userSelections.Count == 0 || !HasAnsweredQuestions())
        {
            Debug.LogWarning("No questions answered yet. Cannot enter scratch mode.");
            return;
        }

        isInScratchMode = true;
        returnToQuestion = currentQuestion;
        EnableQuestionEditButtons();
        
        Debug.Log("Scratch mode enabled: select a question to re-edit.");
    }

    bool HasAnsweredQuestions()
    {
        return userSelections.Exists(selection => selection != -1);
    }

    void EnableQuestionEditButtons()
    {
        for (int i = 0; i < questionEditButtons.Count && i < userSelections.Count; i++)
        {
            if (userSelections[i] != -1)
            {
                questionEditButtons[i].interactable = true;
            }
        }
    }

    void EditQuestion(int index)
    {
        if (!IsValidEditIndex(index) && !isInScratchMode )
        {
            Debug.LogWarning($"Invalid edit: question {index} not answered yet or out of range.");
            return;
        }

        ReplaceVideoForQuestion(index);
        ClearQuestionData(index);
        PrepareQuestionForRetest(index);
    }

    bool IsValidEditIndex(int index)
    {
        return index >= 0 && 
               index < userSelections.Count && 
               userSelections[index] != -1;
    }

    void ReplaceVideoForQuestion(int index)
    {
        int correct = correctAnswers[index];
        string currentVideo = currentVideoSet[index];
        
        if (videoGroups.ContainsKey(correct) && videoGroups[correct].Count > 1)
        {
            var options = videoGroups[correct];
            string newClip;
            
            do
            {
                newClip = options[UnityEngine.Random.Range(0, options.Count)];
            } while (newClip == currentVideo);
            
            currentVideoSet[index] = newClip;
        }
    }

    void ClearQuestionData(int index)
    {
        // Clear UI elements
        if (IsValidIndex(whiteReadingTexts, index))
            whiteReadingTexts[index].text = "";
        
        if (IsValidIndex(selectedValueTexts, index))
            selectedValueTexts[index].text = "";
        
        if (IsValidIndex(scorePerQuestionTexts, index))
            scorePerQuestionTexts[index].text = "";
        
        if (IsValidIndex(actualValueTexts, index))
            actualValueTexts[index].text = "";

        // Clear data
        if (index < userSelections.Count)
            userSelections[index] = -1;
        
        if (index < scoreResults.Count)
            scoreResults[index] = 0;
    }

    void PrepareQuestionForRetest(int index)
    {
        currentSelection = -1;
        currentQuestion = index;
        
        LoadVideo(currentQuestion);
        DisableAllQuestionEditButtons();
        isInScratchMode = false;
    }

    void DisableAllQuestionEditButtons()
    {
        foreach (var btn in questionEditButtons)
        {
            if (btn != null)
                btn.interactable = false;
        }
    }

    #endregion

    #region Summary and Results

    void ShowSummary()
    {
        currentStage = TestStage.Completed;
        TogglePanelVisibility();
        
        StopVideoPlayback();
        CalculateAndDisplayResults();
        
        OnTestCompleted?.Invoke(CalculateTotalScore());
    }

    void StopVideoPlayback()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.gameObject.SetActive(false);
        }
    }

    void CalculateAndDisplayResults()
    {
        int totalScore = CalculateTotalScore();
        
        // Update result UI elements
        for (int i = 0; i < Mathf.Min(scoreResults.Count, totalQuestions); i++)
        {
            UpdateResultRowUI(i);
        }

        UpdateFinalScoreUI(totalScore);
    }

    int CalculateTotalScore()
    {
        int total = 0;
        for (int i = 0; i < scoreResults.Count; i++)
        {
            total += scoreResults[i];
        }
        return total;
    }

    void UpdateResultRowUI(int index)
    {
        if (index >= userSelections.Count || index >= correctAnswers.Count)
            return;

        int score = index < scoreResults.Count ? scoreResults[index] : 0;
        int selection = userSelections[index];
        int correct = correctAnswers[index];

        if (IsValidIndex(questionNumberTexts, index))
            questionNumberTexts[index].text = $"Q{index + 1}";
        
        if (IsValidIndex(selectedValueTexts, index))
            selectedValueTexts[index].text = $"{selection}%";
        
        if (IsValidIndex(actualValueTexts, index))
            actualValueTexts[index].text = $"{correct}%";
        
        if (IsValidIndex(scorePerQuestionTexts, index))
            scorePerQuestionTexts[index].text = score.ToString();
    }

    void UpdateFinalScoreUI(int totalScore)
    {
        if (totalScoreText != null)
            totalScoreText.text = "Total Score: " + totalScore;
        
        if (yourScoreText != null)
            yourScoreText.text = totalScore.ToString();
        
        if (maxScoreText != null)
            maxScoreText.text = scoreThreshold.ToString();
        
        if (testTypeText != null)
        {
            bool isQualified = totalScore <= scoreThreshold;
            testTypeText.text = isQualified ? 
                "<color=green>Qualified</color>" : 
                "<color=red>Unqualified</color>";
        }
    }

    #endregion

    #region UI Management

    void TogglePanelVisibility()
    {
        if (whiteTestPanel != null)
            whiteTestPanel.SetActive(currentStage == TestStage.White);
        
        if (finalResultPanel != null)
            finalResultPanel.SetActive(currentStage == TestStage.Completed);
        
        if (nextPanelAfterTest != null)
            nextPanelAfterTest.SetActive(currentStage == TestStage.Completed);
    }

    void EnableSelectionButtons(bool state)
    {
        foreach (var btn in selectionButtons)
        {
            if (btn != null)
                btn.interactable = state;
        }
    }

    void AssignButtonListeners()
    {
        foreach (Button btn in selectionButtons)
        {
            if (btn == null) continue;
            
            int val = ExtractPercent(btn.name);
            if (val == -1)
            {
                Debug.LogWarning($"Invalid button name for value extraction: {btn.name}");
                continue;
            }

            btn.onClick.AddListener(() => OnAnswerSelected(val));
        }
    }

    #endregion

    #region Utility Methods

    int ExtractPercent(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return -1;

        Match match = Regex.Match(Path.GetFileNameWithoutExtension(filename), @"\d+");
        return match.Success ? int.Parse(match.Value) : -1;
    }

    bool IsValidIndex<T>(List<T> list, int index)
    {
        return list != null && index >= 0 && index < list.Count;
    }

    void EnsureListSize<T>(List<T> list, int size, T defaultValue)
    {
        while (list.Count < size)
        {
            list.Add(defaultValue);
        }
    }

    #endregion

    #region Public API

    public void RestartTest()
    {
        if (whiteSmokeVideos.Count > 0)
        {
            currentStage = TestStage.White;
            StartNewTest(whiteSmokeVideos);
        }
    }

    public int GetCurrentScore()
    {
        return CalculateTotalScore();
    }

    public bool IsTestComplete()
    {
        return currentStage == TestStage.Completed;
    }

    public float GetTestProgress()
    {
        //  return totalQuestions > 0 ? (float)currentQuestion / totalQuestions : 0f;
        if (totalQuestions > 0)
        {

            return (float)currentQuestion / totalQuestions;
        }
        else
        {

         return   0f;
         }

    }

    #endregion
}