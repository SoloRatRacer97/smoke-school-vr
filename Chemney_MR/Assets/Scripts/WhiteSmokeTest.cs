using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class WhiteSmokeTest : MonoBehaviour
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
    private GameObject actualTestPanel;

    [Header("Scratch Button")]
    public Button scratchButton;

    private List<int> correctAnswers = new List<int>();
    private List<int> userSelections = new List<int>();
    private List<string> feedbackResults = new List<string>();
    private List<string> practiceVideos = new List<string>();
    private Dictionary<int, List<string>> groupedVideos = new Dictionary<int, List<string>>();

    private int currentQuestion = -1;
    private int currentSelection = -1;
    private bool[] questionAnswered;

    void Start()
    {
        LoadPracticeVideos();
        GroupVideosByOpacity();
        questionAnswered = new bool[practiceVideos.Count];

        AssignButtonListeners();

        resultPanel.SetActive(false);
        actualTestPanel.SetActive(false);
        practicePanel.SetActive(true);

        SetTestType("Practice White Smoke");

        // Auto-load and play first question video
        currentQuestion = 0;
        LoadVideo(currentQuestion);
        UpdateQuestionButtonInteractivity();
    }

    void LoadPracticeVideos()
    {
        List<string> availableVideos = new List<string>();
        for (int i = 0; i <= 100; i += 5)
        {
            availableVideos.Add($"Point{i}.mp4");
        }

        for (int i = 0; i < 25; i++)
        {
            int randIndex = Random.Range(0, availableVideos.Count);
            practiceVideos.Add(availableVideos[randIndex]);
        }
    }

    void GroupVideosByOpacity()
    {
        groupedVideos.Clear();
        foreach (var video in practiceVideos)
        {
            int opacity = ExtractPercentageFromFilename(video);
            if (!groupedVideos.ContainsKey(opacity))
                groupedVideos[opacity] = new List<string>();
            groupedVideos[opacity].Add(video);
        }
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

        scratchButton.onClick.AddListener(ScratchCurrentVideo);
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

    void OnQuestionClicked(int index)
    {
        if (index != currentQuestion + 1) return; // Only allow next unanswered question

        currentQuestion = index;
        currentSelection = -1;
        LoadVideo(index);
    }

    void LoadVideo(int index)
    {
        if (index >= practiceVideos.Count)
        {
            EndPracticeTest();
            return;
        }

        string filename = practiceVideos[index];
        string path = Path.Combine(Application.streamingAssetsPath, filename);

#if UNITY_WEBGL
        videoPlayer.url = path;
#else
        videoPlayer.url = "file://" + path;
#endif

        videoPlayer.gameObject.SetActive(true);
        videoPlayer.isLooping = true;
        videoPlayer.Play();

        int correct = ExtractPercentageFromFilename(filename);

        if (correctAnswers.Count <= index)
            correctAnswers.Add(correct);
        else
            correctAnswers[index] = correct;

        questionText.text = $"Question: {index + 1}";

        targetOpacityText.text = "";
        yourReadingText.text = "";
        resultSummaryText.text = "";

        resultPanel.SetActive(false);
    }

    int ExtractPercentageFromFilename(string filename)
    {
        string nameOnly = Path.GetFileNameWithoutExtension(filename);
        string digits = Regex.Match(nameOnly, @"\d+").Value;
        return int.TryParse(digits, out int value) ? value : -1;
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
    }

    void UpdateQuestionButtonInteractivity()
    {
        for (int i = 0; i < questionButtons.Count; i++)
        {
            questionButtons[i].interactable = (i == currentQuestion + 1 && questionAnswered[currentQuestion]);
        }

        // Allow replay of current video only if not answered
        if (currentQuestion >= 0 && !questionAnswered[currentQuestion])
        {
            questionButtons[currentQuestion].interactable = true;
        }
    }

    void ScratchCurrentVideo()
    {
        if (currentQuestion < 0 || currentQuestion >= practiceVideos.Count) return;

        int correct = correctAnswers[currentQuestion];
        List<string> options = groupedVideos.ContainsKey(correct) ? groupedVideos[correct] : null;

        if (options == null || options.Count <= 1) return;

        string currentClip = practiceVideos[currentQuestion];
        string newClip = currentClip;

        do {
            newClip = options[Random.Range(0, options.Count)];
        } while (newClip == currentClip);

        practiceVideos[currentQuestion] = newClip;

        // Reset reading and remarks
        if (answerTexts.Count > currentQuestion) answerTexts[currentQuestion].text = "";
        if (remarksSelectedTexts.Count > currentQuestion) remarksSelectedTexts[currentQuestion].text = "";
        if (remarksFeedbackTexts.Count > currentQuestion) remarksFeedbackTexts[currentQuestion].text = "";
        if (userSelections.Count > currentQuestion) userSelections[currentQuestion] = -1;
        if (feedbackResults.Count > currentQuestion) feedbackResults[currentQuestion] = "";

        currentSelection = -1;
        questionAnswered[currentQuestion] = false;

        LoadVideo(currentQuestion);
    }

    void EndPracticeTest()
    {
        practicePanel.SetActive(false);
        actualTestPanel.SetActive(true);
    }

    void SetTestType(string testName)
    {
        if (testTypeText != null)
            testTypeText.text = testName;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
