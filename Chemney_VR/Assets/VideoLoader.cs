using UnityEngine;
using UnityEngine.Video;
using System;
using System.Collections.Generic;
using System.IO;

public class VideoLoader : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public List<string> videoFileNames = new List<string>();
    public Action OnVideoPrepared;

    void Awake()
    {
        GenerateVideoList();
    }

    void GenerateVideoList()
    {
        for (int i = 0; i <= 100; i += 5)
            videoFileNames.Add($"Point{i}.mp4");

        ShuffleList(videoFileNames);
    }

    public int VideoCount => videoFileNames.Count;

    public string GetFilename(int index)
    {
        return index < videoFileNames.Count ? videoFileNames[index] : "";
    }

    public void PlayVideo(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            Debug.LogError("Filename is empty.");
            return;
        }

        if (!videoPlayer.gameObject.activeSelf)
            videoPlayer.gameObject.SetActive(true);

        string path = Path.Combine(Application.streamingAssetsPath, filename);

#if UNITY_WEBGL
        videoPlayer.url = path;
#else
        videoPlayer.url = "file://" + path;
#endif

        videoPlayer.isLooping = false;
        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.Prepare();
    }

    private void HandlePrepared(VideoPlayer vp)
    {
        videoPlayer.Play();
        videoPlayer.prepareCompleted -= HandlePrepared;
        OnVideoPrepared?.Invoke();
    }

    public void Stop()
    {
        if (videoPlayer.isPlaying)
            videoPlayer.Stop();
        videoPlayer.gameObject.SetActive(false);
    }

    public int ExtractPercentageFromFilename(string filename)
    {
        string nameOnly = Path.GetFileNameWithoutExtension(filename);
        string digits = System.Text.RegularExpressions.Regex.Match(nameOnly, @"\d+").Value;
        return int.TryParse(digits, out int value) ? value : -1;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
