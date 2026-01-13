using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ChimneySmokePlayer : MonoBehaviour
{
	public ChimneySmokeVideoData videoData; // Reference to ScriptableObject
	public VideoPlayer videoPlayer;
	public Button refreshButton;
	[Range(0, 100)]
	public int currentSmokePercentage = 0;
	public string currentSmokeType = "white"; // or "black"

	private int currentVideoIndex = 0;
	private ChimneySmokeVideoData.SmokeTypeGroup currentTypeGroup;

	private void Start()
	{
		refreshButton.onClick.AddListener(RefreshVideo);
		LoadGroup(currentSmokePercentage, currentSmokeType);
		PlayCurrentVideo();
	}
	void LoadGroup(int percentage, string type)
	{
		var group = videoData.smokeVideos.FirstOrDefault(g => g.percentage == percentage);
		if (group == null)
		{
			Debug.LogWarning("No smoke group found for " + percentage + "%");
			return;
		}

		currentTypeGroup = group.types.FirstOrDefault(t => t.typeName.ToLower() == type.ToLower());

		if (currentTypeGroup == null || currentTypeGroup.videoFileNames.Count == 0)
		{
			Debug.LogWarning("No videos found for type: " + type + " at " + percentage + "%");
			return;
		}

		// 🔀 Random first video
		currentVideoIndex = Random.Range(0, currentTypeGroup.videoFileNames.Count);
	}

	void PlayCurrentVideo()
	{
		if (currentTypeGroup == null || currentTypeGroup.videoFileNames.Count == 0)
			return;

		string fileName = currentTypeGroup.videoFileNames[currentVideoIndex];
		string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
		videoPlayer.url = path;
		videoPlayer.Play();
	}
	void RefreshVideo()
	{
		if (currentTypeGroup == null || currentTypeGroup.videoFileNames.Count == 0)
			return;

		currentVideoIndex = (currentVideoIndex + 1) % currentTypeGroup.videoFileNames.Count;
		PlayCurrentVideo();
	}



	public void loadnextvideo()
	{

		int temp = GetRandomFromSix();


		SetSmokePercentage(temp, currentSmokeType);
	//	SetSmokePercentage(temp);


	}


	int GetRandomFromSix()
	{
		int[] values = { 10,15, 25, 40, 50,95 }; // your custom values
		return values[Random.Range(0, values.Length)];
	}


	// 🧠 Call this to change smoke percentage and restart from index 0
	public void SetSmokePercentage(int newPercentage,string typename)
	{
		currentSmokePercentage = newPercentage;
		LoadGroup(currentSmokePercentage, currentSmokeType);
		PlayCurrentVideo();
	}
}
