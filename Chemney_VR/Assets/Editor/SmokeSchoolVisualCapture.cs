using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SmokeSchoolVisualCapture
{
    public static void CaptureWhitePracticeCompletion()
    {
        CaptureCompletion("White Smoke Practice Complete", "Continue to WhiteTest", "Continue To White Testing");
    }

    public static void CaptureWhiteTestCompletion()
    {
        CaptureCompletion("White Smoke Testing Complete", "Continue to Black Practice", "Continue To Black Practice", "Open Result Panel Button");
    }

    public static void CaptureBlackPracticeCompletion()
    {
        CaptureCompletion("Black Smoke Practice Complete", "Continue to BlackTest", "Continue To Black Testing");
    }

    public static void CaptureBlackTestCompletion()
    {
        CaptureCompletion("Black Smoke Testing Complete", "Continue to Submission", "Continue To Submission");
    }

    private static void CaptureCompletion(string heading, string primaryButtonName, string primaryButtonText, params string[] additionalButtons)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/ChimneyScene.unity", OpenSceneMode.Single);
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>()
            .Where(item => item != null && item.gameObject.scene == scene)
            .ToArray();

        Transform manager = RequireTransform(transforms, "White Practice Test Panel");
        Transform completion = RequireTransform(transforms, "TestingCompletePannel");
        Canvas canvas = Resources.FindObjectsOfTypeAll<Canvas>()
            .First(item => item != null && item.gameObject.scene == scene && item.transform == manager.parent);

        for (int index = 0; index < canvas.transform.childCount; index++)
        {
            canvas.transform.GetChild(index).gameObject.SetActive(false);
        }

        manager.gameObject.SetActive(true);
        completion.gameObject.SetActive(true);
        RequireTransform(transforms, "Remarks_Panel").gameObject.SetActive(false);
        RequireTransform(transforms, "Scratch_Btn").gameObject.SetActive(false);

        string[] transitionButtons =
        {
            "Continue to WhiteTest",
            "Continue to Black Practice",
            "Open Result Panel Button",
            "Continue to BlackTest",
            "Continue to Submission"
        };
        foreach (string buttonName in transitionButtons)
        {
            RequireTransform(transforms, buttonName).gameObject.SetActive(false);
        }
        Transform primaryButton = RequireTransform(transforms, primaryButtonName);
        primaryButton.gameObject.SetActive(true);
        foreach (string buttonName in additionalButtons)
        {
            RequireTransform(transforms, buttonName).gameObject.SetActive(true);
        }

        TMP_Text completionText = completion.GetComponentsInChildren<TMP_Text>(true)
            .First(item => item.gameObject.name == "White Testing Complete");
        completionText.text = heading;
        primaryButton.GetComponentsInChildren<TMP_Text>(true).First().text = primaryButtonText;

        Canvas.ForceUpdateCanvases();
        Camera camera = Resources.FindObjectsOfTypeAll<Camera>()
            .First(item => item != null && item.gameObject.scene == scene && item.isActiveAndEnabled && item.targetTexture == null);

        const int width = 1440;
        const int height = 1000;
        RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;

        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();

        string outputPath = Environment.GetEnvironmentVariable("SMOKE_CAPTURE_PATH");
        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../white-practice-completion.png"));
        }
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllBytes(outputPath, image.EncodeToPNG());

        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("Smoke School visual capture written to " + outputPath);
    }

    private static Transform RequireTransform(Transform[] transforms, string objectName)
    {
        Transform match = transforms.FirstOrDefault(item => item.name == objectName);
        if (match == null)
        {
            throw new InvalidOperationException("Missing scene object: " + objectName);
        }
        return match;
    }
}
