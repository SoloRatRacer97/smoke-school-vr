using System;
using UnityEngine;
public class EmailTrigger : MonoBehaviour
{
    public ScreenshotSender emailSender;
    public void OnSendEmail()
    {
        Debug.Log("Email Sent");
        string subject = "WebGL Unity Test";
        string body = "This is a test email sent from Unity WebGL.";
        string ending = "Your signature Here";
        emailSender.CaptureAndSend();

        Debug.Log("Email Has been sent");
    }

    // public void screenshot()
    // {
    //     ScreenCapture.CaptureScreenshot("Img_001" + DateTime.Now.ToString("yyyy-mm-dd-hh-mm-ss") + ".png" , 4);
    // }
}