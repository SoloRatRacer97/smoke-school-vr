using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bridge script for WebGL builds — receives login data from HTML overlay
/// to bypass Unity WebGL InputField keyboard freeze.
/// Attach to any active GameObject named "LoginBridge" in the scene.
/// </summary>
public class WebGLLoginBridge : MonoBehaviour
{
    public InputField inputStudentID;
    public InputField inputEmailID;
    public Button goButton;

    /// <summary>
    /// Called from JavaScript: unityInstance.SendMessage('LoginBridge', 'ReceiveLogin', 'studentId|||email')
    /// </summary>
    public void ReceiveLogin(string data)
    {
        string[] parts = data.Split(new string[] { "|||" }, System.StringSplitOptions.None);
        if (parts.Length >= 2)
        {
            string studentId = parts[0];
            string email = parts[1];

            Debug.Log($"[WebGLLoginBridge] Received: {studentId} / {email}");

            if (inputStudentID != null)
                inputStudentID.text = studentId;
            if (inputEmailID != null)
                inputEmailID.text = email;

            // Trigger the Go button click
            if (goButton != null)
                goButton.onClick.Invoke();
        }
        else
        {
            Debug.LogError("[WebGLLoginBridge] Invalid data format: " + data);
        }
    }
}
