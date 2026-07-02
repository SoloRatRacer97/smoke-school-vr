using UnityEngine;
using UnityEngine.Video;
using TMPro;
using System.Collections.Generic;

public class SimpleVideoPlayer : MonoBehaviour
{
    public enum VideoPlayerType { low, med, high, Max }

    [Header("Video URLs (Low → Max)")]
    public string[] videoURLs; // put full URLs here
    public bool preloadVideoURLs = true;

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
    private readonly List<VideoPlayer> preloadPlayers = new List<VideoPlayer>();
    private int nextPreloadIndex = 0;

    void Start()
    {
        if (loadingImage != null)
        {
            loadingImage.SetActive(true);
            loadingImageRect = loadingImage.GetComponent<RectTransform>();
        }

        videoPlayer.prepareCompleted += OnVideoPrepared;

        if (preloadVideoURLs)
        {
            PreloadNextVideoURL();
        }
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

    void PreloadNextVideoURL()
    {
        if (videoURLs == null)
            return;

        while (nextPreloadIndex < videoURLs.Length && string.IsNullOrEmpty(videoURLs[nextPreloadIndex]))
        {
            nextPreloadIndex++;
        }

        if (nextPreloadIndex >= videoURLs.Length)
            return;

        GameObject obj = new GameObject($"PracticePreloadVideoPlayer_{nextPreloadIndex}");
        obj.transform.SetParent(transform);

        VideoPlayer preloadPlayer = obj.AddComponent<VideoPlayer>();
        preloadPlayer.playOnAwake = false;
        preloadPlayer.waitForFirstFrame = true;
        preloadPlayer.skipOnDrop = true;
        preloadPlayer.renderMode = VideoRenderMode.APIOnly;
        preloadPlayer.url = videoURLs[nextPreloadIndex];
        preloadPlayer.prepareCompleted += OnPreloadPrepared;

        preloadPlayers.Add(preloadPlayer);
        nextPreloadIndex++;
        preloadPlayer.Prepare();
    }

    void OnPreloadPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnPreloadPrepared;
        PreloadNextVideoURL();
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

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }

        for (int i = 0; i < preloadPlayers.Count; i++)
        {
            if (preloadPlayers[i] == null)
                continue;

            preloadPlayers[i].prepareCompleted -= OnPreloadPrepared;
            if (preloadPlayers[i].isPlaying || preloadPlayers[i].isPrepared)
            {
                preloadPlayers[i].Stop();
            }
        }
    }
}
