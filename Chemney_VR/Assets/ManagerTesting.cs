using System;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Random = UnityEngine.Random;
using UnityEngine.Events;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

public class ManagerTesting : MonoBehaviour
{
    public enum TestType { whitePractice, whiteTest, blackPractice, blackTest, TestComplete };
    public TestType currenttype;

    [SerializeField] VideoPlayer videoPlayer;

    [Header("Video Preloading System")]
    [SerializeField] VideoPlayer preloadVideoPlayer; // Second VideoPlayer for preloading
    [Tooltip("Enable/disable video preloading for instant playback")]
    public bool enablePreloading = true;
    private bool isNextVideoPrepared = false;
    private string nextVideoURL = "";
    private int nextVideoIndex = -1;

    private int[] answersValue = new int[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 };

    [Header("Question Navigation Settings")]
    [Tooltip("Enable automatic advance to next question (if false, user must click Next button)")]
    public bool useAutoAdvance = false;
    [Tooltip("Delay in seconds before auto-advancing to next question (only if useAutoAdvance is true)")]
    public float autoAdvanceDelay = 0.5f;
    private bool isAutoAdvancing = false;

    [Header("Next Button")]
    [Tooltip("Button that user clicks to advance to next question")]
    public Button btn_Next;
    private bool answerSelected = false;

    [Header("Refresh Button")]
    public Button Refresh;

    [Header("QuestionsValues")]
    public int[] questionvalues_practice_white;
    public int[] questionvalues_test_white;
    public int[] questionvalues_practice_black;
    public int[] questionvalues_test_black;

    [Header("Black Screen")]
    public GameObject blackScreen;
    public GameObject loadingImage;
    public float rotationSpeed = 200f;
    private RectTransform loadingImageRect;

    [Header("Answer Values")]
    public int[] answervalues_practice_white;
    public int[] answervalues_test_white;
    public int[] answervalues_practice_black;
    public int[] answervalues_test_black;

    [Header("Scratch Button")]
    public Button btn_Scratch;
    public bool scratchMode = false;
    int SCRATCHQUESTIONINDEX = -1;
    public int lastQuestionBeforeScratch = -1;


    [Header("Black Smoke Tutorials")]
    public int[] currentQuestionValues;
    public string[] whiteVideoFiles;
    public string[] blackVideoFiles;
    public Button[] btn_points;

    [Header("Questions")]
    public Button[] btn_questions;

    [Header("Current Text")]
    public TMP_Text Txt_currentCompleteTest;
    public TMP_Text questionNmbr_text;
    public TMP_Text CurrentTest_txt;

    [Header("smokeTutorials")]
    public GameObject tutorialsPannel;
    public Button btn_SkipPracticeTest;

    [Header("Videos By Opacity")]
    public List<string> OpacityVideos;

    public TMP_Text[] userSelectedValue;
    public int currentQuestionIndex = 0;

    [Header("REMARKS PANNEL")]
    public GameObject RemarksPannel;
    public TextMeshProUGUI targetOpacityText;
    public TextMeshProUGUI yourReadingText;
    public TextMeshProUGUI resultSummaryText;

    [Header("Signature PANNEL")]
    public GameObject SignaturePannel;
    public Button Btn_Submission;
    public Button Btn_Clear;

    [Header("Result Pannel")]
    public Button openresultPannelButton;
    public TMP_Text[] YourWhiteSelectedValue;
    public TMP_Text[] YourBlackSelectedValue;
    public TMP_Text[] WhiteOpacityActualValue;
    public TMP_Text[] BlackOpacityActualValue;
    public TMP_Text[] whiteSmokeScore;
    public TMP_Text[] BlackSmokeScore;

    [Header("Final Result Panels")]
    public GameObject QualifiedPanel;
    public GameObject NotPassedPanel;
    public TMP_Text YourTotalScore;
    public TMPro.TextMeshProUGUI endTestButtonText;

    [Header("REVIEWPHASE PANNEL")]
    public GameObject TestingCompletePannel;
    public Button Btn_Continue;
    public TMP_Text Txt_ContinueText;

    bool reviewphase = false;
    int REVIEWQUESTIONINDEX = -1;

    [Header("Duplication Check List")]
    public List<int> shuffledAnswerIndices = new List<int>();

    public string currentSmokeType = "white";
    public int currentSmokePercentage = 0;
    private int currentVideoIndex = 0;
    private ChimneySmokeVideoData.SmokeTypeGroup currentTypeGroup;
    public ChimneySmokeVideoData videoData;

    // MODIFIED: Separate scores for white and black smoke tests
    int whiteTestScore = 0;
    int blackTestScore = 0;

    // NEW: Flag to track if first question has been loaded
    private bool isFirstQuestionLoaded = false;

    public bool isBlackSmokeCompleted = false;


    public GameObject SubmissionButton;
    public GameObject BlackPracticeButton;
    public GameObject WhiteTestButton;
    public GameObject BlackTestButton;

    public ManageWhitePracticeTest manageWhitePracticeTest;
    public MangerBlackPractice mangerBlackPractice;
    private void chkgrptype()
    {
        if (currenttype == TestType.whitePractice || currenttype == TestType.whiteTest)
        {
            currentSmokeType = "white";
            Debug.Log("White Smoke Type Selected");
        }
        else if (currenttype == TestType.blackPractice || currenttype == TestType.blackTest)
        {
            currentSmokeType = "black";
            Debug.Log("Black Smoke Type Selected");
        }
    }

    void Start()
    {
        blackScreen.SetActive(false);
        openresultPannelButton.gameObject.SetActive(false);
        btn_SkipPracticeTest.onClick.AddListener(OnSkipPractice);

        // Initialize Next button
        if (btn_Next != null)
        {
            btn_Next.onClick.AddListener(OnNextButtonClicked);
            btn_Next.gameObject.SetActive(false); // Hidden by default
        }
        else if (!useAutoAdvance)
        {
            Debug.LogWarning("Next Button is not assigned! Please assign it in the Inspector or enable Auto-Advance.");
        }

        questionvalues_practice_white = new int[btn_questions.Length];
        questionvalues_test_white = new int[btn_questions.Length];
        questionvalues_practice_black = new int[btn_questions.Length];
        questionvalues_test_black = new int[btn_questions.Length];

        answervalues_practice_white = new int[btn_questions.Length];
        answervalues_test_white = new int[btn_questions.Length];
        answervalues_practice_black = new int[btn_questions.Length];
        answervalues_test_black = new int[btn_questions.Length];

        // Setup preload video player
        InitializePreloadVideoPlayer();

        // Keep skip button visible at all times
        btn_SkipPracticeTest.gameObject.SetActive(true);

        // Handle test type logic
        if (currenttype == TestType.whitePractice)
        {
            btn_Scratch.gameObject.SetActive(false);
            Txt_currentCompleteTest.text = "White Smoke Practice Complete";
            Txt_ContinueText.text = "Continue To White Testing";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip White Practice";
            Debug.Log("White Practice Running");
            reviewphase = false;
            currentQuestionIndex = 0;
            currentQuestionValues = new int[questionvalues_practice_white.Length];

        }
        else if (currenttype == TestType.whiteTest)
        {
            btn_Scratch.gameObject.SetActive(true);
            Txt_currentCompleteTest.text = "White Smoke Test";
            Txt_ContinueText.text = "Continue To Black Practice";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip White Smoke Test";
            Debug.Log("White Test Running");
            reviewphase = false;
            currentQuestionIndex = 0;
            currentQuestionValues = new int[questionvalues_test_white.Length];
        }
        else if (currenttype == TestType.blackPractice)
        {
            btn_Scratch.gameObject.SetActive(false);
            Txt_currentCompleteTest.text = "Black Smoke Practice";
            Txt_ContinueText.text = "Continue To Black Testing";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip Black Practice";
            Debug.Log("Black Practice Running");
            reviewphase = false;
            currentQuestionIndex = 0;
            currentQuestionValues = new int[questionvalues_practice_black.Length];
        }
        else if (currenttype == TestType.blackTest)
        {
            
            btn_Scratch.gameObject.SetActive(true);
            Txt_currentCompleteTest.text = "Black Smoke Test";
            Txt_ContinueText.text = "Continue To Submission";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip Black Smoke Test";
            Debug.Log("Black Test Running");
            reviewphase = false;
            currentQuestionIndex = 0;
            currentQuestionValues = new int[questionvalues_test_black.Length];
        }

        DisableAnswers();
        LoadQuestions();
        LoadAnswerListeners();

        // video setup
        {
            loadingImage.SetActive(false);
            if (videoPlayer == null)
            {
                Debug.LogError("VideoPlayer not assigned!");
                return;
            }

            if (loadingImage == null)
            {
                Debug.LogError("LoadingImage not assigned!");
                return;
            }

            loadingImageRect = loadingImage.GetComponent<RectTransform>();
            loadingImage.SetActive(true);

            videoPlayer.started += OnVideoStarted;
            videoPlayer.loopPointReached += OnVideoEnded;
            videoPlayer.errorReceived += OnVideoError;

            // Add preload video player events
            if (preloadVideoPlayer != null)
            {
                preloadVideoPlayer.prepareCompleted += OnPreloadVideoPrepared;
            }
        }

        // Automatically load and play the first video
        //playVideoByIndex(0);

        //// NEW: Mark that first question is being loaded
        //isFirstQuestionLoaded = true;



        //new logic for question
        if (btn_questions != null && btn_questions.Length > 0)
        {
            // ensure currentQuestionIndex is 0
            currentQuestionIndex = 0;
            // invoke the question click so OnQuestion handles everything (SetSmokePercentage, PlayVideoWithPreload, etc.)
            btn_questions[currentQuestionIndex].onClick.Invoke();
        }
        isFirstQuestionLoaded = true;
    }

    // ========== VIDEO PRELOADING SYSTEM ==========

    /// <summary>
    /// Initialize the preload video player (create if doesn't exist)
    /// </summary>
    void InitializePreloadVideoPlayer()
    {
        if (!enablePreloading)
        {
            Debug.Log("Video preloading is disabled");
            return;
        }

        if (preloadVideoPlayer == null)
        {
            // Create a new GameObject for the preload video player
            GameObject preloadObj = new GameObject("PreloadVideoPlayer");
            preloadObj.transform.SetParent(transform);
            preloadVideoPlayer = preloadObj.AddComponent<VideoPlayer>();

            // Configure preload video player (hidden, no audio, just for buffering)
            preloadVideoPlayer.playOnAwake = false;
            preloadVideoPlayer.waitForFirstFrame = true;
            preloadVideoPlayer.skipOnDrop = true;
            preloadVideoPlayer.renderMode = VideoRenderMode.APIOnly; // Don't render, just buffer

            Debug.Log("Preload VideoPlayer created successfully");
        }
    }

    /// <summary>
    /// Preload the next video in the background for instant playback
    /// </summary>
    void PreloadNextVideo()
    {
        if (!enablePreloading || preloadVideoPlayer == null)
            return;

        // Don't preload in review or scratch mode
        if (reviewphase || scratchMode)
            return;

        // Check if there's a next question
        int nextQuestionIndex = currentQuestionIndex + 1;
        if (nextQuestionIndex >= btn_questions.Length || nextQuestionIndex >= currentQuestionValues.Length)
        {
            Debug.Log("No next question to preload");
            return;
        }

        // Get the next question's opacity value
        int nextOpacityValue = currentQuestionValues[nextQuestionIndex];
        int nextOpacityIndex = IndexOfOpacity(nextOpacityValue);

        if (nextOpacityIndex == -1)
        {
            Debug.LogWarning($"Invalid opacity index for preload: {nextOpacityValue}");
            return;
        }

        // Build the URL for the next video
        string nextURL = GetVideoURLByIndex(nextOpacityIndex);

        if (string.IsNullOrEmpty(nextURL))
        {
            Debug.LogWarning("Failed to get next video URL for preload");
            return;
        }

        // Store the next video info
        nextVideoURL = nextURL;
        nextVideoIndex = nextOpacityIndex;
        isNextVideoPrepared = false;

        // Stop any previous preparation
        if (preloadVideoPlayer.isPrepared)
        {
            preloadVideoPlayer.Stop();
        }

        // Set the URL and prepare (buffer) the video
        preloadVideoPlayer.url = nextURL;
        preloadVideoPlayer.Prepare();

        Debug.Log($"Preloading next video: Question {nextQuestionIndex + 1}, Opacity {nextOpacityValue}%, Index {nextOpacityIndex}");
    }


    // Get video URL by index without playing it

    string GetVideoURLByIndex(int index)
    {
        string filename = "";

        if (currenttype == TestType.whiteTest || currenttype == TestType.whitePractice)
        {
            if (index >= 0 && index < whiteVideoFiles.Length)
                filename = whiteVideoFiles[index].ToString();
        }
        else
        {
            if (index >= 0 && index < blackVideoFiles.Length)
                filename = blackVideoFiles[index].ToString();
        }

        if (string.IsNullOrEmpty(filename))
            return "";

        string path = Path.Combine(Application.streamingAssetsPath, filename);

#if UNITY_WEBGL
        return path;
#else
        return "file://" + path;
#endif
    }


    // Called when the preload video is ready
 
    void OnPreloadVideoPrepared(VideoPlayer source)
    {
        isNextVideoPrepared = true;
        Debug.Log("Next video preloaded and ready for instant playback!");
    }

   
    // Use preloaded video if available, otherwise load normally
    
    void PlayVideoWithPreload(int index)
    {
        // Check if the requested video is already preloaded
        if (enablePreloading && isNextVideoPrepared && nextVideoIndex == index && !string.IsNullOrEmpty(nextVideoURL))
        {
            Debug.Log("Using preloaded video for instant playback!");

            // Stop current video
            if (videoPlayer.isPlaying)
                videoPlayer.Stop();

            // Swap: the preloaded video becomes the main video
            videoPlayer.url = nextVideoURL;
            videoPlayer.isLooping = true;
            videoPlayer.Play();


            isNextVideoPrepared = false;
            nextVideoURL = "";
            nextVideoIndex = -1;

            PreloadNextVideo();
        }
        else
        {
            // Normal loading (fallback)
            playVideoByIndex(index);
        }
    }


    // Skip Practice/Test Button
public void OnSkipPractice()
{
    Debug.Log("Skip button pressed for " + currenttype);

    if (currenttype == TestType.whitePractice)
    {
        Debug.Log("Skipping White Practice to White Test");
            manageWhitePracticeTest.GoToWhiteTutorial();
        //SkipToTest(TestType.whiteTest);
    }
    else if (currenttype == TestType.whiteTest)
    {
        Debug.Log("Skipping White Test to Black Practice");
        SkipToTest(TestType.blackPractice);
    }
    else if (currenttype == TestType.blackPractice)
    {
        Debug.Log("Skipping Black Practice to Black Test");
            //SkipToTest(TestType.blackTest);
            mangerBlackPractice.GoToblackTutorial();
        }
    else if (currenttype == TestType.blackTest)
    {
        Debug.Log("Skipping Black Test to Signature Panel");
        
        
        currenttype = TestType.TestComplete;
        ShowingFinalResult();
        
        OpenSignaturePanel();
    }
}

    public void WhiteTestStart()
    {
        SkipToTest(TestType.whiteTest);
    }
    public void BlackPraticeStart()
    {
        SkipToTest(TestType.blackPractice);
    }
    public void BlackTestStart()
    {
        SkipToTest(TestType.blackTest);
    }
    // Skip to test phase
    private void SkipToTest(TestType testType)
    {
        reviewphase = false;
        currentQuestionIndex = 0;
        scratchMode = false;
        isFirstQuestionLoaded = false; // Reset for new phase

        foreach (TMP_Text tt in userSelectedValue)
        {
            tt.text = "";
        }

        TestingCompletePannel.SetActive(false);
        RemarksPannel.SetActive(false);

        currenttype = testType;

        if (testType == TestType.whiteTest)
        {
            currentSmokeType = "white";
            currentQuestionValues = questionvalues_test_white;
            btn_Scratch.gameObject.SetActive(true);
            Txt_currentCompleteTest.text = "White Smoke Test";
            Txt_ContinueText.text = "Continue To Black Practice";
            CurrentTest_txt.text = "White Smoke Testing";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip White Smoke Test";
            
        }
        else if (testType == TestType.blackPractice)
        {
            currentSmokeType = "black";
            currentQuestionValues = questionvalues_practice_black;
            btn_Scratch.gameObject.SetActive(false);
            Txt_currentCompleteTest.text = "Black Smoke Practice";
            Txt_ContinueText.text = "Continue To Black Testing";
            CurrentTest_txt.text = "Black Smoke Practice";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip Black Practice";
        }
        else if (testType == TestType.blackTest)
        {
            currentSmokeType = "black";
            currentQuestionValues = questionvalues_test_black;
            btn_Scratch.gameObject.SetActive(true);
            Txt_currentCompleteTest.text = "Black Smoke Test";
            Txt_ContinueText.text = "Continue To Submission";
            CurrentTest_txt.text = "Black Smoke Testing";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip Black Smoke Test";
        }

        LoadCurrentQuestion();
        DisableAnswers();

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        // Reset preload state when skipping
        isNextVideoPrepared = false;
        nextVideoURL = "";
        nextVideoIndex = -1;

        //// Automatically load and play the first video of the new test phase
        //playVideoByIndex(0);
        //isFirstQuestionLoaded = true; // Mark first question as being loaded

        // Trigger the first question so OnQuestion handles playback and UI properly
        currentQuestionIndex = 0;
        LoadCurrentQuestion(); // make sure only question 0 is interactable
        if (btn_questions != null && btn_questions.Length > 0)
        {
            btn_questions[currentQuestionIndex].onClick.Invoke();
        }
        isFirstQuestionLoaded = true;

    }

    // Open Signature Panel
    public void OpenSignaturePanel()
    {
        Debug.Log("Opening Signature Panel");

        // Hide all test panels
        TestingCompletePannel.SetActive(false);
        RemarksPannel.SetActive(false);
        tutorialsPannel.SetActive(false);

        // Stop video if playing
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        // Hide skip button since we're done with tests
        btn_SkipPracticeTest.gameObject.SetActive(false);

        // Show signature panel
        SignaturePannel.SetActive(true);
        Btn_Clear.gameObject.SetActive(true);
        Btn_Submission.gameObject.SetActive(true);

        currenttype = TestType.TestComplete;
    }

    private void Update()
    {
        bool isVideoPlaying = videoPlayer.isPlaying;
        bool isBlackScreenOff = !blackScreen.activeSelf;

        if (!isVideoPlaying && isBlackScreenOff)
        {
            loadingImage.SetActive(true);
            RotateLoadingImage();
        }
        else
        {
            loadingImage.SetActive(false);
        }

        // Update skip button text based on current phase
        if (currenttype == TestType.whitePractice)
        {
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip to White Smoke Test";
        }
        else if (currenttype == TestType.whiteTest)
        {
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip to Black Smoke Practice";
        }
        else if (currenttype == TestType.blackPractice)
        {
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip to Black Smoke Test";
        }
        else if (currenttype == TestType.blackTest)
        {
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip to Signature";
        }
        else if (currenttype == TestType.TestComplete)
        {
            btn_SkipPracticeTest.gameObject.SetActive(false);
        }
    }

    void LoadGroup(int percentage, string type)
    {
        var group = videoData.smokeVideos.FirstOrDefault(g => g.percentage == percentage);
        if (group == null)
        {
            Debug.LogWarning("No smoke group found for " + percentage + "%");
            return;
        }

        currentTypeGroup = group.types.FirstOrDefault(t => t.typeName.ToLower() == type.ToLower());

        if (currentTypeGroup == null || currentTypeGroup.videoFileNames.Count == 0)
        {
            Debug.LogWarning("No videos found for type: " + type + " at " + percentage + "%");
            return;
        }

        currentVideoIndex = Random.Range(0, currentTypeGroup.videoFileNames.Count);
    }

    void PlayCurrentVideo()
    {
        if (currentTypeGroup == null || currentTypeGroup.videoFileNames.Count == 0)
            return;

        string fileName = currentTypeGroup.videoFileNames[currentVideoIndex];
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        videoPlayer.url = path;
        videoPlayer.Play();
    }

    public void RefreshVideo()
    {
        chkgrptype();
        currentVideoIndex = (currentVideoIndex + 1) % currentTypeGroup.videoFileNames.Count;
        PlayCurrentVideo();
    }

    public void SetSmokePercentage(int newPercentage, string typename)
    {
        currentSmokePercentage = newPercentage;
        LoadGroup(currentSmokePercentage, currentSmokeType);
    }

    public void ContinueToNextPhase()
    {
        reviewphase = false;
        currentQuestionIndex = 0;
        isFirstQuestionLoaded = false; // Reset for new phase
        LoadCurrentQuestion();
        TestingCompletePannel.SetActive(false);
        RemarksPannel.SetActive(false);

        // Hide Next button when starting new phase
        if (btn_Next != null)
        {
            btn_Next.gameObject.SetActive(false);
        }
        answerSelected = false;

        foreach (TMP_Text tt in userSelectedValue)
        {
            tt.text = "";
        }

        if (currenttype == TestType.whitePractice)
        {
            currentSmokeType = "White";
            currentQuestionValues = questionvalues_test_white;
            currenttype = TestType.whiteTest;
            btn_Scratch.gameObject.SetActive(true);
            btn_SkipPracticeTest.gameObject.SetActive(true);
        }
        else if (currenttype == TestType.whiteTest)
        {
            currentSmokeType = "White";
            currentQuestionValues = questionvalues_practice_black;
            currenttype = TestType.blackPractice;
            btn_Scratch.gameObject.SetActive(false);
            btn_SkipPracticeTest.gameObject.SetActive(true);
        }
        else if (currenttype == TestType.blackPractice)
        {
            currentSmokeType = "Black";
            btn_Scratch.gameObject.SetActive(true);
            btn_SkipPracticeTest.gameObject.SetActive(true);
            currentQuestionValues = questionvalues_test_black;
            currenttype = TestType.blackTest;
        }
        else if (currenttype == TestType.blackTest)
        {
            currentSmokeType = "Black";
            btn_Scratch.gameObject.SetActive(false);
            btn_SkipPracticeTest.gameObject.SetActive(false);
            Debug.Log("OPEN RESULT PANNEL");
        }
        else if (currenttype == TestType.TestComplete)
        {
            OpenSignaturePanel();
        }

        // Reset preload state when continuing to next phase
        isNextVideoPrepared = false;
        nextVideoURL = "";
        nextVideoIndex = -1;

        // Auto-play first video of new phase
        //playVideoByIndex(0);
        //isFirstQuestionLoaded = true;

        // Auto-trigger first question of new phase so OnQuestion runs (safer than force-playing index 0)
        currentQuestionIndex = 0;
        LoadCurrentQuestion();
        if (btn_questions != null && btn_questions.Length > 0)
        {
            btn_questions[currentQuestionIndex].onClick.Invoke();
        }
        isFirstQuestionLoaded = true;

    }

    void LoadAnswerListeners()
    {
        for (int i = 0; i < btn_points.Length; i++)
        {
            int index = i;
            btn_points[i].onClick.AddListener(new UnityAction(() => OnAnswer(index)));
        }
    }

    void LoadQuestions()
    {
        Debug.Log("Loading questions");

        for (int z = 0; z < 4; z++)
        {
            int[] currentArray = null;

            if (z == 0) currentArray = questionvalues_practice_white;
            else if (z == 1) currentArray = questionvalues_test_white;
            else if (z == 2) currentArray = questionvalues_practice_black;
            else if (z == 3) currentArray = questionvalues_test_black;

            List<int> availableValues = new List<int>(answersValue);
            List<int> usedValues = new List<int>();

            for (int i = 0; i < currentArray.Length; i++)
            {
                List<int> validValues = new List<int>();

                if (i == 0)
                {
                    validValues.AddRange(availableValues);
                }
                else
                {
                    int previousValue = currentArray[i - 1];

                    foreach (int value in availableValues)
                    {
                        int difference = Mathf.Abs(value - previousValue);
                        if (difference > 5)
                        {
                            validValues.Add(value);
                        }
                    }

                    if (validValues.Count == 0)
                    {
                        Debug.Log($"No available unique values for position {i}, checking used values");
                        foreach (int value in usedValues)
                        {
                            int difference = Mathf.Abs(value - previousValue);
                            if (difference > 5)
                            {
                                validValues.Add(value);
                            }
                        }
                    }

                    if (validValues.Count == 0)
                    {
                        Debug.LogWarning($"No valid values found for position {i}, using all values as fallback");
                        validValues.AddRange(answersValue);
                    }
                }

                int randomValidIndex = Random.Range(0, validValues.Count);
                int selectedValue = validValues[randomValidIndex];
                currentArray[i] = selectedValue;

                if (availableValues.Contains(selectedValue))
                {
                    availableValues.Remove(selectedValue);
                    usedValues.Add(selectedValue);
                    Debug.Log($"Value {selectedValue} moved from available to used. Remaining available: {availableValues.Count}");
                }

                Debug.Log($"Value {i} for array {z}: {selectedValue} " +
                         (i > 0 ? $"(previous: {currentArray[i - 1]}, difference: {Mathf.Abs(selectedValue - currentArray[i - 1])})" : "(first value)"));
            }

            Debug.Log($"Array {z} completed. Used {usedValues.Count} unique values out of {answersValue.Length} total values.");
        }

        if (currenttype == TestType.whitePractice)
        {
            currentQuestionValues = questionvalues_practice_white;
        }
        else if (currenttype == TestType.whiteTest)
        {
            currentQuestionValues = questionvalues_test_white;
        }
        else if (currenttype == TestType.blackPractice)
        {
            currentQuestionValues = questionvalues_practice_black;
        }
        else if (currenttype == TestType.blackTest)
        {
            currentQuestionValues = questionvalues_test_black;
        }

        for (int i = 0; i < btn_questions.Length; i++)
        {
            int index = i;
            btn_questions[i].onClick.AddListener(new UnityAction(() => OnQuestion(index)));
            Debug.Log("Button assign");
            btn_questions[i].interactable = false;
        }

        LoadCurrentQuestion();
    }

    void OnQuestion(int i)
    {
        blackScreen.SetActive(false);
        if (scratchMode)
        {
            Debug.Log(" In scratch Phase");
            SCRATCHQUESTIONINDEX = i;
            int jvalue = currentQuestionValues[i];
            int opacityIndex = IndexOfOpacity(jvalue);
            Debug.Log("opacity index is " + opacityIndex);
            playVideoByIndex(opacityIndex);
            RemarksPannel.gameObject.SetActive(false);
            EnableAnswers();
        }
        if (reviewphase)
        {
            Debug.Log(" In Reviewphase");
            TestingCompletePannel.SetActive(false);
            

            REVIEWQUESTIONINDEX = i;
            Debug.Log("Question clicked " + i);
            int jvalue = currentQuestionValues[i];
            int opacityIndex = IndexOfOpacity(jvalue);
            Debug.Log("opacity index is " + opacityIndex);
            playVideoByIndex(opacityIndex);
            EnableAnswers();
            RemarksPannel.SetActive(false);
            //DisableAnswers();
            btn_Scratch.gameObject.SetActive(false);
        }
        else if (currentQuestionIndex == i)
        {
            TestingCompletePannel.SetActive(false);
            RemarksPannel.SetActive(false);
            Debug.Log("Question clicked " + i);
            Debug.Log("Question Opacity Value  " + currentQuestionValues[i]);
            int jvalue = currentQuestionValues[i];
            questionNmbr_text.text = ("Question: " + (currentQuestionIndex + 1));

            currentSmokePercentage = currentQuestionValues[i];

            SetSmokePercentage(currentSmokePercentage, currentSmokeType);

            int opacityIndex = IndexOfOpacity(jvalue);

            Debug.Log("opacity index is " + opacityIndex);

            // Use preloaded video if available
            PlayVideoWithPreload(opacityIndex);

            EnableAnswers();
        }
        else
        {
            Debug.Log("invalid question" + currentQuestionIndex);
        }

        if (currenttype == TestType.whitePractice) { CurrentTest_txt.text = "White Smoke Practice"; }
        if (currenttype == TestType.whiteTest) { CurrentTest_txt.text = "White Smoke Testing"; }
        if (currenttype == TestType.blackPractice) { CurrentTest_txt.text = "Black Smoke Practice"; }
        if (currenttype == TestType.blackTest) { CurrentTest_txt.text = "Black Smoke Testing"; }
    }

    // MODIFIED: Auto-advance coroutine with preloading
    private IEnumerator AutoAdvanceToNextQuestion()
    {
        isAutoAdvancing = true;

        // Wait for the specified delay
        yield return new WaitForSeconds(autoAdvanceDelay);

        // Load and click the next question
        if (currenttype == TestType.whiteTest || currenttype == TestType.blackTest ||
            currenttype == TestType.whitePractice || currenttype == TestType.blackPractice)
        {
            blackScreen.SetActive(true);
        }
        LoadCurrentQuestion();
        btn_Scratch.gameObject.SetActive(true);

        // Automatically trigger the next question (will use preloaded video)
        btn_questions[currentQuestionIndex].onClick.Invoke();

        isAutoAdvancing = false;
    }

    // NEW: Handle Next button click
    void OnNextButtonClicked()
    {
        if (!answerSelected)
        {
            Debug.LogWarning("Please select an answer before clicking Next!");
            return;
        }

        // Hide Next button
        if (btn_Next != null)
        {
            btn_Next.gameObject.SetActive(false);
        }

        answerSelected = false;

        // Advance to next question
        if (currenttype == TestType.whiteTest || currenttype == TestType.blackTest ||
            currenttype == TestType.whitePractice || currenttype == TestType.blackPractice)
        {
            blackScreen.SetActive(true);
        }
        LoadCurrentQuestion();
        btn_Scratch.gameObject.SetActive(true);

        // Trigger the next question (will use preloaded video)
        btn_questions[currentQuestionIndex].onClick.Invoke();
    }
    void LockPreviousQuestions(int index)
    {
        for (int q = 0; q < btn_questions.Length; q++)
        {
            if (q == index)
                btn_questions[q].interactable = true; // only current question
            else
                btn_questions[q].interactable = false;
        }
    }

    // Replace the OnAnswer method in your ManagerTesting.cs with this modified version:

    void OnAnswer(int i)
    {
        Debug.Log("ANSWER SELECTED IS " + answersValue[i]);

        if (scratchMode)
        {
            userSelectedValue[SCRATCHQUESTIONINDEX].text = "" + answersValue[i];

            if (currenttype == TestType.whitePractice) { answervalues_practice_white[SCRATCHQUESTIONINDEX] = answersValue[i]; }
            if (currenttype == TestType.whiteTest) { answervalues_test_white[SCRATCHQUESTIONINDEX] = answersValue[i]; }
            if (currenttype == TestType.blackPractice) { answervalues_practice_black[SCRATCHQUESTIONINDEX] = answersValue[i]; }
            if (currenttype == TestType.blackTest) { answervalues_test_black[SCRATCHQUESTIONINDEX] = answersValue[i]; }

            DisableAnswers();
            videoPlayer.Stop();

            scratchMode = false;
            SCRATCHQUESTIONINDEX = -1;

            if (lastQuestionBeforeScratch >= 0)
            {
                currentQuestionIndex = lastQuestionBeforeScratch;
                lastQuestionBeforeScratch = -1;
                LockPreviousQuestions(currentQuestionIndex);
                btn_questions[currentQuestionIndex].onClick.Invoke();
            }
            return;
        }

        if (reviewphase)
        {
            userSelectedValue[REVIEWQUESTIONINDEX].text = "" + answersValue[i];

            if (currenttype == TestType.whitePractice) { answervalues_practice_white[REVIEWQUESTIONINDEX] = answersValue[i]; }
            if (currenttype == TestType.whiteTest) { answervalues_test_white[REVIEWQUESTIONINDEX] = answersValue[i]; }
            if (currenttype == TestType.blackPractice) { answervalues_practice_black[REVIEWQUESTIONINDEX] = answersValue[i]; }
            if (currenttype == TestType.blackTest) { answervalues_test_black[REVIEWQUESTIONINDEX] = answersValue[i]; }

            int selected = answersValue[i];
            int actual = currentQuestionValues[REVIEWQUESTIONINDEX];
            int diff = Mathf.Abs(selected - actual);
            int score = Mathf.Abs(diff / 5);

            if (currenttype == TestType.whiteTest)
            {
                YourWhiteSelectedValue[REVIEWQUESTIONINDEX].text = selected.ToString();
                WhiteOpacityActualValue[REVIEWQUESTIONINDEX].text = actual.ToString();
                whiteSmokeScore[REVIEWQUESTIONINDEX].text = score.ToString();
            }
            else if (currenttype == TestType.TestComplete)
            {
                YourBlackSelectedValue[REVIEWQUESTIONINDEX].text = selected.ToString();
                BlackOpacityActualValue[REVIEWQUESTIONINDEX].text = actual.ToString();
                BlackSmokeScore[REVIEWQUESTIONINDEX].text = score.ToString();
            }

            DisableAnswers();
            videoPlayer.Stop();
            ReOpenTestCompletePannel();
            btn_Scratch.gameObject.SetActive(false);
            return;
        }
        else
        {
            userSelectedValue[currentQuestionIndex].text = "" + answersValue[i];

            int selected = answersValue[i];
            int actual = currentQuestionValues[currentQuestionIndex];
            int diff = Mathf.Abs(selected - actual);
            int score = Mathf.Abs(diff / 5);

            if (currenttype == TestType.whiteTest)
            {
                whiteTestScore += score;
                YourWhiteSelectedValue[currentQuestionIndex].text = selected.ToString();
                WhiteOpacityActualValue[currentQuestionIndex].text = actual.ToString();
                whiteSmokeScore[currentQuestionIndex].text = score.ToString();
            }
            else if (currenttype == TestType.blackTest)
            {
                blackTestScore += score;
                YourBlackSelectedValue[currentQuestionIndex].text = selected.ToString();
                BlackOpacityActualValue[currentQuestionIndex].text = actual.ToString();
                BlackSmokeScore[currentQuestionIndex].text = score.ToString();
            }

            if (currenttype == TestType.whitePractice) { answervalues_practice_white[currentQuestionIndex] = answersValue[i]; }
            if (currenttype == TestType.whiteTest) { answervalues_test_white[currentQuestionIndex] = answersValue[i]; }
            if (currenttype == TestType.blackPractice) { answervalues_practice_black[currentQuestionIndex] = answersValue[i]; }
            if (currenttype == TestType.blackTest) { answervalues_test_black[currentQuestionIndex] = answersValue[i]; }

            DisableAnswers();
            videoPlayer.Stop();

            bool isPractice = (currenttype == TestType.whitePractice || currenttype == TestType.blackPractice);
            bool isTest = (currenttype == TestType.whiteTest || currenttype == TestType.blackTest);
            bool isLastQuestion = (currentQuestionIndex == btn_questions.Length - 1);

            if (!isLastQuestion)
            {
                currentQuestionIndex++;
                answerSelected = true;

                if (isPractice)
                {
                    OpenRemarksPannel();
                    btn_Scratch.gameObject.SetActive(false);
                    btn_SkipPracticeTest.gameObject.SetActive(true);

                    if (btn_Next != null)
                        btn_Next.gameObject.SetActive(true);
                    else
                        StartCoroutine(AutoAdvanceToNextQuestion());
                }
                else if (isTest)
                {
                    btn_Scratch.gameObject.SetActive(true);
                    if (!isAutoAdvancing)
                        StartCoroutine(AutoAdvanceToNextQuestion());
                }
            }
            else
            {
                btn_Scratch.gameObject.SetActive(false);

                if (isPractice)
                {
                    // ✅ LAST QUESTION IN PRACTICE: Show remarks immediately
                    OpenRemarksPannel();
                    TestingCompletePannel.SetActive(false);
                    btn_Next.gameObject.SetActive(false);
                    btn_Scratch.gameObject.SetActive(false);
                    // Show TestingComplete after 3 seconds
                    StartCoroutine(ShowTestCompleteAfterDelay(3f));
                }
                else
                {
                    ShowTestCompletePanel();
                }
            }
        }
    }

    // Coroutine to show Test Complete panel after a delay
    private IEnumerator ShowTestCompleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowTestCompletePanel();
    }

    // Extracted function to handle TestComplete panel logic
    private void ShowTestCompletePanel()
    {
        TestingCompletePannel.SetActive(true);

        if (currenttype == TestType.whitePractice)
        {
          WhiteTestButton.SetActive(true);


            Txt_ContinueText.text = "Continue To White Testing";
            Txt_currentCompleteTest.text = "White Smoke Practice Complete";
            Debug.Log("White pratice complete");
        }
        else if (currenttype == TestType.whiteTest)
        {
            Txt_ContinueText.text = "Continue To Black Practice";
            Txt_currentCompleteTest.text = "White Smoke Testing Complete";
            openresultPannelButton.gameObject.SetActive(true);
            BlackPracticeButton.SetActive(true);
        }
        else if (currenttype == TestType.blackPractice)
        {
            Txt_ContinueText.text = "Continue To Black Testing";
            Txt_currentCompleteTest.text = "Black Smoke Practice Complete";
            BlackTestButton.SetActive(true);
            Btn_Submission.gameObject.SetActive(false);
            openresultPannelButton.gameObject.SetActive(false);
            Debug.Log("Black pratice complete");
        }
        else if (currenttype == TestType.blackTest)
        {
            SubmissionButton.SetActive(true);
            Btn_Submission.gameObject.SetActive(false);
            openresultPannelButton.gameObject.SetActive(false) ;
            //Txt_ContinueText.text = "Continue To Submission";
            Txt_currentCompleteTest.text = "Black Smoke Testing Complete";
            currenttype = TestType.TestComplete;
           // ShowingFinalResult();
        }

        reviewphase = true;
        scratchMode = false;
        foreach (Button X in btn_questions)
            X.interactable = true;
    }



    void ReOpenTestCompletePannel()
    {
        TestingCompletePannel.SetActive(true);
    }

    public void OpenRemarksPannel()
    {
        DisableAnswers();
        int ogvalue = 0;
        int ansvalue = 0;
        RemarksPannel.SetActive(true);
        if (reviewphase)
        {
            ogvalue = currentQuestionValues[REVIEWQUESTIONINDEX];
            ansvalue = int.Parse(userSelectedValue[REVIEWQUESTIONINDEX].text);
        }
        else
        {
            ogvalue = currentQuestionValues[(currentQuestionIndex) - 1];
            ansvalue = int.Parse(userSelectedValue[(currentQuestionIndex) - 1].text);
        }

        targetOpacityText.text = "" + ogvalue;
        yourReadingText.text = "" + ansvalue;
        string remarks = "";

        if (ansvalue == ogvalue)
        {
            remarks = "Your Value was Perfect";
        }
        else if (ansvalue > ogvalue)
        {
            int x = ansvalue - ogvalue;
            remarks = "Your Value was " + x + "% too high";
        }
        else if (ansvalue < ogvalue)
        {
            int x = ogvalue - ansvalue;
            remarks = "Your Value was " + x + "% too low";
        }
        resultSummaryText.text = "" + remarks;
    }

    private void LoadCurrentQuestion()
    {
        for (int i = 0; i < btn_questions.Length; i++)
        {
            btn_questions[i].interactable = false;
        }
        btn_questions[currentQuestionIndex].interactable = true;
    }

    // MODIFIED: playVideoByIndex now triggers preloading after video starts
    void playVideoByIndex(int index)
    {
        if (currenttype == TestType.whiteTest || currenttype == TestType.whitePractice)
        {
            string filename = whiteVideoFiles[index].ToString();
            string path = Path.Combine(Application.streamingAssetsPath, filename);
#if UNITY_WEBGL
            string url = path;
#else
            string url = "file://" + path;
#endif

            if (videoPlayer.isPlaying)
                videoPlayer.Stop();

            videoPlayer.url = "";
            videoPlayer.url = url;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }
        else
        {
            string filename = blackVideoFiles[index].ToString();
            string path = Path.Combine(Application.streamingAssetsPath, filename);
#if UNITY_WEBGL
            string url = path;
#else
            string url = "file://" + path;
#endif

            if (videoPlayer.isPlaying)
                videoPlayer.Stop();

            videoPlayer.url = "";
            videoPlayer.url = url;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        // After starting current video, preload the next one in background
        PreloadNextVideo();
    }

    public void DisableAnswers()
    {
        Debug.Log("check enable");
        foreach (Button y in btn_points)
        {
            y.interactable = false;
        }
        Refresh.gameObject.SetActive(false);
        btn_Scratch.gameObject.SetActive(true);

        // Hide Next button when answers are disabled
        if (btn_Next != null)
        {
            btn_Next.gameObject.SetActive(false);
        }
    }

    public void EnableAnswers()
    {
        foreach (Button y in btn_points)
        {
            y.interactable = true;
        }
        Refresh.gameObject.SetActive(true);
        btn_Scratch.gameObject.SetActive(true);
    }

    public int IndexOfOpacity(int x)
    {
        int index = Array.IndexOf(answersValue, x);
        if (index == -1)
        {
            Debug.LogWarning($"Value {x} not found in answersValue array");
            return -1;
        }
        return index;
    }

    public void OnEnabledscratchmode()
    {
        reviewphase = false;
        scratchMode = true;

        // Save current question before scratch mode
        lastQuestionBeforeScratch = currentQuestionIndex;

        SCRATCHQUESTIONINDEX = -1;
        Debug.Log("Scratch Mode Enabled");

        if (btn_Next != null)
            btn_Next.gameObject.SetActive(false);

        answerSelected = false;

        for (int i = 0; i < currentQuestionIndex; i++)
        {
            Button btn = btn_questions[i];
            btn.interactable = true;
        }
        DisableAnswers();
    }


    // Updated score calculation logic

    public void ShowingFinalResult()
    {
        // Check if user answered any question
        bool answeredAny = DidUserAnswerAnyQuestion();

        if (!answeredAny)
        {
            // User didn’t answer any question
            NotPassedPanel.SetActive(true);
            QualifiedPanel.SetActive(false);

            YourTotalScore.text = "null"; // display null
            endTestButtonText.text = "Retake Test";
            Debug.Log("User failed because no answers were selected");
            return;
        }

        // Calculate total score
        int totalScore = whiteTestScore + blackTestScore;
        YourTotalScore.text = totalScore.ToString();

        // Simple pass/fail logic
        bool failed = totalScore >37; // Make sure condition is correct

        if (failed)
        {
            NotPassedPanel.SetActive(true);
            QualifiedPanel.SetActive(false);
            endTestButtonText.text = "Retake Test";
            Debug.Log($"FAILED - Total Score: {totalScore} (White: {whiteTestScore}, Black: {blackTestScore})");
        }
        else
        {
            QualifiedPanel.SetActive(true);
            NotPassedPanel.SetActive(false);
            endTestButtonText.text = "End Test";
            Debug.Log($"PASSED - Total Score: {totalScore} (White: {whiteTestScore}, Black: {blackTestScore})");
        }
    }



    private bool DidUserAnswerAnyQuestion()
    {
        // Check white test answers
        foreach (var ans in answervalues_test_white)
        {
            if (ans != 0) return true; // at least one answered
        }

        // Check black test answers
        foreach (var ans in answervalues_test_black)
        {
            if (ans != 0) return true; // at least one answered
        }

        return false; // no answers
    }


    private void RotateLoadingImage()
    {
        if (loadingImageRect != null)
        {
            loadingImageRect.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }

    // MODIFIED: Enable answers for first question when video starts
    public void OnVideoStarted(VideoPlayer vp)
    {
        loadingImage.SetActive(false);

        // NEW: Enable answers for the first question when its video starts playing
        if (isFirstQuestionLoaded && currentQuestionIndex == 0 && !reviewphase && !scratchMode)
        {
            Debug.Log("First question video started - enabling answers");
            EnableAnswers();
            isFirstQuestionLoaded = false; // Reset flag so it only happens once
        }
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        loadingImage.SetActive(true);
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("VideoPlayer error: " + message);
        loadingImage.SetActive(true);
    }

    public void OnEndTestButtonClicked()
    {
        // Reset scores for retake
        whiteTestScore = 0;
        blackTestScore = 0;

        DataInput_Fields.checkSceneReload = 1;

        if (whiteTestScore + blackTestScore >= 37)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Debug.Log("Home Screen Open!");
        }
    }
}