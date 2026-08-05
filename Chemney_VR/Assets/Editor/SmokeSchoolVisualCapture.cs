using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SmokeSchoolVisualCapture
{
    public static void EnsureComingSoonVrAnnouncement()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/ChimneyScene.unity", OpenSceneMode.Single);
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>()
            .Where(item => item != null && item.gameObject.scene == scene)
            .ToArray();
        Transform root = RequireTransform(transforms, "WelcomePanel");
        Transform existing = root.Find("Coming Soon VR Announcement");
        GameObject announcement;
        if (existing == null)
        {
            announcement = new GameObject(
                "Coming Soon VR Announcement",
                typeof(RectTransform),
                typeof(Image));
            announcement.layer = root.gameObject.layer;
            announcement.transform.SetParent(root, false);
        }
        else
        {
            announcement = existing.gameObject;
        }
        announcement.transform.SetAsLastSibling();

        RectTransform announcementRect = (RectTransform)announcement.transform;
        announcementRect.anchorMin = new Vector2(0.25f, 0f);
        announcementRect.anchorMax = new Vector2(0.75f, 0.22f);
        announcementRect.anchoredPosition = Vector2.zero;
        announcementRect.sizeDelta = Vector2.zero;
        announcementRect.localPosition = new Vector3(
            announcementRect.localPosition.x,
            announcementRect.localPosition.y,
            0f);

        TMP_Text visibleButtonLabel = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .First(item => item != null && item.gameObject.scene == scene && item.text == "Begin Test");
        Image visibleButtonBackground = visibleButtonLabel.transform.parent.GetComponent<Image>();
        Image background = announcement.GetComponent<Image>();
        background.sprite = visibleButtonBackground.sprite;
        background.type = visibleButtonBackground.type;
        background.pixelsPerUnitMultiplier = visibleButtonBackground.pixelsPerUnitMultiplier;
        background.color = new Color(0.95686275f, 0.7254902f, 0.25882354f, 0.7f);
        background.raycastTarget = false;

        TMP_Text reference = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .First(item => item != null && item.gameObject.scene == scene && item.text == "Coming soon");
        Transform titleTransform = announcement.transform.Find("Title");
        TMP_Text title = titleTransform != null
            ? titleTransform.GetComponent<TMP_Text>()
            : UnityEngine.Object.Instantiate(reference, announcement.transform);
        title.gameObject.name = "Title";
        RectTransform titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = new Vector2(0.08f, 0.64f);
        titleRect.anchorMax = new Vector2(0.92f, 0.92f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = Vector2.zero;
        title.text = "Coming Soon";
        title.fontSize = 40f;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.16078432f, 0.13725491f, 0.16078432f, 1f);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        title.gameObject.SetActive(true);

        Transform bodyTransform = announcement.transform.Find("Body");
        TMP_Text body = bodyTransform != null
            ? bodyTransform.GetComponent<TMP_Text>()
            : UnityEngine.Object.Instantiate(reference, announcement.transform);
        body.gameObject.name = "Body";
        RectTransform bodyRect = (RectTransform)body.transform;
        bodyRect.anchorMin = new Vector2(0.10f, 0.30f);
        bodyRect.anchorMax = new Vector2(0.90f, 0.62f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = Vector2.zero;
        body.text = "Our VR testing environment will be live soon. Return back here shortly.";
        body.fontSize = 26f;
        body.fontStyle = FontStyles.Normal;
        body.color = title.color;
        body.alignment = TextAlignmentOptions.Center;
        body.enableWordWrapping = true;
        body.raycastTarget = false;
        body.gameObject.SetActive(true);
        announcement.SetActive(true);

        TMP_Text footer = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .First(item => item != null && item.gameObject.scene == scene && item.text.Contains("smokeschoolvr.com"));
        footer.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void EnsureEndTestButtonBridge()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/ChimneyScene.unity", OpenSceneMode.Single);
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>()
            .Where(item => item != null && item.gameObject.scene == scene)
            .ToArray();
        MonoBehaviour manager = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .First(item => item != null && item.gameObject.scene == scene && item.GetType().Name == "ManagerTesting");
        GameObject endTestButton = RequireTransform(transforms, "End Test Button").gameObject;
        Button button = endTestButton.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();

        SmokeSchoolEndTestButton bridge = endTestButton.GetComponent<SmokeSchoolEndTestButton>();
        if (bridge == null)
        {
            bridge = endTestButton.AddComponent<SmokeSchoolEndTestButton>();
        }

        SerializedObject bridgeData = new SerializedObject(bridge);
        bridgeData.FindProperty("manager").objectReferenceValue = manager;
        bridgeData.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void EnsureTestingCompletionReviewMessage()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/ChimneyScene.unity", OpenSceneMode.Single);
        MonoBehaviour manager = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .First(item => item != null && item.gameObject.scene == scene && item.GetType().Name == "ManagerTesting");
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>()
            .Where(item => item != null && item.gameObject.scene == scene)
            .ToArray();
        Transform completion = RequireTransform(transforms, "TestingCompletePannel");
        TMP_Text heading = RequireChild(completion, "White Testing Complete").GetComponent<TMP_Text>();
        Transform messageTransform = heading.transform.parent.Find("Completion Review Message");
        TMP_Text message;
        if (messageTransform == null)
        {
            message = UnityEngine.Object.Instantiate(heading, heading.transform.parent);
            message.gameObject.name = "Completion Review Message";
        }
        else
        {
            message = messageTransform.GetComponent<TMP_Text>();
        }

        RectTransform rect = (RectTransform)message.transform;
        rect.anchorMin = new Vector2(0.08f, 0.40f);
        rect.anchorMax = new Vector2(0.92f, 0.57f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        message.fontSize = 24f;
        message.fontStyle = FontStyles.Normal;
        message.alignment = TextAlignmentOptions.Center;
        message.enableWordWrapping = true;
        message.text = string.Empty;
        message.gameObject.SetActive(false);

        SerializedObject managerData = new SerializedObject(manager);
        managerData.FindProperty("completionReviewMessage").objectReferenceValue = message;
        managerData.ApplyModifiedPropertiesWithoutUndo();

        MonoBehaviour login = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .First(item => item != null && item.gameObject.scene == scene && item.GetType().Name == "DataInput_Fields");
        SerializedObject loginData = new SerializedObject(login);
        loginData.FindProperty("whitePracticeIntroPanel").objectReferenceValue = RequireTransform(transforms, "Begin Practice Panel").gameObject;
        loginData.FindProperty("testingPanel").objectReferenceValue = RequireTransform(transforms, "White Practice Test Panel").gameObject;
        loginData.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void EnsureTestingVideoOverlayCanvas()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/ChimneyScene.unity", OpenSceneMode.Single);
        Transform manager = Resources.FindObjectsOfTypeAll<Transform>()
            .First(item => item != null && item.gameObject.scene == scene && item.name == "White Practice Test Panel");
        Transform practice = RequireChild(manager, "Practice Panel");
        Transform video = RequireChild(practice, "Videoplayer");
        Canvas videoCanvas = video.GetComponent<Canvas>();
        if (videoCanvas != null)
        {
            UnityEngine.Object.DestroyImmediate(videoCanvas);
        }

        Transform overlay = practice.Find("Testing Video Indicators Overlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject(
                "Testing Video Indicators Overlay",
                typeof(RectTransform),
                typeof(Canvas));
            overlayObject.layer = video.gameObject.layer;
            overlay = overlayObject.transform;
            overlay.SetParent(practice, false);
        }

        RectTransform overlayRect = (RectTransform)overlay;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = Vector2.zero;
        overlayRect.localPosition = new Vector3(overlayRect.localPosition.x, overlayRect.localPosition.y, 0f);
        overlay.SetSiblingIndex(video.GetSiblingIndex() + 1);

        RequireChild(video, "Question Number").SetParent(overlay, false);
        RequireChild(video, "Test Type").SetParent(overlay, false);

        Canvas overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 10;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void RestoreTestingReturnHomeButton()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/ChimneyScene.unity", OpenSceneMode.Single);
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>()
            .Where(item => item != null && item.gameObject.scene == scene)
            .ToArray();
        if (transforms.Any(item => item.name == "Shared Return to Home Button"))
        {
            return;
        }

        Transform scratch = RequireTransform(transforms, "Scratch_Btn");
        GameObject returnHome = UnityEngine.Object.Instantiate(scratch.gameObject, scratch.parent);
        returnHome.name = "Shared Return to Home Button";

        RectTransform rect = (RectTransform)returnHome.transform;
        rect.anchorMin = new Vector2(0.37f, 0.095f);
        rect.anchorMax = new Vector2(0.63f, 0.155f);
        rect.anchoredPosition = new Vector2(0f, -185f);
        rect.sizeDelta = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        Button button = returnHome.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        returnHome.GetComponent<Image>().color = new Color(0.14509805f, 0.7921569f, 0f, 1f);
        returnHome.GetComponentInChildren<TMP_Text>(true).text = "Return to Home";
        returnHome.AddComponent<SmokeSchoolReturnHome>();

        Transform mockup = returnHome.transform.Find("Mockup");
        if (mockup != null)
        {
            UnityEngine.Object.DestroyImmediate(mockup.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void CaptureWhitePracticeCompletion()
    {
        CaptureCompletion("White Smoke Practice Complete", "Continue to WhiteTest", "Continue to White Smoke Test");
    }

    public static void CaptureWhiteTestCompletion()
    {
        CaptureCompletion("White Smoke Test Complete", "Continue to Black Practice", "Continue to Black Smoke Practice", "Open Result Panel Button");
    }

    public static void CaptureBlackPracticeCompletion()
    {
        CaptureCompletion("Black Smoke Practice Complete", "Continue to BlackTest", "Continue to Black Smoke Test");
    }

    public static void CaptureBlackTestCompletion()
    {
        CaptureCompletion("Black Smoke Test Complete", "Continue to Submission", "Continue to Signature");
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

    private static Transform RequireChild(Transform root, string objectName)
    {
        Transform match = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == objectName);
        if (match == null)
        {
            throw new InvalidOperationException("Missing child object: " + objectName);
        }
        return match;
    }

}
