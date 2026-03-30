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
using System.Text.RegularExpressions;

public class ManagerTesting : MonoBehaviour
{
    public static int testRunNumber = 1;

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
    private int nextPreparedQuestionIndex = -1;
    private int nextPreparedOpacity = -1;
    private string nextPreparedSmokeType = "";

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
    bool scratchModeStartedFromReview = false;


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
    public SmokeVideoURLData videoURLData;
    private SmokeVideoURLData.SmokeTypeGroup currentTypeGroup;
    private int[] questionVideoIndices;
    private string[] questionVideoUrls;

    // MODIFIED: Separate scores for white and black smoke tests
    int whiteTestScore = 0;
    int blackTestScore = 0;
    const int CertificationScoreThreshold = 37;
    const int IndividualFailureThreshold = 3;

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

    private bool IsPracticeMode()
    {
        return currenttype == TestType.whitePractice || currenttype == TestType.blackPractice;
    }

    private bool IsCertificationTestMode()
    {
        return currenttype == TestType.whiteTest ||
               currenttype == TestType.blackTest ||
               currenttype == TestType.TestComplete;
    }

    private int GetDisplayedQuestionIndex()
    {
        if (scratchMode && SCRATCHQUESTIONINDEX >= 0)
        {
            return SCRATCHQUESTIONINDEX;
        }

        if (reviewphase && REVIEWQUESTIONINDEX >= 0)
        {
            return REVIEWQUESTIONINDEX;
        }

        return currentQuestionIndex;
    }

    private void UpdateQuestionNumberLabel()
    {
        if (questionNmbr_text == null)
        {
            return;
        }

        int displayedQuestionIndex = Mathf.Max(0, GetDisplayedQuestionIndex());
        questionNmbr_text.text = "Question: " + (displayedQuestionIndex + 1);
    }

    private void ApplyScratchAndRefreshButtonState()
    {
        bool allowRedoControls = (IsPracticeMode() || IsCertificationTestMode()) &&
                                 (SignaturePannel == null || !SignaturePannel.activeSelf) &&
                                 (TestingCompletePannel == null || !TestingCompletePannel.activeSelf);
        bool showScratchButton = allowRedoControls;

        if (btn_Scratch != null)
        {
            btn_Scratch.interactable = showScratchButton;
            btn_Scratch.gameObject.SetActive(showScratchButton);
        }

        if (Refresh != null)
        {
            Refresh.interactable = allowRedoControls;
            Refresh.gameObject.SetActive(allowRedoControls);
        }
    }

    private bool IsShowingPracticeRemarksForPreviousQuestion()
    {
        return !scratchMode &&
               !reviewphase &&
               IsPracticeMode() &&
               answerSelected &&
               RemarksPannel != null &&
               RemarksPannel.activeSelf &&
               currentQuestionIndex > 0;
    }

    private int GetRedoTargetQuestionIndex()
    {
        if (scratchMode)
        {
            return SCRATCHQUESTIONINDEX;
        }

        if (reviewphase)
        {
            return REVIEWQUESTIONINDEX;
        }

        if (IsShowingPracticeRemarksForPreviousQuestion())
        {
            return currentQuestionIndex - 1;
        }

        return currentQuestionIndex;
    }

    void Awake()
    {
        if (blackScreen != null)
        {
            blackScreen.SetActive(false);
        }

        if (loadingImage != null)
        {
            loadingImage.SetActive(false);
        }
    }

    void Start()
    {
        SmokeSchoolAppState.ResetCertificationState();
        Debug.Log($"ManagerTesting Start - current test run #{testRunNumber}");
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
        InitializeAnswerArrays();

        // Setup preload video player
        InitializePreloadVideoPlayer();

        // Keep skip button visible at all times
        btn_SkipPracticeTest.gameObject.SetActive(true);

        // Handle test type logic
        if (currenttype == TestType.whitePractice)
        {
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
        ResetQuestionVideoState();
        LoadAnswerListeners();
        ApplyScratchAndRefreshButtonState();

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



        StartCurrentPhaseAtFirstQuestion();
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

    private void ResetQuestionVideoState()
    {
        int questionCount = btn_questions != null ? btn_questions.Length : 0;
        questionVideoIndices = new int[questionCount];
        questionVideoUrls = new string[questionCount];

        for (int i = 0; i < questionCount; i++)
        {
            questionVideoIndices[i] = -1;
            questionVideoUrls[i] = string.Empty;
        }

        ClearPreparedVideoState();
    }

    private void ClearPreparedVideoState()
    {
        isNextVideoPrepared = false;
        nextVideoURL = string.Empty;
        nextVideoIndex = -1;
        nextPreparedQuestionIndex = -1;
        nextPreparedOpacity = -1;
        nextPreparedSmokeType = string.Empty;
    }

    private void StartCurrentPhaseAtFirstQuestion()
    {
        if (btn_questions == null || btn_questions.Length == 0)
        {
            return;
        }

        reviewphase = false;
        scratchMode = false;
        REVIEWQUESTIONINDEX = -1;
        SCRATCHQUESTIONINDEX = -1;
        scratchModeStartedFromReview = false;
        currentQuestionIndex = 0;
        answerSelected = false;
        isFirstQuestionLoaded = true;

        if (blackScreen != null)
        {
            blackScreen.SetActive(false);
        }

        LoadCurrentQuestion();
        UpdateQuestionNumberLabel();
        LoadQuestionVideo(currentQuestionIndex, false);
    }

    private int GetActualOpacityForQuestion(int questionIndex)
    {
        if (currentQuestionValues == null || questionIndex < 0 || questionIndex >= currentQuestionValues.Length)
        {
            return -1;
        }

        int definedOpacity = currentQuestionValues[questionIndex];
        string assignedUrl = questionVideoUrls != null && questionIndex < questionVideoUrls.Length
            ? questionVideoUrls[questionIndex]
            : string.Empty;

        if (TryParseOpacityFromVideoUrl(assignedUrl, out string parsedSmokeType, out int parsedOpacity) &&
            parsedOpacity != definedOpacity)
        {
            Debug.LogError(
                $"Question {questionIndex + 1} expected {definedOpacity}% {currentSmokeType} smoke but assigned video '{ExtractVideoFilename(assignedUrl)}' encodes {parsedSmokeType}{parsedOpacity}.");
        }

        return definedOpacity;
    }

    private bool TryParseOpacityFromVideoUrl(string url, out string smokeType, out int opacity)
    {
        smokeType = string.Empty;
        opacity = -1;

        string filename = ExtractVideoFilename(url);
        if (string.IsNullOrEmpty(filename))
        {
            return false;
        }

        Match match = Regex.Match(filename, @"(?i)(White|Black)(\d{2,3})");
        if (!match.Success)
        {
            return false;
        }

        smokeType = match.Groups[1].Value;
        return int.TryParse(match.Groups[2].Value, out opacity);
    }

    private int GetStoredVideoIndex(int questionIndex)
    {
        if (questionVideoIndices == null || questionIndex < 0 || questionIndex >= questionVideoIndices.Length)
        {
            return -1;
        }

        return questionVideoIndices[questionIndex];
    }

    private int SelectVideoIndexForQuestion(int questionIndex, bool forceNewVariation)
    {
        if (currentTypeGroup == null || currentTypeGroup.videoURLs == null || currentTypeGroup.videoURLs.Count == 0)
        {
            return -1;
        }

        int existingIndex = GetStoredVideoIndex(questionIndex);
        int videoCount = currentTypeGroup.videoURLs.Count;

        if (!forceNewVariation && existingIndex >= 0 && existingIndex < videoCount)
        {
            return existingIndex;
        }

        if (videoCount == 1)
        {
            return 0;
        }

        int selectedIndex;
        do
        {
            selectedIndex = Random.Range(0, videoCount);
        }
        while (selectedIndex == existingIndex);

        return selectedIndex;
    }

    private bool TryUsePreparedVideo(int questionIndex, int opacity)
    {
        if (!enablePreloading || !isNextVideoPrepared || string.IsNullOrEmpty(nextVideoURL))
        {
            return false;
        }

        if (nextPreparedQuestionIndex != questionIndex || nextPreparedOpacity != opacity)
        {
            return false;
        }

        if (!string.Equals(nextPreparedSmokeType, currentSmokeType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        currentVideoIndex = nextVideoIndex;
        questionVideoIndices[questionIndex] = currentVideoIndex;
        questionVideoUrls[questionIndex] = nextVideoURL;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        videoPlayer.url = nextVideoURL;
        videoPlayer.isLooping = true;
        videoPlayer.Play();

        ClearPreparedVideoState();
        PreloadNextVideo();
        return true;
    }

    private bool LoadQuestionVideo(int questionIndex, bool forceNewVariation)
    {
        int actualOpacity = GetActualOpacityForQuestion(questionIndex);
        if (actualOpacity < 0)
        {
            return false;
        }

        currentSmokePercentage = actualOpacity;
        LoadGroup(actualOpacity, currentSmokeType);

        if (currentTypeGroup == null || currentTypeGroup.videoURLs == null || currentTypeGroup.videoURLs.Count == 0)
        {
            Debug.LogWarning($"No videos found for {currentSmokeType} smoke at {actualOpacity}% for question {questionIndex + 1}.");
            return false;
        }

        if (!forceNewVariation && TryUsePreparedVideo(questionIndex, actualOpacity))
        {
            return true;
        }

        currentVideoIndex = SelectVideoIndexForQuestion(questionIndex, forceNewVariation);
        if (currentVideoIndex < 0 || currentVideoIndex >= currentTypeGroup.videoURLs.Count)
        {
            return false;
        }

        string url = currentTypeGroup.videoURLs[currentVideoIndex];
        questionVideoIndices[questionIndex] = currentVideoIndex;
        questionVideoUrls[questionIndex] = url;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        videoPlayer.url = url;
        videoPlayer.isLooping = true;
        videoPlayer.Play();

        PreloadNextVideo();
        return true;
    }

    /// <summary>
    /// Preload the next video in the background for instant playback
    void PreloadNextVideo()
    {
        if (!enablePreloading || preloadVideoPlayer == null || reviewphase || scratchMode) return;

        int nextQuestionIndex = currentQuestionIndex + 1;
        if (nextQuestionIndex >= currentQuestionValues.Length) return;

        int nextOpacityValue = GetActualOpacityForQuestion(nextQuestionIndex);
        if (nextOpacityValue < 0) return;

        LoadGroup(nextOpacityValue, currentSmokeType);
        if (currentTypeGroup == null || currentTypeGroup.videoURLs == null || currentTypeGroup.videoURLs.Count == 0) return;

        int preparedVideoIndex = SelectVideoIndexForQuestion(nextQuestionIndex, false);
        if (preparedVideoIndex < 0 || preparedVideoIndex >= currentTypeGroup.videoURLs.Count) return;

        nextVideoURL = currentTypeGroup.videoURLs[preparedVideoIndex];
        nextVideoIndex = preparedVideoIndex;
        nextPreparedQuestionIndex = nextQuestionIndex;
        nextPreparedOpacity = nextOpacityValue;
        nextPreparedSmokeType = currentSmokeType;
        isNextVideoPrepared = false;

        if (preloadVideoPlayer.isPrepared)
            preloadVideoPlayer.Stop();

        preloadVideoPlayer.url = nextVideoURL;
        preloadVideoPlayer.Prepare();
    }


    // Called when the preload video is ready

    void OnPreloadVideoPrepared(VideoPlayer source)
    {
        isNextVideoPrepared = true;
        Debug.Log("Next video preloaded and ready for instant playback!");
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
        REVIEWQUESTIONINDEX = -1;
        currentQuestionIndex = 0;
        scratchMode = false;
        SCRATCHQUESTIONINDEX = -1;
        scratchModeStartedFromReview = false;
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
            Txt_currentCompleteTest.text = "White Smoke Test";
            Txt_ContinueText.text = "Continue To Black Practice";
            CurrentTest_txt.text = "White Smoke Testing";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip White Smoke Test";

        }
        else if (testType == TestType.blackPractice)
        {
            currentSmokeType = "black";
            currentQuestionValues = questionvalues_practice_black;
            Txt_currentCompleteTest.text = "Black Smoke Practice";
            Txt_ContinueText.text = "Continue To Black Testing";
            CurrentTest_txt.text = "Black Smoke Practice";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip Black Practice";
        }
        else if (testType == TestType.blackTest)
        {
            currentSmokeType = "black";
            currentQuestionValues = questionvalues_test_black;
            Txt_currentCompleteTest.text = "Black Smoke Test";
            Txt_ContinueText.text = "Continue To Submission";
            CurrentTest_txt.text = "Black Smoke Testing";
            btn_SkipPracticeTest.GetComponentInChildren<TMPro.TMP_Text>().text = "Skip Black Smoke Test";
        }

        LoadCurrentQuestion();
        ResetQuestionVideoState();
        DisableAnswers();

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        // Reset preload state when skipping
        ClearPreparedVideoState();

        //// Automatically load and play the first video of the new test phase
        //playVideoByIndex(0);
        //isFirstQuestionLoaded = true; // Mark first question as being loaded

        StartCurrentPhaseAtFirstQuestion();
        ApplyScratchAndRefreshButtonState();

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
        reviewphase = false;
        REVIEWQUESTIONINDEX = -1;
        scratchMode = false;
        SCRATCHQUESTIONINDEX = -1;
        scratchModeStartedFromReview = false;
        ApplyScratchAndRefreshButtonState();
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
        var group = videoURLData.smokeVideos.FirstOrDefault(g => g.percentage == percentage);
        if (group == null)
        {
            currentTypeGroup = null;
            return;
        }

        currentTypeGroup = group.types.FirstOrDefault(t => t.typeName.ToLower() == type.ToLower());
        if (currentTypeGroup == null || currentTypeGroup.videoURLs.Count == 0)
        {
            currentTypeGroup = null;
        }
    }

    void PlayCurrentVideo()
    {
        if (currentTypeGroup == null || currentTypeGroup.videoURLs.Count == 0)
            return;

        // Get the URL directly from the ScriptableObject
        string videoURL = currentTypeGroup.videoURLs[currentVideoIndex];

        if (string.IsNullOrEmpty(videoURL))
        {
            Debug.LogWarning("Video URL is empty!");
            return;
        }

        // Stop current video if playing
        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        // Assign URL and play
        videoPlayer.url = videoURL;
        videoPlayer.isLooping = true;
        videoPlayer.Play();

        // Preload next video in background
        PreloadNextVideo();
    }
    public void RefreshVideo()
    {
        chkgrptype();

        int targetQuestionIndex = GetRedoTargetQuestionIndex();

        if (targetQuestionIndex < 0)
        {
            targetQuestionIndex = currentQuestionIndex;
        }

        if (!scratchMode && !reviewphase && IsPracticeMode())
        {
            currentQuestionIndex = targetQuestionIndex;
            LoadCurrentQuestion();
        }

        RemarksPannel.SetActive(false);
        answerSelected = false;
        EnableAnswers();

        if (btn_Next != null)
        {
            btn_Next.gameObject.SetActive(false);
        }

        // Refresh replays the SAME video (forceNewVariation = false)
        // Scratch loads a NEW video (forceNewVariation = true)
        LoadQuestionVideo(targetQuestionIndex, false);
    }

    public void SetSmokePercentage(int newPercentage, string typename)
    {
        currentSmokePercentage = newPercentage;
        LoadGroup(currentSmokePercentage, currentSmokeType);
    }

    public void ContinueToNextPhase()
    {
        reviewphase = false;
        REVIEWQUESTIONINDEX = -1;
        currentQuestionIndex = 0;
        isFirstQuestionLoaded = false; // Reset for new phase
        scratchMode = false;
        SCRATCHQUESTIONINDEX = -1;
        scratchModeStartedFromReview = false;
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
            btn_SkipPracticeTest.gameObject.SetActive(true);
        }
        else if (currenttype == TestType.whiteTest)
        {
            currentSmokeType = "White";
            currentQuestionValues = questionvalues_practice_black;
            currenttype = TestType.blackPractice;
            btn_SkipPracticeTest.gameObject.SetActive(true);
        }
        else if (currenttype == TestType.blackPractice)
        {
            currentSmokeType = "Black";
            btn_SkipPracticeTest.gameObject.SetActive(true);
            currentQuestionValues = questionvalues_test_black;
            currenttype = TestType.blackTest;
        }
        else if (currenttype == TestType.blackTest)
        {
            currentSmokeType = "Black";
            btn_SkipPracticeTest.gameObject.SetActive(false);
            Debug.Log("OPEN RESULT PANNEL");
        }
        else if (currenttype == TestType.TestComplete)
        {
            OpenSignaturePanel();
        }

        ResetQuestionVideoState();

        // Auto-play first video of new phase
        //playVideoByIndex(0);
        //isFirstQuestionLoaded = true;

        StartCurrentPhaseAtFirstQuestion();
        ApplyScratchAndRefreshButtonState();

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
            SetQuestionButtonLabel(i);
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
            Debug.Log("Question Opacity Value " + GetActualOpacityForQuestion(i));
            UpdateQuestionNumberLabel();
            LoadQuestionVideo(i, true);
            RemarksPannel.gameObject.SetActive(false);
            EnableAnswers();
            return;
        }
        if (reviewphase)
        {
            Debug.Log(" In Reviewphase");
            TestingCompletePannel.SetActive(false);


            REVIEWQUESTIONINDEX = i;
            Debug.Log("Question clicked " + i);
            Debug.Log("Question Opacity Value " + GetActualOpacityForQuestion(i));
            UpdateQuestionNumberLabel();
            LoadQuestionVideo(i, true);
            EnableAnswers();
            RemarksPannel.SetActive(false);
            //DisableAnswers();
            ApplyScratchAndRefreshButtonState();
        }
        else if (currentQuestionIndex == i)
        {
            TestingCompletePannel.SetActive(false);
            RemarksPannel.SetActive(false);
            Debug.Log("Question clicked " + i);
            Debug.Log("Question Opacity Value  " + GetActualOpacityForQuestion(i));
            UpdateQuestionNumberLabel();

            LoadQuestionVideo(i, false);

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

    private void SetQuestionButtonLabel(int index)
    {
        if (btn_questions == null || index < 0 || index >= btn_questions.Length || btn_questions[index] == null)
        {
            return;
        }

        string displayNumber = (index + 1).ToString();
        TMP_Text tmpLabel = btn_questions[index].GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
        {
            tmpLabel.text = displayNumber;
            return;
        }

        Text legacyLabel = btn_questions[index].GetComponentInChildren<Text>(true);
        if (legacyLabel != null)
        {
            legacyLabel.text = displayNumber;
        }
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
        ApplyScratchAndRefreshButtonState();

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
        ApplyScratchAndRefreshButtonState();

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
            if (SCRATCHQUESTIONINDEX < 0 || SCRATCHQUESTIONINDEX >= userSelectedValue.Length)
            {
                Debug.LogWarning("Scratch answer ignored because no scratch question is selected.");
                return;
            }

            int scratchQuestionIndex = SCRATCHQUESTIONINDEX;
            userSelectedValue[scratchQuestionIndex].text = "" + answersValue[i];

            if (currenttype == TestType.whitePractice) { answervalues_practice_white[scratchQuestionIndex] = answersValue[i]; }
            if (currenttype == TestType.whiteTest) { answervalues_test_white[scratchQuestionIndex] = answersValue[i]; }
            if (currenttype == TestType.blackPractice) { answervalues_practice_black[scratchQuestionIndex] = answersValue[i]; }
            if (currenttype == TestType.blackTest) { answervalues_test_black[scratchQuestionIndex] = answersValue[i]; }

            if (currenttype == TestType.whiteTest || currenttype == TestType.blackTest)
            {
                int actual = GetActualOpacityForQuestion(scratchQuestionIndex);
                int selected = answersValue[i];
                int score = Mathf.Abs(selected - actual) / 5;
                UpsertSlideRecord(
                    scratchQuestionIndex,
                    currenttype == TestType.whiteTest ? "White" : "Black",
                    actual,
                    selected,
                    score);
            }

            DisableAnswers();
            videoPlayer.Stop();

            bool returnToReview = scratchModeStartedFromReview || reviewphase;
            scratchMode = false;
            SCRATCHQUESTIONINDEX = -1;
            scratchModeStartedFromReview = false;

            ApplyScratchAndRefreshButtonState();

            int nextIndex = scratchQuestionIndex + 1;
            bool hasNextQuestion = nextIndex < btn_questions.Length;

            if (returnToReview)
            {
                answerSelected = false;
                RemarksPannel.SetActive(false);
                if (hasNextQuestion)
                {
                    REVIEWQUESTIONINDEX = nextIndex;
                    btn_questions[nextIndex].onClick.Invoke();
                }
                else
                {
                    ReOpenTestCompletePannel();
                }
                return;
            }

            bool isPracticeScratch = currenttype == TestType.whitePractice || currenttype == TestType.blackPractice;

            if (isPracticeScratch)
            {
                answerSelected = true;
                currentQuestionIndex = Mathf.Min(nextIndex, btn_questions.Length - 1);
                ShowRemarksForQuestion(scratchQuestionIndex);
                btn_SkipPracticeTest.gameObject.SetActive(true);
                ApplyScratchAndRefreshButtonState();

                if (hasNextQuestion)
                {
                    if (btn_Next != null)
                    {
                        btn_Next.gameObject.SetActive(true);
                    }
                    else
                    {
                        StartCoroutine(AutoAdvanceToNextQuestion());
                    }
                }
                else
                {
                    if (btn_Next != null)
                    {
                        btn_Next.gameObject.SetActive(false);
                    }
                    TestingCompletePannel.SetActive(false);
                    StartCoroutine(ShowTestCompleteAfterDelay(3f));
                }

                return;
            }

            if (hasNextQuestion)
            {
                currentQuestionIndex = nextIndex;
                answerSelected = false;
                if (currenttype == TestType.whiteTest || currenttype == TestType.blackTest ||
                    currenttype == TestType.whitePractice || currenttype == TestType.blackPractice)
                {
                    blackScreen.SetActive(true);
                }
                LoadCurrentQuestion();
                ApplyScratchAndRefreshButtonState();
                btn_questions[currentQuestionIndex].onClick.Invoke();
            }
            else
            {
                ApplyScratchAndRefreshButtonState();
                ShowTestCompletePanel();
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
            int actual = GetActualOpacityForQuestion(REVIEWQUESTIONINDEX);
            int diff = Mathf.Abs(selected - actual);
            int score = Mathf.Abs(diff / 5);

            if (currenttype == TestType.whiteTest || currenttype == TestType.TestComplete)
            {
                SmokeSchoolAppState.SmokeSection reviewSection = GetActiveCertificationSection();
                UpsertSlideRecord(
                    REVIEWQUESTIONINDEX,
                    reviewSection == SmokeSchoolAppState.SmokeSection.White ? "White" : "Black",
                    actual,
                    selected,
                    score);
            }

            if (currenttype == TestType.whiteTest)
            {
                UpdateCertificationResultRow(REVIEWQUESTIONINDEX, SmokeSchoolAppState.SmokeSection.White);
            }
            else if (currenttype == TestType.TestComplete)
            {
                UpdateCertificationResultRow(REVIEWQUESTIONINDEX, SmokeSchoolAppState.SmokeSection.Black);
            }

            DisableAnswers();
            videoPlayer.Stop();
            ReOpenTestCompletePannel();
            ApplyScratchAndRefreshButtonState();
            return;
        }
        else
        {
            userSelectedValue[currentQuestionIndex].text = "" + answersValue[i];

            int selected = answersValue[i];
            int actual = GetActualOpacityForQuestion(currentQuestionIndex);
            int diff = Mathf.Abs(selected - actual);
            int score = Mathf.Abs(diff / 5);

            if (currenttype == TestType.whiteTest)
            {
                UpsertSlideRecord(currentQuestionIndex, "White", actual, selected, score);
            }
            else if (currenttype == TestType.blackTest)
            {
                UpsertSlideRecord(currentQuestionIndex, "Black", actual, selected, score);
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
                    btn_SkipPracticeTest.gameObject.SetActive(true);
                    ApplyScratchAndRefreshButtonState();

                    if (btn_Next != null)
                        btn_Next.gameObject.SetActive(true);
                    else
                        StartCoroutine(AutoAdvanceToNextQuestion());
                }
                else if (isTest)
                {
                    ApplyScratchAndRefreshButtonState();
                    if (!isAutoAdvancing)
                        StartCoroutine(AutoAdvanceToNextQuestion());
                }
            }
            else
            {
                ApplyScratchAndRefreshButtonState();

                if (isPractice)
                {
                    // ✅ LAST QUESTION IN PRACTICE: Show remarks immediately
                    OpenRemarksPannel();
                    TestingCompletePannel.SetActive(false);
                    btn_Next.gameObject.SetActive(false);
                    ApplyScratchAndRefreshButtonState();
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
            openresultPannelButton.gameObject.SetActive(false);
            //Txt_ContinueText.text = "Continue To Submission";
            Txt_currentCompleteTest.text = "Black Smoke Testing Complete";
            currenttype = TestType.TestComplete;
            // ShowingFinalResult();
        }

        reviewphase = true;
        scratchMode = false;
        SCRATCHQUESTIONINDEX = -1;
        scratchModeStartedFromReview = false;
        foreach (Button X in btn_questions)
            X.interactable = true;
        ApplyScratchAndRefreshButtonState();
    }



    void ReOpenTestCompletePannel()
    {
        TestingCompletePannel.SetActive(true);
        ApplyScratchAndRefreshButtonState();
    }

    public void OpenRemarksPannel()
    {
        DisableAnswers();
        if (reviewphase)
        {
            ShowRemarksForQuestion(REVIEWQUESTIONINDEX);
            return;
        }

        ShowRemarksForQuestion(currentQuestionIndex - 1);
    }

    private void ShowRemarksForQuestion(int questionIndex)
    {
        if (questionIndex < 0 || questionIndex >= userSelectedValue.Length)
        {
            Debug.LogWarning($"Cannot show remarks for invalid question index {questionIndex}.");
            RemarksPannel.SetActive(false);
            return;
        }

        if (!int.TryParse(userSelectedValue[questionIndex].text, out int ansvalue))
        {
            Debug.LogWarning($"Cannot show remarks for question {questionIndex + 1} because no answer is recorded.");
            RemarksPannel.SetActive(false);
            return;
        }

        int ogvalue = GetActualOpacityForQuestion(questionIndex);
        RemarksPannel.SetActive(true);
        targetOpacityText.text = "" + ogvalue;
        yourReadingText.text = "" + ansvalue;

        if (ansvalue == ogvalue)
        {
            resultSummaryText.text = "Your Value was Perfect";
        }
        else if (ansvalue > ogvalue)
        {
            int x = ansvalue - ogvalue;
            resultSummaryText.text = "Your Value was " + x + "% too high";
        }
        else
        {
            int x = ogvalue - ansvalue;
            resultSummaryText.text = "Your Value was " + x + "% too low";
        }
    }

    private void UpsertSlideRecord(int questionIndex, string smokeColor, int actualOpacity, int studentAnswer, int deviation)
    {
        SmokeSchoolAppState.SmokeSection smokeSection = string.Equals(smokeColor, "White", StringComparison.OrdinalIgnoreCase)
            ? SmokeSchoolAppState.SmokeSection.White
            : SmokeSchoolAppState.SmokeSection.Black;

        SmokeSchoolAppState.RecordCertificationAnswer(
            smokeSection,
            questionIndex,
            actualOpacity,
            studentAnswer,
            ExtractVideoFilename(GetAssignedVideoUrl(questionIndex)));

        SyncCertificationScoresFromState();
        UpdateCertificationResultRow(questionIndex, smokeSection);
    }

    private string ExtractVideoFilename(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
        {
            return Path.GetFileName(uri.LocalPath);
        }

        return Path.GetFileName(url);
    }

    private string GetAssignedVideoUrl(int questionIndex)
    {
        if (questionVideoUrls != null &&
            questionIndex >= 0 &&
            questionIndex < questionVideoUrls.Length &&
            !string.IsNullOrEmpty(questionVideoUrls[questionIndex]))
        {
            return questionVideoUrls[questionIndex];
        }

        return videoPlayer != null ? videoPlayer.url : string.Empty;
    }

    private List<SmokeSchoolAppState.QuestionResult> GetIndividualFailingReadings()
    {
        return SmokeSchoolAppState.GetCertificationFailures(IndividualFailureThreshold);
    }

    private string BuildTotalScoreText(bool individualFail)
    {
        SyncCertificationScoresFromState();
        return $"White: {whiteTestScore}\nBlack: {blackTestScore}";
    }

    private void InitializeAnswerArrays()
    {
        for (int i = 0; i < btn_questions.Length; i++)
        {
            answervalues_practice_white[i] = -1;
            answervalues_test_white[i] = -1;
            answervalues_practice_black[i] = -1;
            answervalues_test_black[i] = -1;
        }
    }



    private void LogIndividualFailingReadings(List<SmokeSchoolAppState.QuestionResult> failingReadings, string context)
    {
        if (failingReadings == null || failingReadings.Count == 0)
        {
            return;
        }

        string details = string.Join("; ", failingReadings.Select(record =>
            $"{record.SmokeColorLabel} Q{record.questionNumber} actual {record.actualOpacity}, answer {record.studentAnswer}, deviation {record.deviation}"));
        Debug.LogWarning($"{context}: automatic fail due to individual reading(s) exceeding 15%: {details}");
    }

    private void SyncCertificationScoresFromState()
    {
        whiteTestScore = SmokeSchoolAppState.GetCertificationTotalScore(SmokeSchoolAppState.SmokeSection.White);
        blackTestScore = SmokeSchoolAppState.GetCertificationTotalScore(SmokeSchoolAppState.SmokeSection.Black);
    }

    private SmokeSchoolAppState.SmokeSection GetActiveCertificationSection()
    {
        if (currenttype == TestType.whiteTest)
        {
            return SmokeSchoolAppState.SmokeSection.White;
        }

        return SmokeSchoolAppState.SmokeSection.Black;
    }

    private void UpdateCertificationResultRow(int questionIndex, SmokeSchoolAppState.SmokeSection smokeSection)
    {
        if (!SmokeSchoolAppState.TryGetCertificationResult(smokeSection, questionIndex, out SmokeSchoolAppState.QuestionResult result))
        {
            return;
        }

        TMP_Text[] selectedValues = smokeSection == SmokeSchoolAppState.SmokeSection.White ? YourWhiteSelectedValue : YourBlackSelectedValue;
        TMP_Text[] actualValues = smokeSection == SmokeSchoolAppState.SmokeSection.White ? WhiteOpacityActualValue : BlackOpacityActualValue;
        TMP_Text[] scoreValues = smokeSection == SmokeSchoolAppState.SmokeSection.White ? whiteSmokeScore : BlackSmokeScore;

        if (questionIndex >= 0 && questionIndex < selectedValues.Length && selectedValues[questionIndex] != null)
        {
            selectedValues[questionIndex].text = result.studentAnswer.ToString();
        }

        if (questionIndex >= 0 && questionIndex < actualValues.Length && actualValues[questionIndex] != null)
        {
            actualValues[questionIndex].text = result.actualOpacity.ToString();
        }

        if (questionIndex >= 0 && questionIndex < scoreValues.Length && scoreValues[questionIndex] != null)
        {
            scoreValues[questionIndex].text = result.deviation.ToString();
        }
    }

    private void RefreshCertificationResultRows()
    {
        RefreshCertificationResultRowsForSection(SmokeSchoolAppState.SmokeSection.White, YourWhiteSelectedValue, WhiteOpacityActualValue, whiteSmokeScore);
        RefreshCertificationResultRowsForSection(SmokeSchoolAppState.SmokeSection.Black, YourBlackSelectedValue, BlackOpacityActualValue, BlackSmokeScore);
    }

    private void RefreshCertificationResultRowsForSection(
        SmokeSchoolAppState.SmokeSection smokeSection,
        TMP_Text[] selectedValues,
        TMP_Text[] actualValues,
        TMP_Text[] scoreValues)
    {
        for (int i = 0; i < selectedValues.Length; i++)
        {
            if (SmokeSchoolAppState.TryGetCertificationResult(smokeSection, i, out SmokeSchoolAppState.QuestionResult result))
            {
                if (selectedValues[i] != null)
                {
                    selectedValues[i].text = result.studentAnswer.ToString();
                }

                if (actualValues[i] != null)
                {
                    actualValues[i].text = result.actualOpacity.ToString();
                }

                if (scoreValues[i] != null)
                {
                    scoreValues[i].text = result.deviation.ToString();
                }
            }
            else
            {
                if (selectedValues[i] != null)
                {
                    selectedValues[i].text = string.Empty;
                }

                if (actualValues[i] != null)
                {
                    actualValues[i].text = string.Empty;
                }

                if (scoreValues[i] != null)
                {
                    scoreValues[i].text = string.Empty;
                }
            }
        }
    }

    private void LoadCurrentQuestion()
    {
        for (int i = 0; i < btn_questions.Length; i++)
        {
            btn_questions[i].interactable = false;
        }
        btn_questions[currentQuestionIndex].interactable = true;
        UpdateQuestionNumberLabel();
    }

    public void DisableAnswers()
    {
        Debug.Log("check enable");
        foreach (Button y in btn_points)
        {
            y.interactable = false;
        }
        ApplyScratchAndRefreshButtonState();

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
        ApplyScratchAndRefreshButtonState();
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
        int targetQuestionIndex = GetRedoTargetQuestionIndex();
        scratchModeStartedFromReview = reviewphase;
        scratchMode = true;

        // Save current question before scratch mode
        lastQuestionBeforeScratch = currentQuestionIndex;

        SCRATCHQUESTIONINDEX = targetQuestionIndex;
        Debug.Log("Scratch Mode Enabled");
        RemarksPannel.SetActive(false);
        TestingCompletePannel.SetActive(false);

        if (btn_Next != null)
            btn_Next.gameObject.SetActive(false);

        answerSelected = false;
        UpdateQuestionNumberLabel();
        DisableAnswers();
        if (SCRATCHQUESTIONINDEX >= 0)
        {
            LoadQuestionVideo(SCRATCHQUESTIONINDEX, true);
            EnableAnswers();
        }
    }


    // Updated score calculation logic

    public void ShowingFinalResult()
    {
        RefreshCertificationResultRows();
        SyncCertificationScoresFromState();

        // Check if user answered any question
        bool answeredAny = DidUserAnswerAnyQuestion();

        if (!answeredAny)
        {
            // User didn't answer any question
            NotPassedPanel.SetActive(true);
            QualifiedPanel.SetActive(false);
            ScreenshotSender.didPass = false;

            YourTotalScore.text = "No scored answers recorded";
            endTestButtonText.text = "Retake Test";
            Debug.Log($"User failed because no answers were selected on run #{testRunNumber}");
            return;
        }

        List<SmokeSchoolAppState.QuestionResult> individualFailingReadings = GetIndividualFailingReadings();
        bool hasIndividualFail = individualFailingReadings.Count > 0;
        bool whitePassed = whiteTestScore <= CertificationScoreThreshold;
        bool blackPassed = blackTestScore <= CertificationScoreThreshold;
        bool didPass = !hasIndividualFail && whitePassed && blackPassed;

        YourTotalScore.text = BuildTotalScoreText(hasIndividualFail);
        ScreenshotSender.didPass = didPass;

        LogIndividualFailingReadings(individualFailingReadings, "ShowingFinalResult");

        if (!didPass)
        {
            NotPassedPanel.SetActive(true);
            QualifiedPanel.SetActive(false);
            endTestButtonText.text = "Retake Test";
            Debug.Log($"FAILED - Run #{testRunNumber} - White Score: {whiteTestScore} (Pass: {whitePassed}), Black Score: {blackTestScore} (Pass: {blackPassed}), Individual Fail: {hasIndividualFail}");
        }
        else
        {
            QualifiedPanel.SetActive(true);
            NotPassedPanel.SetActive(false);
            endTestButtonText.text = "End Test";
            Debug.Log($"PASSED - Run #{testRunNumber} - White Score: {whiteTestScore} (Pass: {whitePassed}), Black Score: {blackTestScore} (Pass: {blackPassed}), Individual Fail: {hasIndividualFail}");
        }
    }



    private bool DidUserAnswerAnyQuestion()
    {
        return SmokeSchoolAppState.HasAnyCertificationAnswer();
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
        RefreshCertificationResultRows();
        SyncCertificationScoresFromState();

        List<SmokeSchoolAppState.QuestionResult> individualFailingReadings = GetIndividualFailingReadings();
        bool hasIndividualFail = individualFailingReadings.Count > 0;
        bool whitePassed = whiteTestScore <= CertificationScoreThreshold;
        bool blackPassed = blackTestScore <= CertificationScoreThreshold;
        int completedRunNumber = testRunNumber;
        DataInput_Fields.checkSceneReload = 1;
        ScreenshotSender.didPass = (!hasIndividualFail && whitePassed && blackPassed);
        YourTotalScore.text = BuildTotalScoreText(hasIndividualFail);
        LogIndividualFailingReadings(individualFailingReadings, "OnEndTestButtonClicked");

        if (ScreenshotSender.didPass)
        {
            Debug.Log($"OnEndTestButtonClicked: passed on run #{completedRunNumber}. Resetting test run counter to 1 before scene reload.");
            testRunNumber = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            testRunNumber++;
            Debug.Log($"OnEndTestButtonClicked: retake triggered from run #{completedRunNumber}. Next run will be #{testRunNumber}. Reloading scene.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Debug.Log($"Home Screen Open! Current test run after increment: #{testRunNumber}");
        }
    }
}
