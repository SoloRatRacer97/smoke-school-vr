using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class SimpleVideoPlayer : MonoBehaviour
{
    public enum VideoPlayerType { low, med, high, Max }

    [Header("Video URLs (Low → Max)")]
    public string[] videoURLs; // put full URLs here

    public VideoPlayer videoPlayer;

    [Header("UI Text Fields")]
    public TMP_Text opacityText;
    public TMP_Text testTypeText;
    public string currentURL = "";
    public VideoPlayerType currentvideo;

    [Header("Loading UI")]
    public GameObject loadingImage;
    private RectTransform loadingImageRect;
    public float rotationSpeed = 200f;

    void Start()
    {
        if (loadingImage != null)
        {
            loadingImage.SetActive(true);
            loadingImageRect = loadingImage.GetComponent<RectTransform>();
        }

        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    void Update()
    {
        if (loadingImage != null && loadingImage.activeSelf)
            RotateLoadingImage();
    }

    public void playVideoURL(int x)
    {
        PlayVideo((VideoPlayerType)x);
    }

    void PlayVideo(VideoPlayerType videoPlayerType)
    {
        currentvideo = videoPlayerType;
        int percent = 0;
        string url = "";

        if (loadingImage != null)
            loadingImage.SetActive(true);

        switch (videoPlayerType)
        {
            case VideoPlayerType.low:
                url = videoURLs[0];
                percent = 25;
                break;

            case VideoPlayerType.med:
                url = videoURLs[1];
                percent = 50;
                break;

            case VideoPlayerType.high:
                url = videoURLs[2];
                percent = 75;
                break;

            case VideoPlayerType.Max:
                url = videoURLs[3];
                percent = 100;
                break;
        }

        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        videoPlayer.url = url;
        videoPlayer.isLooping = true;

        videoPlayer.Prepare();

        currentURL = url;
        UpdateOpacityUI(percent);

        Debug.Log("Preparing video from URL: " + url);
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play();
        if (loadingImage != null)
            loadingImage.SetActive(false);

        Debug.Log("Video started!");
    }

    void UpdateOpacityUI(int percent)
    {
        opacityText.text = "Opacity: " + percent + "%";
    }

    private void RotateLoadingImage()
    {
        if (loadingImageRect != null)
            loadingImageRect.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}
