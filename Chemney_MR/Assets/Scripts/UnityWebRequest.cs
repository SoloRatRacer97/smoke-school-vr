//using System.Collections;
//using UnityEngine;
//using System;
//using UnityEngine.Networking;
//using System.Text;

//public class ScreenshotSender : MonoBehaviour
//{
//    public RectTransform panelToCapture;
//    public Camera CaptureCamera;
//    private string sendGridUrl = "https://smokeschoolvr.piper-386.workers.dev/";
//    public static string messageToSend;
//    public void CaptureAndSend()
//    {
//        StartCoroutine(CapturePanelAndSend());
//    }

//    IEnumerator CapturePanelAndSend()
//    {
//        CaptureCamera.gameObject.SetActive(true);
//        panelToCapture.gameObject.SetActive(true);

//        yield return new WaitForSeconds(0.5f);
//        yield return new WaitForEndOfFrame();

//        Vector2 panelSize = panelToCapture.rect.size;
//        int width = Mathf.Max(1280, (int)(panelSize.x * 3f));
//        int height = Mathf.Max(720, (int)(panelSize.y * 3f));

//        RenderTexture rt = new RenderTexture(width, height, 24);
//        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

//        CaptureCamera.targetTexture = rt;
//        CaptureCamera.Render();
//        RenderTexture.active = rt;
//        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//        tex.Apply();

//        byte[] jpgData = tex.EncodeToJPG(90);
//        string base64Image = Convert.ToBase64String(jpgData);

//        // Cleanup
//        CaptureCamera.targetTexture = null;
//        RenderTexture.active = null;
//        Destroy(rt);
//        Destroy(tex);
//        CaptureCamera.gameObject.SetActive(false);

//        // ✅ Send directly
//        string emailToSend = DataInput_Fields.playerEmail;
//        StartCoroutine(SendUsingSendgrid(emailToSend, "Smoke Test Result", $"See attached screenshot {emailToSend}", base64Image));
//        //StartCoroutine(SendTextToWeb(messageToSend));
//    }

//    IEnumerator SendTextToWeb(string message)
//    {
//        WWWForm form = new WWWForm();
//        form.AddField("message", message);

//        using (UnityWebRequest www = UnityWebRequest.Post("YOUR_WEB_URL_HERE", form))
//        {
//            yield return www.SendWebRequest();

//            if (www.result != UnityWebRequest.Result.Success)
//            {
//                Debug.LogError("Text send failed: " + www.error);
//            }
//            else
//            {
//                Debug.Log("Text sent successfully: " + www.downloadHandler.text);
//            }
//        }
//    }

//    IEnumerator SendUsingSendgrid(string toEmail, string subject, string message, string base64Image)
//    {
//        string jsonPayload = $@"
//        {{
//            ""personalizations"": [
//                {{
//                    ""to"": [ {{ ""email"": ""{toEmail}"" }} ],
//                    ""subject"": ""{subject}""
//                }}
//            ],
//            ""from"": {{
//                ""email"": ""info@piperhale.com"",
//                ""name"": ""Smoke School""
//            }},
//            ""content"": [
//                {{ ""type"": ""text/plain"", ""value"": ""{message.Replace("\"", "\\\"")}"" }}
//            ],
//            ""attachments"": [
//                {{
//                    ""content"": ""{base64Image}"",
//                    ""filename"": ""screenshot.jpg"",
//                    ""type"": ""image/jpeg"",
//                    ""disposition"": ""attachment""
//                }}
//            ]
//        }}".Trim();

//        UnityWebRequest www = new UnityWebRequest(sendGridUrl, "POST");
//        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
//        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
//        www.downloadHandler = new DownloadHandlerBuffer();
//        www.SetRequestHeader("Content-Type", "application/json");

//        yield return www.SendWebRequest();

//        if (www.result != UnityWebRequest.Result.Success)
//        {
//            Debug.LogError("❌ Email send failed: " + www.error + "\n" + www.downloadHandler.text);
//        }
//        else
//        {
//            Debug.Log("✅ Email sent successfully from WebVR!");
//        }
//    }
//}


using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Text;
public class ScreenshotSender : MonoBehaviour
{
    public RectTransform panelToCapture;
    public Camera CaptureCamera;

    private string sendGridUrl = "https://smokeschoolvr.piper-386.workers.dev/";
    public static string messageToSend;

    private string ccEmail = "piper@smokeschoolinc.com";

    public void CaptureAndSend()
    {
        StartCoroutine(CapturePanelAndSend());
    }

    IEnumerator CapturePanelAndSend()
    {
        CaptureCamera.gameObject.SetActive(true);
        panelToCapture.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);
        yield return new WaitForEndOfFrame();

        Vector2 panelSize = panelToCapture.rect.size;
        int width = Mathf.Max(1280, (int)(panelSize.x * 3f));
        int height = Mathf.Max(720, (int)(panelSize.y * 3f));

        RenderTexture rt = new RenderTexture(width, height, 24);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        CaptureCamera.targetTexture = rt;
        CaptureCamera.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] jpgData = tex.EncodeToJPG(90);
        string base64Image = Convert.ToBase64String(jpgData);

        CaptureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(tex);
        CaptureCamera.gameObject.SetActive(false);

        string playerEmail = DataInput_Fields.playerEmail;
        string subject = "Smoke Test Result";

        // 1️⃣ Send to user
        string userMessage = "See attached screenshot";
        StartCoroutine(SendUsingSendgrid(playerEmail, subject, userMessage, base64Image));

        // 2️⃣ Send separate email to CC with player email
        string ccMessage = $"Test by {playerEmail}";
        StartCoroutine(SendUsingSendgrid(ccEmail, subject, ccMessage, base64Image));
    }

    IEnumerator SendUsingSendgrid(string toEmail, string subject, string message, string base64Image)
    {
        string jsonPayload = $@"
        {{
            ""personalizations"": [
                {{
                    ""to"": [ {{ ""email"": ""{toEmail}"" }} ],
                    ""subject"": ""{subject}""
                }}
            ],
            ""from"": {{
                ""email"": ""info@piperhale.com"",
                ""name"": ""Smoke School""
            }},
            ""content"": [
                {{
                    ""type"": ""text/plain"",
                    ""value"": ""{message.Replace("\"", "\\\"")}"" 
                }}
            ],
            ""attachments"": [
                {{
                    ""content"": ""{base64Image}"",
                    ""filename"": ""screenshot.jpg"",
                    ""type"": ""image/jpeg"",
                    ""disposition"": ""attachment""
                }}
            ]
        }}";

        UnityWebRequest www = new UnityWebRequest(sendGridUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($":x: Email send failed to {toEmail}: {www.error}\n{www.downloadHandler.text}");
        }
        else
        {
            Debug.Log($":white_check_mark: Email sent successfully to {toEmail}!");
        }
    }
}