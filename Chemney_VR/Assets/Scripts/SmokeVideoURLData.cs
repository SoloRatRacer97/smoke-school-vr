using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SmokeVideoURLData",
    menuName = "VideoData/SmokeVideoURLData"
)]
public class SmokeVideoURLData : ScriptableObject
{
    [System.Serializable]
    public class SmokeTypeGroup
    {
        public string typeName;            // e.g. "white", "black"
        public List<string> videoURLs;     // Full URLs (https://...)
    }

    [System.Serializable]
    public class SmokeVideoGroup
    {
        [Range(0, 100)]
        public int percentage;             // 0, 10, 20, etc
        public List<SmokeTypeGroup> types; // white / black groups
    }

    public List<SmokeVideoGroup> smokeVideos;
}
