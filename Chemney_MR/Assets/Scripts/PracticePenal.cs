using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Practicepenal : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject practicePanel;
    public GameObject testPanel;

    [Header("Buttons")]
    public Button startPracticeButton;
    public Button startTestButton;
    public Button beginPracticeButton;

    public Button button25;
    public Button button50;
    public Button button75;
    public Button button100;

    [Header("Practice Test Buttons")]
    public List<Button> selectionButtons; // Point 0–100 buttons for real practice

    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("UI Elements")]
    public GameObject introImage;
    public TextMeshProUGUI opacityDisplayText;
    public List<TextMeshProUGUI> practiceAnswerTexts;   // Selected value display
    public List<TextMeshProUGUI> practiceFeedbackTexts; // Feedback display

    private List<string> videoFileNames = new List<string>();
    private List<int> correctValues = new List<int>();

    private int currentIndex = 0;
    private bool awaitingSelection = false;

    void Start()
    {
        testPanel.SetActive(false);

        // Add Point0.mp4 to Point100.mp4
        for (int i = 0; i <= 100; i += 5)
        {
            string filename = $"Point{i}.mp4";
            videoFileNames.Add(filename);
            correctValues.Add(i);
        }

        startPracticeButton.onClick.AddListener(OpenPracticePanel);
        startTestButton.onClick.AddListener(OpenTestPanel);

        beginPracticeButton.onClick.AddListener(() =>
        {
            currentIndex = 0;
            if (introImage != null)
                introImage.SetActive(false);
            LoadVideo(currentIndex);
        });

        // Tutorial buttons (keep these)
        button25.onClick.AddListener(() => LoadVideoByFilename("smoke25.mp4"));
        button50.onClick.AddListener(() => LoadVideoByFilename("smoke50.mp4"));
        button75.onClick.AddListener(() => LoadVideoByFilename("smoke75.mp4"));
        button100.onClick.AddListener(() => LoadVideoByFilename("smoke100.mp4"));

        // Selection buttons (0–100%)
        foreach (Button btn in selectionButtons)
        {
            int value = ExtractValueFromButtonName(btn.name);
            btn.onClick.AddListener(() => OnUserSelected(value));
        }
    }

    void OpenPracticePanel()
    {
        practicePanel.SetActive(true);
        testPanel.SetActive(false);
    }

    void OpenTestPanel()
    {
        testPanel.SetActive(true);
        practicePanel.SetActive(false);
    }

    void LoadVideoByFilename(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return;

        if (!videoPlayer.gameObject.activeSelf)
            videoPlayer.gameObject.SetActive(true);

        string path = Path.Combine(Application.streamingAssetsPath, filename);
#if UNITY_WEBGL
        videoPlayer.url = path;
#else
        videoPlayer.url = "file://" + path;
#endif
        videoPlayer.Stop();
        videoPlayer.isLooping = true;
        videoPlayer.Play();

        string digits = Regex.Match(filename, @"\d+").Value;
        if (opacityDisplayText != null && int.TryParse(digits, out int value))
        {
            opacityDisplayText.text = $"Opacity: {value}%";
        }
    }

    void LoadVideo(int index)
    {
        if (index >= videoFileNames.Count)
        {
            Debug.Log("✅ Practice complete.");
            return;
        }

        string filename = videoFileNames[index];
        LoadVideoByFilename(filename);

        awaitingSelection = true;
    }

    void OnUserSelected(int selectedValue)
    {
        if (!awaitingSelection) return;

        int correct = correctValues[currentIndex];
        int diff = Mathf.Abs(selectedValue - correct);

        string feedback = "";
        if (selectedValue == correct)
            feedback = "Perfect!";
        else if (diff <= 15)
            feedback = selectedValue > correct ? "bit high" : "bit low";
        else
            feedback = selectedValue > correct ? "too high" : "too low";

        // Display selected value
        if (practiceAnswerTexts.Count > currentIndex)
            practiceAnswerTexts[currentIndex].text = selectedValue + "%";

        // Display feedback
        if (practiceFeedbackTexts.Count > currentIndex)
            practiceFeedbackTexts[currentIndex].text = feedback;

        awaitingSelection = false;
        currentIndex++;
        Invoke(nameof(LoadNextVideo), 2f);
    }

    void LoadNextVideo()
    {
        LoadVideo(currentIndex);
    }

    int ExtractValueFromButtonName(string name)
    {
        if (name.StartsWith("Point "))
        {
            string digits = name.Substring(6);
            if (int.TryParse(digits, out int value))
                return value;
        }
        return -1;
    }
}