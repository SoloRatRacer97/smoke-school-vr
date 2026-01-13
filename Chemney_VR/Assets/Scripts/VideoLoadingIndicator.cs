// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.Video;

// public class VideoLoadingIndicator : MonoBehaviour
// {
//     public VideoPlayer videoPlayer;
//     public GameObject loadingImage;
//     public float rotationSpeed = 200f;

//     private RectTransform loadingImageRect;

//     private void Start()
//     {
//        loadingImage.SetActive(false);
//         if (videoPlayer == null)
//         {
//             Debug.LogError("VideoPlayer not assigned!");
//             return;
//         }

//         if (loadingImage == null)
//         {
//             Debug.LogError("LoadingImage not assigned!");
//             return;
//         }

//         loadingImageRect = loadingImage.GetComponent<RectTransform>();
//         loadingImage.SetActive(true);

//         videoPlayer.started += OnVideoStarted;
//         videoPlayer.loopPointReached += OnVideoEnded;
//         videoPlayer.errorReceived += OnVideoError;
//     }

//     private void Update()
//     {
//         bool isVideoPlaying = videoPlayer.isPlaying;

//         if (!isVideoPlaying)
//         {
//             // Show and rotate loading image when video is not playing
//             loadingImage.SetActive(true);
//             RotateLoadingImage();
//         }
//         else
//         {
//             loadingImage.SetActive(false);
//         }
//     }

//     private void RotateLoadingImage()
//     {
//         if (loadingImageRect != null)
//         {
//             loadingImageRect.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
//         }
//     }

//     public void OnVideoStarted(VideoPlayer vp)
//     {
//         loadingImage.SetActive(false);
//     }

//     private void OnVideoEnded(VideoPlayer vp)
//     {
//         loadingImage.SetActive(true);
//     }

//     private void OnVideoError(VideoPlayer vp, string message)
//     {
//         Debug.LogError("VideoPlayer error: " + message);
//         loadingImage.SetActive(true);
//     }
// }
