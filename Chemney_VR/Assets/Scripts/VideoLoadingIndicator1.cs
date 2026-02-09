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

    void Start()
    {
        if (loadingImage != null)
        {
            loadingImageRect = loadingImage.GetComponent<RectTransform>();
            loadingImage.SetActive(true);
        }

        // ✅ LOAD VIDEO FROM LINK
        videoPlayer.url = videoURL;
        Debug.Log("VIDEO URL = " + videoURL);

        // Events
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnFinished;

        videoPlayer.Prepare();
    }

    void Update()
    {
        RotateLoadingImage();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        if (loadingImage != null)
            loadingImage.SetActive(false);

        vp.Play();
    }

    public void StopVideo()
    {
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
        if (loadingImage != null)
            loadingImage.SetActive(true);

        videoPlayer.Stop();
        videoPlayer.Prepare();
    }
}
