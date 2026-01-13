using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class WhiteSmokeTestManager : MonoBehaviour
{
    [Header("Video and UI")]
    public VideoPlayer videoPlayer;
    public List<Button> selectionButtons;

    [Header("Panels")]
    public GameObject testPanel;
    public GameObject resultPanel;
    public GameObject completionPanel;

    [Header("Reading Panel Fields")]
    public List<TextMeshProUGUI> readingTexts; // Q1–Q30

    [Header("Result Panel Fields")]
    public List<TextMeshProUGUI> questionNumberTexts;
    public List<TextMeshProUGUI> selectedValueTexts;
    public List<TextMeshProUGUI> actualValueTexts;
    public List<TextMeshProUGUI> scorePerQuestionTexts;

    [Header("Per-Question Result")]
    public TextMeshProUGUI questionNumberText;
    public TextMeshProUGUI yourReadingText;
    public TextMeshProUGUI targetOpacityText;
    public TextMeshProUGUI resultSummaryText;

    [Header("Final Score UI")]
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI yourScoreText;
    public TextMeshProUGUI maxScoreText;

    [Header("Control Buttons")]
    public Button scratchButton;
    public Button continueButton;
    public List<Button> questionEditButtons;

    [Header("Video Clips")]
    public List<string> whiteSmokeVideos = new List<string>();

    private Dictionary<int, List<string>> videoGroups = new Dictionary<int, List<string>>();
    private List<string> selectedVideos = new List<string>();
    private List<int> correctAnswers = new List<int>();
    private List<int> userSelections = new List<int>();
    private List<int> scoreResults = new List<int>();

    private int currentQuestion = 0;
    private int currentSelection = -1;
    private int lastScratchedIndex = -1;
    private const int TotalQuestions = 30;
    private const int ScoreThreshold = 37;

    void Start()
    {
        LoadVideoList();
        GroupVideos();
        PrepareTest();

        AssignButtonListeners();
        scratchButton.onClick.AddListener(Scratch);
        continueButton.onClick.AddListener(ShowCompletionPanel);

        for (int i = 0; i < questionEditButtons.Count; i++)
        {
            int index = i;
            questionEditButtons[i].onClick.AddListener(() => EditQuestion(index));
        }
    }

    void LoadVideoList()
    {
        for (int i = 0; i <= 100; i += 5)
            whiteSmokeVideos.Add($"Point{i}.mp4");
    }

    void GroupVideos()
    {
        videoGroups.Clear();
        foreach (var clip in whiteSmokeVideos)
        {
            int percent = ExtractPercent(clip);
            if (!videoGroups.ContainsKey(percent))
                videoGroups[percent] = new List<string>();
            videoGroups[percent].Add(clip);
        }
    }

    void PrepareTest()
    {
        selectedVideos.Clear();
        correctAnswers.Clear();
        userSelections.Clear();
        scoreResults.Clear();

        List<int> keys = new List<int>(videoGroups.Keys);
        for (int i = 0; i < TotalQuestions; i++)
        {
            int percent = keys[Random.Range(0, keys.Count)];
            var variations = videoGroups[percent];
            string chosenClip = variations[Random.Range(0, variations.Count)];
            selectedVideos.Add(chosenClip);
            correctAnswers.Add(percent);
            userSelections.Add(-1);
            scoreResults.Add(0);
        }

        currentQuestion = 0;
        testPanel.SetActive(true);
        resultPanel.SetActive(false);
        completionPanel.SetActive(false);

        LoadVideo(currentQuestion);
    }

    void LoadVideo(int index)
    {
        if (index >= TotalQuestions)
        {
            ShowCompletionPanel();
            return;
        }

        string clip = selectedVideos[index];
        string path = Path.Combine(Application.streamingAssetsPath, clip);
#if UNITY_WEBGL
        videoPlayer.url = path;
#else
        videoPlayer.url = "file://" + path;
#endif
        videoPlayer.Play();
        videoPlayer.isLooping = true;

        currentSelection = -1;
        questionNumberText.text = $"Question: {index + 1} / {TotalQuestions}";
        yourReadingText.text = "";
        targetOpacityText.text = "";
        resultSummaryText.text = "";
    }

    void AssignButtonListeners()
    {
        foreach (var btn in selectionButtons)
        {
            int value = ExtractPercent(btn.name);
            btn.onClick.AddListener(() => OnAnswerSelected(value));
        }
    }

    void OnAnswerSelected(int selection)
    {
        if (currentSelection != -1) return;

        int correct = correctAnswers[currentQuestion];
        int difference = Mathf.Abs(selection - correct);
        int score = 0;
        string feedback = "";

        if (selection == correct)
        {
            feedback = "<color=green>Your reading was Perfect!</color>";
        }
        else if (difference <= 15)
        {
            feedback = $"<color=yellow>Your reading was {selection}%, {difference}% {(selection > correct ? "High" : "Low")}</color>";
            score = difference == 5 ? 1 : difference == 10 ? 2 : 3;
        }
        else
        {
            feedback = $"<color=red>Your reading was {selection}%, too {(selection > correct ? "High" : "Low")}</color>";
        }

        userSelections[currentQuestion] = selection;
        scoreResults[currentQuestion] = score;

        // Per-question display
        yourReadingText.text = $"{selection}%";
        targetOpacityText.text = $"{correct}%";
        resultSummaryText.text = feedback;

        // Reading Panel
        if (readingTexts.Count > currentQuestion)
            readingTexts[currentQuestion].text = $"{selection}%";

        // Result Panel
        if (questionNumberTexts.Count > currentQuestion)
            questionNumberTexts[currentQuestion].text = $"Q{currentQuestion + 1}";
        if (selectedValueTexts.Count > currentQuestion)
            selectedValueTexts[currentQuestion].text = $"{selection}%";
        if (actualValueTexts.Count > currentQuestion)
            actualValueTexts[currentQuestion].text = $"{correct}%";
        if (scorePerQuestionTexts.Count > currentQuestion)
            scorePerQuestionTexts[currentQuestion].text = score.ToString();

        lastScratchedIndex = currentQuestion;
        currentSelection = selection;

        currentQuestion++;
        Invoke(nameof(NextVideo), 0f);
    }

    void NextVideo()
    {
        resultPanel.SetActive(false);
        LoadVideo(currentQuestion);
    }

    void Scratch()
    {
        int prev = currentQuestion - 1;
        if (prev != lastScratchedIndex || prev < 0) return;

        int correct = correctAnswers[prev];
        string currentClip = selectedVideos[prev];
        var options = videoGroups[correct];
        string newClip = currentClip;

        if (options.Count > 1)
        {
            do newClip = options[Random.Range(0, options.Count)];
            while (newClip == currentClip);
        }

        selectedVideos[prev] = newClip;
        currentQuestion = prev;

        // Clear UI
        if (readingTexts.Count > prev) readingTexts[prev].text = "";
        if (selectedValueTexts.Count > prev) selectedValueTexts[prev].text = "";
        if (actualValueTexts.Count > prev) actualValueTexts[prev].text = "";
        if (scorePerQuestionTexts.Count > prev) scorePerQuestionTexts[prev].text = "";

        userSelections[prev] = -1;
        scoreResults[prev] = 0;
        currentSelection = -1;

        LoadVideo(currentQuestion);
    }

    void EditQuestion(int index)
    {
        if (index < 0 || index >= TotalQuestions) return;
        if (userSelections[index] == -1) return;

        currentQuestion = index;
        LoadVideo(currentQuestion);
    }

    void ShowCompletionPanel()
    {
        testPanel.SetActive(false);
        completionPanel.SetActive(true);
        resultPanel.SetActive(false);
        videoPlayer.Stop();
        videoPlayer.gameObject.SetActive(false);

        int total = 0;
        for (int i = 0; i < TotalQuestions; i++)
            total += scoreResults[i];

        totalScoreText.text = "Total Score: " + total;
        yourScoreText.text = total.ToString();
        maxScoreText.text = ScoreThreshold.ToString();
    }

    int ExtractPercent(string filename)
    {
        Match match = Regex.Match(Path.GetFileNameWithoutExtension(filename), @"\d+");
        return match.Success ? int.Parse(match.Value) : -1;
    }
}
