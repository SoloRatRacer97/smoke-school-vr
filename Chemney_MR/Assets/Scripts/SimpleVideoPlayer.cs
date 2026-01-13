using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.IO;

public class SimpleVideoPlayer : MonoBehaviour
{
    public enum VideoPlayerType { low, med, high, Max };
    public string[] filenames;
    public VideoPlayer videoPlayer;

    [Header("UI Text Fields")]
    public TMP_Text opacityText;
    public TMP_Text testTypeText;
    public string currentURL = "";
    public VideoPlayerType currentvideo;

    [Header("Loading UI")]
    public GameObject loadingImage;   // your loading image
    private RectTransform loadingImageRect;
    public float rotationSpeed = 200f;

    void Start()
    {
        // Show loading image immediately on start
        if (loadingImage != null)
        {
            loadingImage.SetActive(true);
            loadingImageRect = loadingImage.GetComponent<RectTransform>();
        }

        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    void Update()
    {
        // Rotate the loading image if active
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
        string filename = "";
        int percent = 0;

        // Show loading image on new video
        if (loadingImage != null)
            loadingImage.SetActive(true);

        switch (videoPlayerType)
        {
            case VideoPlayerType.low:
                filename = filenames[0];
                percent = 25;
                break;

            case VideoPlayerType.med:
                filename = filenames[1];
                percent = 50;
                break;

            case VideoPlayerType.high:
                filename = filenames[2];
                percent = 75;
                break;

            case VideoPlayerType.Max:
                filename = filenames[3];
                percent = 100;
                break;
        }

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

        // Prepare video before playing
        videoPlayer.Prepare();

        UpdateOpacityUI(percent);
        currentURL = url;

        Debug.Log("Preparing video: " + url);
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play();
        if (loadingImage != null)
            loadingImage.SetActive(false); // Hide loading image when video starts
        Debug.Log("Video started!");
    }

    void UpdateOpacityUI(int percent)
    {
        opacityText.text = "Opacity: " + percent + "%";
    }

    private void RotateLoadingImage()
    {
        if (loadingImageRect != null)
        {
            loadingImageRect.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }
}
