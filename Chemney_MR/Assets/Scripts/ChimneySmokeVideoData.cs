using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChimneySmokeVideoData", menuName = "VideoData/ChimneySmokeVideoData")]
public class ChimneySmokeVideoData : ScriptableObject
{
	[System.Serializable]
public class SmokeTypeGroup
{
	public string typeName; // "white", "black"
	public List<string> videoFileNames; // ["10_white_1.mp4", ...]
}
[System.Serializable]
public class SmokeVideoGroup
{
	[Range(0, 100)]
	public int percentage; // 0, 10, 20, etc
	public List<SmokeTypeGroup> types; // List of types (white, black)
}
public List<SmokeVideoGroup> smokeVideos;
	
}