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
using System.Collections.Generic;
public class ScreenshotSender : MonoBehaviour
{
    public RectTransform panelToCapture;
    public Camera CaptureCamera;

    private string sendGridUrl = "https://smokeschoolvr.piper-386.workers.dev/";
    public static string messageToSend;

    private string ccEmail = "todd@cascadewebsolutions.co";

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
        string subject = "Smoke Test Result - " + DataInput_Fields.studentname;
        string htmlMessage = BuildHtmlMessage(playerEmail);

        // 1️⃣ Send to user
        StartCoroutine(SendUsingSendgrid(playerEmail, subject, htmlMessage, base64Image));

        // 2️⃣ Send separate email to CC with player email
        StartCoroutine(SendUsingSendgrid(ccEmail, subject, htmlMessage, base64Image));
    }

    private string BuildHtmlMessage(string playerEmail)
    {
        List<ManagerTesting.SlideRecord> orderedRecords = new List<ManagerTesting.SlideRecord>(ManagerTesting.slideRecords);
        orderedRecords.Sort((a, b) =>
        {
            int colorCompare = string.Equals(a.smokeColor, b.smokeColor, StringComparison.OrdinalIgnoreCase)
                ? 0
                : (string.Equals(a.smokeColor, "White", StringComparison.OrdinalIgnoreCase) ? -1 : 1);
            return colorCompare != 0 ? colorCompare : a.questionNumber.CompareTo(b.questionNumber);
        });

        int whiteScore = 0;
        int blackScore = 0;
        foreach (ManagerTesting.SlideRecord record in orderedRecords)
        {
            if (string.Equals(record.smokeColor, "White", StringComparison.OrdinalIgnoreCase))
            {
                whiteScore += record.deviation;
            }
            else if (string.Equals(record.smokeColor, "Black", StringComparison.OrdinalIgnoreCase))
            {
                blackScore += record.deviation;
            }
        }

        bool whitePassed = whiteScore <= 37;
        bool blackPassed = blackScore <= 37;
        string resultText = didPass ? "Passed" : "Failed";
        string dateText = DateTime.Now.ToString("MM/dd/yyyy");

        StringBuilder rows = new StringBuilder();
        for (int i = 0; i < orderedRecords.Count; i++)
        {
            ManagerTesting.SlideRecord record = orderedRecords[i];
            string backgroundColor = i % 2 == 0 ? "#ffffff" : "#f5f5f5";
            string deviationStyle = record.deviation > 3 ? "color:#b91c1c;font-weight:bold;" : "color:#111827;";

            rows.Append($@"
                <tr style=""background-color:{backgroundColor};"">
                    <td style=""border:1px solid #d1d5db;padding:8px;"">{record.questionNumber}</td>
                    <td style=""border:1px solid #d1d5db;padding:8px;"">{HtmlEncode(record.smokeColor)}</td>
                    <td style=""border:1px solid #d1d5db;padding:8px;"">{HtmlEncode(record.videoFilename)}</td>
                    <td style=""border:1px solid #d1d5db;padding:8px;"">{record.actualOpacity}%</td>
                    <td style=""border:1px solid #d1d5db;padding:8px;"">{record.studentAnswer}%</td>
                    <td style=""border:1px solid #d1d5db;padding:8px;{deviationStyle}"">{record.deviation}</td>
                </tr>");
        }

        return $@"
        <html>
            <body style=""margin:0;padding:24px;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;color:#111827;"">
                <div style=""max-width:960px;margin:0 auto;background-color:#ffffff;border:1px solid #d1d5db;padding:24px;"">
                    <div style=""font-size:24px;font-weight:bold;color:#0f172a;margin-bottom:8px;"">Smokeschoolinc.com</div>
                    <div style=""font-size:14px;color:#4b5563;margin-bottom:24px;"">Smoke opacity certification results</div>
                    <div style=""margin-bottom:20px;line-height:1.6;"">
                        <div><strong>Student:</strong> {HtmlEncode(DataInput_Fields.studentname)}</div>
                        <div><strong>Email:</strong> {HtmlEncode(playerEmail)}</div>
                        <div><strong>Date:</strong> {dateText}</div>
                        <div><strong>White Score:</strong> {whiteScore} {(whitePassed ? "(Pass)" : "(Fail)")}</div>
                        <div><strong>Black Score:</strong> {blackScore} {(blackPassed ? "(Pass)" : "(Fail)")}</div>
                        <div><strong>Overall Result:</strong> {resultText}</div>
                    </div>
                    <table style=""width:100%;border-collapse:collapse;font-size:14px;"">
                        <thead>
                            <tr style=""background-color:#111827;color:#ffffff;"">
                                <th style=""border:1px solid #d1d5db;padding:10px;text-align:left;"">#</th>
                                <th style=""border:1px solid #d1d5db;padding:10px;text-align:left;"">Color</th>
                                <th style=""border:1px solid #d1d5db;padding:10px;text-align:left;"">Video ID</th>
                                <th style=""border:1px solid #d1d5db;padding:10px;text-align:left;"">Actual %</th>
                                <th style=""border:1px solid #d1d5db;padding:10px;text-align:left;"">Student %</th>
                                <th style=""border:1px solid #d1d5db;padding:10px;text-align:left;"">Deviation</th>
                            </tr>
                        </thead>
                        <tbody>
                            {rows}
                        </tbody>
                    </table>
                    <div style=""margin-top:16px;font-size:12px;color:#6b7280;"">Readings with deviation greater than 3 are highlighted in red per the EPA 15% rule.</div>
                    <div style=""margin-top:12px;font-size:12px;color:#6b7280;"">Screenshot attached separately.</div>
                </div>
            </body>
        </html>";
    }

    private string HtmlEncode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "")
            .Replace("\n", "\\n");
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
                    ""type"": ""text/html"",
                    ""value"": ""{EscapeJson(message)}""
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