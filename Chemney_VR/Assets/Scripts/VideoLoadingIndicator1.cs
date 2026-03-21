using UnityEngine;
using UnityEngine.Video;

public class VideoLoadingIndicator1 : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    [Header("Online Video URL")]
    public string videoURL = "https://res.cloudinary.com/dkzd0f0tu/video/upload/v1769441745/PlaceholderVideo_mkkihm.mp4";

    public GameObject loadingImage;

    private RectTransform loadingImageRect;
    private float rotationSpeed = 200f;
    private bool eventsRegistered;

    void Start()
    {
        if (loadingImage != null)
        {
            loadingImageRect = loadingImage.GetComponent<RectTransform>();
            loadingImage.SetActive(true);
        }

        RegisterVideoEvents();
        videoPlayer.url = videoURL;
        videoPlayer.Prepare();
    }

    void Update()
    {
        RotateLoadingImage();
    }

    private void OnDisable()
    {
        StopVideo();
    }

    private void OnDestroy()
    {
        if (!eventsRegistered || videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.loopPointReached -= OnFinished;
    }

    private void RegisterVideoEvents()
    {
        if (eventsRegistered || videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnFinished;
        eventsRegistered = true;
    }

    private void OnPrepared(VideoPlayer vp)
    {
        if (loadingImage != null)
            loadingImage.SetActive(false);

        vp.Play();
    }

    public void StopVideo()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.Stop();
    }

    private void OnFinished(VideoPlayer vp)
    {
        if (loadingImage != null)
            loadingImage.SetActive(true);
    }

    private void RotateLoadingImage()
    {
        if (loadingImageRect != null)
        {
            loadingImageRect.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }

    public void ReplayVideo()
    {
        if (videoPlayer == null)
        {
            return;
        }

        RegisterVideoEvents();

        if (loadingImage != null)
            loadingImage.SetActive(true);

        videoPlayer.url = videoURL;
        videoPlayer.Stop();
        videoPlayer.Prepare();
    }
}
