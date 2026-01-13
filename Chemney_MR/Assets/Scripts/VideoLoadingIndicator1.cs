using UnityEngine;
using UnityEngine.Video;

public class VideoLoadingIndicator1 : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string videoFileName = "";
    public GameObject loadingImage;
    private RectTransform loadingImageRect;
    private float rotationSpeed = 200f;

    void Start()
    {
        if (loadingImage != null)
            loadingImageRect = loadingImage.GetComponent<RectTransform>();
        // Show loading image when video starts preparing
        if (loadingImage != null)
            loadingImage.SetActive(true);

        // ------------------------------
        // ✅ NEW PATH LOGIC YOU REQUESTED
        // ------------------------------
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);

#if UNITY_WEBGL
        string url = path;
#else
        string url = "file://" + path;
#endif

        videoPlayer.url = url;
        // ------------------------------

        Debug.Log("VIDEO URL = " + videoPlayer.url);

        // Prepare video
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnFinished;
        videoPlayer.Prepare();
    }


    private void Update()
    {
        RotateLoadingImage();
    }
    private void OnPrepared(VideoPlayer vp)
    {
        // Hide loading image when video is ready
        if (loadingImage != null)
            loadingImage.SetActive(false);

        vp.Play();
    }

    public void StopVideo(VideoPlayer vp)
    {
        vp.Stop();
    }

    private void OnFinished(VideoPlayer vp)
    {
        // Optional: show image again after finish
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
