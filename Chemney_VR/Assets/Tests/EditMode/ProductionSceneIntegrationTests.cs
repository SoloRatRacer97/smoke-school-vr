using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmokeSchool.Tests
{
    public class ProductionSceneIntegrationTests
    {
        private const string ScenePath = "Assets/Scenes/ChimneyScene.unity";
        private Scene scene;
        private SceneSetup[] previousSetup;

        [SetUp]
        public void OpenProductionScene()
        {
            previousSetup = EditorSceneManager.GetSceneManagerSetup();
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [TearDown]
        public void CloseProductionScene()
        {
            if (previousSetup != null && previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void BuildSettings_EnableOnlyTheProductionScene()
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes.Where(entry => entry.enabled).ToArray();
            Assert.That(enabledScenes, Has.Length.EqualTo(1));
            Assert.That(enabledScenes[0].path, Is.EqualTo(ScenePath));
        }

        [Test]
        public void Scene_HasOneFullyWiredManagerUsingTheProductionCatalog()
        {
            MonoBehaviour[] managers = SceneBehaviours().Where(component => component.GetType().Name == "ManagerTesting").ToArray();
            Assert.That(managers, Has.Length.EqualTo(1));

            MonoBehaviour manager = managers[0];
            SerializedObject serialized = new SerializedObject(manager);
            Assert.That(AssetDatabase.GetAssetPath(serialized.FindProperty("videoURLData").objectReferenceValue),
                Is.EqualTo("Assets/Scripts/SmokeVideoURLData.asset"));
            AssertReference(serialized, "videoPlayer");
            AssertReference(serialized, "loadingImage");
            AssertReference(serialized, "TestingCompletePannel");
            AssertReference(serialized, "RemarksPannel");

            AssertObjectArray(serialized, "btn_points", 21);
            AssertObjectArray(serialized, "btn_questions", 25);
            Assert.That(manager.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(((RectTransform)manager.transform).anchoredPosition.y, Is.EqualTo(-100f).Within(0.001f));
        }

        [Test]
        public void Scene_TestLayoutHasNoConflictingGridAndExpectedCardinality()
        {
            Transform manager = SceneBehaviours().Single(component => component.GetType().Name == "ManagerTesting").transform;
            Transform answers = FindChild(manager, "Points Selection Buttons");
            Transform questions = FindChild(manager, "QuestionsHolder");
            Transform readings = FindChild(manager, "ReadingNumbers");

            Assert.That(answers, Is.Not.Null);
            Assert.That(answers.childCount, Is.EqualTo(21));
            HorizontalLayoutGroup answerLayout = answers.GetComponent<HorizontalLayoutGroup>();
            Assert.That(answerLayout, Is.Not.Null);
            Assert.That(answerLayout.enabled, Is.True, "The original one-row answer layout must remain enabled.");
            Assert.That(answers.GetComponent<GridLayoutGroup>(), Is.Null,
                "A GridLayoutGroup here conflicts with the serialized HorizontalLayoutGroup and crashes Start Test activation.");
            Assert.That(Enumerable.Range(0, answers.childCount)
                .Select(index => ((RectTransform)answers.GetChild(index)).sizeDelta.x)
                .Max(), Is.LessThan(60f), "The 0-100 answer buttons must retain their original width.");
            Assert.That(questions, Is.Not.Null);
            Assert.That(questions.childCount, Is.EqualTo(25));
            Assert.That(readings, Is.Not.Null);
            Assert.That(readings.childCount, Is.EqualTo(25));
        }

        [Test]
        public void Scene_TestVideoAndOverlaysMatchTheFullTutorialFootprint()
        {
            MonoBehaviour managerComponent = SceneBehaviours().Single(component => component.GetType().Name == "ManagerTesting");
            Transform manager = managerComponent.transform;
            RectTransform practicePanel = (RectTransform)FindChild(manager, "Practice Panel");
            RectTransform videoImage = (RectTransform)FindChild(practicePanel, "Videoplayer");
            RectTransform indicatorOverlay = (RectTransform)FindChild(practicePanel, "Testing Video Indicators Overlay");
            RectTransform questionNumber = (RectTransform)FindChild(indicatorOverlay, "Question Number");
            RectTransform testType = (RectTransform)FindChild(indicatorOverlay, "Test Type");
            Canvas videoOverlayCanvas = indicatorOverlay.GetComponent<Canvas>();
            SerializedObject managerData = new SerializedObject(managerComponent);
            GameObject remarksPanel = (GameObject)managerData.FindProperty("RemarksPannel").objectReferenceValue;
            GameObject testingCompletePanel = (GameObject)managerData.FindProperty("TestingCompletePannel").objectReferenceValue;
            GameObject whiteTestButton = (GameObject)managerData.FindProperty("WhiteTestButton").objectReferenceValue;
            GameObject blackPracticeButton = (GameObject)managerData.FindProperty("BlackPracticeButton").objectReferenceValue;
            GameObject blackTestButton = (GameObject)managerData.FindProperty("BlackTestButton").objectReferenceValue;
            GameObject submissionButton = (GameObject)managerData.FindProperty("SubmissionButton").objectReferenceValue;
            RectTransform returnHome = (RectTransform)FindSceneObject("Shared Return to Home Button").transform;
            Button openResultsButton = (Button)managerData.FindProperty("openresultPannelButton").objectReferenceValue;
            Button skipButton = (Button)managerData.FindProperty("btn_SkipPracticeTest").objectReferenceValue;
            Button scratchButton = (Button)managerData.FindProperty("btn_Scratch").objectReferenceValue;

            Assert.That(manager.GetComponent<Image>().color.a, Is.EqualTo(0f));
            Assert.That(practicePanel.GetComponent<Image>().color.a, Is.EqualTo(0f));
            Assert.That(practicePanel.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(practicePanel.anchorMin.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(practicePanel.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(practicePanel.anchorMax.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(practicePanel.anchoredPosition.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(practicePanel.anchoredPosition.y, Is.EqualTo(300f).Within(0.001f));
            Assert.That(practicePanel.sizeDelta.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(practicePanel.sizeDelta.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(videoImage.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(videoImage.anchorMin.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(videoImage.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(videoImage.anchorMax.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(videoImage.GetComponent<Canvas>(), Is.Null,
                "The direct/preloaded video target must not contain a nested Canvas.");
            Assert.That(indicatorOverlay.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(indicatorOverlay.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(indicatorOverlay.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(indicatorOverlay.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(indicatorOverlay.localPosition.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(indicatorOverlay.GetSiblingIndex(), Is.GreaterThan(videoImage.GetSiblingIndex()));
            Assert.That(videoOverlayCanvas, Is.Not.Null);
            Assert.That(videoOverlayCanvas.overrideSorting, Is.True);
            Assert.That(videoOverlayCanvas.sortingOrder, Is.EqualTo(10));
            Assert.That(questionNumber.gameObject.activeSelf, Is.True);
            Assert.That(questionNumber.anchorMin.x, Is.EqualTo(0.010876359f).Within(0.001f));
            Assert.That(questionNumber.anchorMin.y, Is.EqualTo(0.9293039f).Within(0.001f));
            Assert.That(questionNumber.anchorMax.x, Is.EqualTo(0.1703963f).Within(0.001f));
            Assert.That(questionNumber.anchorMax.y, Is.EqualTo(0.9836855f).Within(0.001f));
            Assert.That(questionNumber.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(questionNumber.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(questionNumber.localPosition.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(testType.gameObject.activeSelf, Is.True);
            Assert.That(testType.anchorMin.x, Is.EqualTo(0.8296037f).Within(0.001f));
            Assert.That(testType.anchorMin.y, Is.EqualTo(0.9293039f).Within(0.001f));
            Assert.That(testType.anchorMax.x, Is.EqualTo(0.98912364f).Within(0.001f));
            Assert.That(testType.anchorMax.y, Is.EqualTo(0.9836855f).Within(0.001f));
            Assert.That(testType.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(testType.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(testType.localPosition.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(questionNumber.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(testType.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(((RectTransform)remarksPanel.transform).anchoredPosition.y, Is.EqualTo(165f).Within(0.001f));
            Assert.That(((RectTransform)testingCompletePanel.transform).anchoredPosition.y, Is.EqualTo(165f).Within(0.001f));
            AssertTransitionButton(whiteTestButton, 0.07944743f, 0.32f, 300f, -265f);
            AssertTransitionButton(blackPracticeButton, 0.07944743f, 0.32f, 300f, -265f);
            AssertTransitionButton(blackTestButton, 0.07944743f, 0.32f, 300f, -265f);
            AssertTransitionButton(submissionButton, 0.37972373f, 0.6202763f, 300f, -265f);
            Assert.That((((RectTransform)submissionButton.transform).anchorMin.x +
                         ((RectTransform)submissionButton.transform).anchorMax.x) * 0.5f,
                Is.EqualTo(0.5f).Within(0.0001f));
            AssertTransitionButton(openResultsButton.gameObject, 0.35f, 0.49501812f, 200f);
            Assert.That(skipButton.gameObject.activeSelf, Is.True);
            AssertSameVerticalPlacement(skipButton, scratchButton);

            manager.gameObject.SetActive(true);
            testingCompletePanel.SetActive(true);
            whiteTestButton.SetActive(true);
            submissionButton.SetActive(true);
            skipButton.gameObject.SetActive(true);
            returnHome.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            Vector3 skipCenter = manager.InverseTransformPoint(GetRectCenter((RectTransform)skipButton.transform));
            Vector3 continueCenter = manager.InverseTransformPoint(GetRectCenter((RectTransform)whiteTestButton.transform));
            Assert.That(continueCenter.y, Is.EqualTo(skipCenter.y).Within(0.01f));
            Assert.That(continueCenter.z, Is.EqualTo(skipCenter.z).Within(0.01f));
            foreach (GameObject continuation in new[] { blackPracticeButton, blackTestButton, submissionButton })
            {
                Vector3 center = manager.InverseTransformPoint(GetRectCenter((RectTransform)continuation.transform));
                Assert.That(center.y, Is.EqualTo(continueCenter.y).Within(0.01f), continuation.name);
                Assert.That(center.z, Is.EqualTo(continueCenter.z).Within(0.01f), continuation.name);
            }
            Vector3 signatureCenter = manager.InverseTransformPoint(GetRectCenter((RectTransform)submissionButton.transform));
            Vector3 returnHomeCenter = manager.InverseTransformPoint(GetRectCenter(returnHome));
            Assert.That(signatureCenter.x, Is.EqualTo(returnHomeCenter.x).Within(0.01f));
        }

        [Test]
        public void Manager_AllPreparedVideosUseTheSameDirectDisplayPath()
        {
            string managerSource = File.ReadAllText(Path.Combine(Application.dataPath, "ManagerTesting.cs"));

            Assert.That(managerSource, Does.Match(
                @"private bool TryUsePreparedVideo[\s\S]*SetActivePlaybackPlayer\(preparedPlayer\);[\s\S]*preparedPlayer\.Play\(\);"),
                "Prepared White and Black videos must enter playback through the shared active-player path.");
            Assert.That(managerSource, Does.Match(
                @"private void SetActivePlaybackPlayer[\s\S]*smokeVideoDirectDisplay\.SetVideoPlayer\(activeVideoPlayer\);"),
                "Every active main or preloaded player must be bound to the direct display renderer.");
            Assert.That(managerSource, Does.Match(
                @"void StartPreloadSlot[\s\S]*slot\.player\.renderMode = VideoRenderMode\.APIOnly;[\s\S]*slot\.player\.Prepare\(\);"),
                "All preloaded videos must use the same API-only pre-render algorithm.");
            Assert.That(managerSource, Does.Match(
                @"private bool TryUsePreparedVideo[\s\S]*BeginVideoPlayback\(true\);[\s\S]*RequestSmokeVideoDirectDisplay\(\);"),
                "Prepared playback must suppress loading and display the queued texture immediately.");
            Assert.That(managerSource, Does.Match(
                @"private bool LoadQuestionVideo[\s\S]*BeginVideoPlayback\(false\);"),
                "Only videos not ready in the queue should enter the loading state.");
            Assert.That(managerSource, Does.Not.Contain("waitingForVideoStart = true;"),
                "Question navigation must not activate loading before checking the prepared queue.");
            Assert.That(managerSource, Does.Not.Match(
                @"private IEnumerator AutoAdvanceToNextQuestion\(\)[\s\S]{0,700}loadingImage\.SetActive\(true\)"));
            Assert.That(managerSource, Does.Not.Match(
                @"void OnNextButtonClicked\(\)[\s\S]{0,700}loadingImage\.SetActive\(true\)"));
            Assert.That(managerSource, Does.Not.Match(
                @"currentQuestionIndex = nextIndex;[\s\S]{0,220}loadingImage\.SetActive\(true\)"));
        }

        [Test]
        public void Manager_PreparedPlaybackNeverActivatesTheSpinner()
        {
            MonoBehaviour manager = SceneBehaviours().Single(component => component.GetType().Name == "ManagerTesting");
            GameObject loadingImage = (GameObject)new SerializedObject(manager)
                .FindProperty("loadingImage")
                .objectReferenceValue;

            loadingImage.SetActive(true);
            TestReflection.Invoke(manager, "BeginVideoPlayback", true);
            Assert.That(loadingImage.activeSelf, Is.False);
            Assert.That(TestReflection.GetField(manager, "waitingForVideoStart"), Is.False);
            Assert.That(TestReflection.GetField(manager, "suppressLoadingForPreparedVideo"), Is.True);

            TestReflection.Invoke(manager, "BeginVideoPlayback", false);
            Assert.That(loadingImage.activeSelf, Is.True);
            Assert.That(TestReflection.GetField(manager, "waitingForVideoStart"), Is.True);
            Assert.That(TestReflection.GetField(manager, "suppressLoadingForPreparedVideo"), Is.False);
        }

        [Test]
        public void Scene_LoginPanelUsesUnityAuthenticationComponent()
        {
            MonoBehaviour[] authComponents = SceneBehaviours().Where(component => component.GetType().Name == "DataInput_Fields").ToArray();
            Assert.That(authComponents, Has.Length.EqualTo(1));
            Assert.That(authComponents[0].gameObject.name, Is.EqualTo("LoginPanel"));
        }

        [Test]
        public void Scene_StartTestButtonsHavePersistentNavigationEvents()
        {
            GameObject managerPanel = SceneBehaviours().Single(component => component.GetType().Name == "ManagerTesting").gameObject;
            Button[] buttons = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .Where(button => button.gameObject.name == "Start Test Btn")
                .ToArray();

            Assert.That(buttons.Length, Is.GreaterThanOrEqualTo(4));
            foreach (Button button in buttons)
            {
                Assert.That(button.onClick.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(2),
                    $"{GetPath(button.transform)} must hide its tutorial panel and activate the shared test panel.");

                SerializedProperty calls = new SerializedObject(button).FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                int managerActivationIndex = Enumerable.Range(0, calls.arraySize)
                    .Where(index =>
                    {
                        SerializedProperty call = calls.GetArrayElementAtIndex(index);
                        return call.FindPropertyRelative("m_Target").objectReferenceValue == managerPanel &&
                               call.FindPropertyRelative("m_MethodName").stringValue == "SetActive" &&
                               call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue;
                    })
                    .DefaultIfEmpty(-1)
                    .First();
                Assert.That(managerActivationIndex, Is.GreaterThanOrEqualTo(0),
                    $"{GetPath(button.transform)} does not activate the shared test panel.");

                string[] phaseStartMethods = { "WhiteTestStart", "BlackPraticeStart", "BlackTestStart" };
                int phaseStartIndex = Enumerable.Range(0, calls.arraySize)
                    .Where(index => phaseStartMethods.Contains(
                        calls.GetArrayElementAtIndex(index).FindPropertyRelative("m_MethodName").stringValue))
                    .DefaultIfEmpty(-1)
                    .First();
                if (phaseStartIndex >= 0)
                {
                    Assert.That(managerActivationIndex, Is.LessThan(phaseStartIndex),
                        $"{GetPath(button.transform)} must activate the manager before starting phase playback and preloading.");
                }
            }
        }

        [Test]
        public void Scene_RestoresPhaseSkipAndTutorialHomeNavigation()
        {
            MonoBehaviour manager = SceneBehaviours().Single(component => component.GetType().Name == "ManagerTesting");
            Button skipButton = (Button)new SerializedObject(manager)
                .FindProperty("btn_SkipPracticeTest").objectReferenceValue;
            Assert.That(skipButton.gameObject.activeSelf, Is.True);
            Assert.That(skipButton.onClick.GetPersistentEventCount(), Is.EqualTo(0),
                "The shared Skip button is intentionally wired to OnSkipPractice at runtime.");

            string managerSource = File.ReadAllText("Assets/ManagerTesting.cs");
            Assert.That(managerSource, Does.Contain("manageWhitePracticeTest.GoToWhiteTutorial();"));
            Assert.That(managerSource, Does.Contain("mangerBlackPractice.GoToblackTutorial();"));
            Assert.That(managerSource, Does.Contain("Skip to White Smoke Test"));
            Assert.That(managerSource, Does.Contain("Skip to Black Smoke Test"));
            Assert.That(managerSource, Does.Not.Contain("Skip to Black Smoke Practice"));
            Assert.That(managerSource, Does.Not.Contain("Skip to Signature"));
            Assert.That(managerSource, Does.Contain("CertificationResultReporter.HasCompleteReadings"),
                "Skipped certification sections must remain incomplete and ineligible to pass.");
            Assert.That(managerSource, Does.Contain("SetVideoIndicatorsVisible(true);"));
            Assert.That(managerSource, Does.Contain("SetVideoIndicatorsVisible(false);"));

            GameObject welcomePanel = FindSceneObject("WelcomePanel");
            string[] tutorialPanelNames =
            {
                "Begin Practice Panel",
                "Begin Practice Panel After Practice",
                "Begin Practice PanelBlack",
                "Begin Practice PanelBlack Aftedr Practice"
            };
            foreach (string panelName in tutorialPanelNames)
            {
                GameObject panel = FindSceneObject(panelName);
                Button home = FindChild(panel.transform, "Back to Home").GetComponent<Button>();
                Button start = FindChild(panel.transform, "Start Test Btn").GetComponent<Button>();
                Assert.That(home.gameObject.activeSelf, Is.True, $"{panelName} Home button is disabled.");
                AssertBottomNavigationButton(home.gameObject, 0.07944743f, 0.30060008f);
                AssertBottomNavigationButton(start.gameObject, 0.69857436f, 0.919727f);

                SerializedProperty homeCalls = GetPersistentCalls(home);
                AssertSetActiveCall(homeCalls, welcomePanel, true);
                AssertSetActiveCall(homeCalls, panel, false);
            }
        }

        [Test]
        public void Manager_ShowsSkipOnlyDuringPracticePhases()
        {
            MonoBehaviour manager = SceneBehaviours().Single(component => component.GetType().Name == "ManagerTesting");
            Button skip = (Button)new SerializedObject(manager).FindProperty("btn_SkipPracticeTest").objectReferenceValue;
            (string phase, bool expectedVisible)[] expectations =
            {
                ("whitePractice", true),
                ("whiteTest", false),
                ("blackPractice", true),
                ("blackTest", false)
            };

            foreach ((string phase, bool expectedVisible) in expectations)
            {
                TestReflection.SetField(manager, "currenttype", TestReflection.EnumValue("ManagerTesting+TestType", phase));
                TestReflection.Invoke(manager, "SetSkipButtonActive", true);
                Assert.That(skip.gameObject.activeSelf, Is.EqualTo(expectedVisible), phase);
            }
        }

        [Test]
        public void Manager_TestCompletionShowsOnlyTheValidRouteAndReviewMessage()
        {
            MonoBehaviour manager = SceneBehaviours().Single(component => component.GetType().Name == "ManagerTesting");
            SerializedObject data = new SerializedObject(manager);
            Button skip = (Button)data.FindProperty("btn_SkipPracticeTest").objectReferenceValue;
            Button openResults = (Button)data.FindProperty("openresultPannelButton").objectReferenceValue;
            GameObject blackPractice = (GameObject)data.FindProperty("BlackPracticeButton").objectReferenceValue;
            GameObject submission = (GameObject)data.FindProperty("SubmissionButton").objectReferenceValue;
            Component message = (Component)data.FindProperty("completionReviewMessage").objectReferenceValue;

            TestReflection.SetField(manager, "currenttype", TestReflection.EnumValue("ManagerTesting+TestType", "whiteTest"));
            TestReflection.Invoke(manager, "ShowTestCompletePanel");
            Assert.That(skip.gameObject.activeSelf, Is.False);
            Assert.That(openResults.gameObject.activeSelf, Is.False);
            Assert.That(blackPractice.activeSelf, Is.True);
            Assert.That(message.gameObject.activeSelf, Is.True);
            Assert.That(new SerializedObject(message).FindProperty("m_text").stringValue, Is.EqualTo(
                "Feel free to review and change any answer before proceeding to Black Smoke Test."));

            TestReflection.SetField(manager, "currenttype", TestReflection.EnumValue("ManagerTesting+TestType", "blackTest"));
            TestReflection.Invoke(manager, "ShowTestCompletePanel");
            Assert.That(skip.gameObject.activeSelf, Is.False);
            Assert.That(openResults.gameObject.activeSelf, Is.False);
            Assert.That(submission.activeSelf, Is.True);
            Assert.That(message.gameObject.activeSelf, Is.True);
            Assert.That(new SerializedObject(message).FindProperty("m_text").stringValue, Is.EqualTo(
                "Feel free to review and change any answer before continuing to the results page."));
        }

        [Test]
        public void ResultsRetakeRoutesToTheWhitePracticeIntro()
        {
            MonoBehaviour manager = SceneBehaviours().Single(component => component.GetType().Name == "ManagerTesting");
            MonoBehaviour login = SceneBehaviours().Single(component => component.GetType().Name == "DataInput_Fields");
            SerializedObject loginData = new SerializedObject(login);
            GameObject intro = (GameObject)loginData.FindProperty("whitePracticeIntroPanel").objectReferenceValue;
            GameObject testing = (GameObject)loginData.FindProperty("testingPanel").objectReferenceValue;
            GameObject welcome = (GameObject)loginData.FindProperty("welcomePannel").objectReferenceValue;
            Assert.That(intro.name, Is.EqualTo("Begin Practice Panel"));

            intro.SetActive(false);
            testing.SetActive(true);
            welcome.SetActive(true);
            TestReflection.SetStaticField("ManagerTesting", "testRunNumber", 1);
            TestReflection.SetStaticField("ManagerTesting", "restartAtWhitePracticeIntro", false);
            TestReflection.SetStaticField("DataInput_Fields", "checkSceneReload", 0);
            TestReflection.Invoke(manager, "StartWhitePracticeRetake", 1, false);
            Assert.That(TestReflection.GetStaticField("ManagerTesting", "testRunNumber"), Is.EqualTo(2));
            Assert.That(TestReflection.GetStaticField("ManagerTesting", "restartAtWhitePracticeIntro"), Is.True);
            Assert.That(TestReflection.GetStaticField("DataInput_Fields", "checkSceneReload"), Is.EqualTo(1));
            Assert.That(TestReflection.Invoke(login, "ApplyPostReloadPanelRoute"), Is.True);
            Assert.That(intro.activeSelf, Is.True);
            Assert.That(testing.activeSelf, Is.False);
            Assert.That(welcome.activeSelf, Is.False);
            Assert.That(TestReflection.GetStaticField("ManagerTesting", "restartAtWhitePracticeIntro"), Is.False);

            Button endTest = FindSceneObject("End Test Button").GetComponent<Button>();
            SerializedProperty calls = GetPersistentCalls(endTest);
            Assert.That(calls.arraySize, Is.EqualTo(1));
            Assert.That(calls.GetArrayElementAtIndex(0).FindPropertyRelative("m_Target").objectReferenceValue,
                Is.EqualTo(manager));
            Assert.That(calls.GetArrayElementAtIndex(0).FindPropertyRelative("m_MethodName").stringValue,
                Is.EqualTo("OnEndTestButtonClicked"));

            string managerSource = File.ReadAllText("Assets/ManagerTesting.cs");
            Assert.That(managerSource, Does.Match(
                @"private IEnumerator CompleteEndTest[\s\S]{0,180}if \(!ScreenshotSender\.didPass\)[\s\S]{0,180}StartWhitePracticeRetake\(completedRunNumber, true\);[\s\S]{0,80}yield break;"));
            Assert.That(managerSource.IndexOf("StartWhitePracticeRetake(completedRunNumber, true);", System.StringComparison.Ordinal),
                Is.LessThan(managerSource.IndexOf("CertificationResultReporter.Submit(completedRunNumber)", System.StringComparison.Ordinal)));

            string loginSource = File.ReadAllText("Assets/Scripts/DataInput_Fields.cs");
            Assert.That(loginSource, Does.Contain("whitePracticeIntroPanel.SetActive(true);"));
            Assert.That(loginSource, Does.Match(
                @"whitePracticeIntroPanel\.GetComponent<SimpleVideoPlayer>\(\)[\s\S]{0,180}introPlayer\.playVideoURL\(0\)"));

            TestReflection.SetStaticField("ManagerTesting", "testRunNumber", 1);
            TestReflection.SetStaticField("ManagerTesting", "restartAtWhitePracticeIntro", false);
            TestReflection.SetStaticField("DataInput_Fields", "checkSceneReload", 0);
        }

        [Test]
        public void Scene_TestingEnvironmentHasPersistentReturnHomeButton()
        {
            GameObject manager = FindSceneObject("White Practice Test Panel");
            GameObject returnHome = FindSceneObject("Shared Return to Home Button");
            Assert.That(returnHome.transform.parent, Is.EqualTo(manager.transform));
            Assert.That(returnHome.activeSelf, Is.True);
            Assert.That(GetText(FindChild(returnHome.transform, "Text (TMP)").gameObject), Is.EqualTo("Return to Home"));

            RectTransform rect = (RectTransform)returnHome.transform;
            Assert.That(rect.anchorMin.x, Is.EqualTo(0.37f).Within(0.001f));
            Assert.That(rect.anchorMin.y, Is.EqualTo(0.095f).Within(0.001f));
            Assert.That(rect.anchorMax.x, Is.EqualTo(0.63f).Within(0.001f));
            Assert.That(rect.anchorMax.y, Is.EqualTo(0.155f).Within(0.001f));
            Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(0f, -185f)));

            Image background = returnHome.GetComponent<Image>();
            Assert.That(background.color.r, Is.EqualTo(0.14509805f).Within(0.001f));
            Assert.That(background.color.g, Is.EqualTo(0.7921569f).Within(0.001f));
            Assert.That(background.color.b, Is.EqualTo(0f).Within(0.001f));
            Assert.That(returnHome.GetComponents<MonoBehaviour>()
                .Any(component => component.GetType().Name == "SmokeSchoolReturnHome"), Is.True);
            Assert.That(returnHome.GetComponent<Button>().onClick.GetPersistentEventCount(), Is.EqualTo(0));

            string returnHomeSource = File.ReadAllText("Assets/Scripts/SmokeSchoolReturnHome.cs");
            Assert.That(returnHomeSource, Does.Contain("SmokeSchoolAppState.ResetCertificationState();"));
            Assert.That(returnHomeSource, Does.Contain("DataInput_Fields.checkSceneReload = 1;"));
            Assert.That(returnHomeSource, Does.Contain("SceneManager.LoadScene"));
        }

        [Test]
        public void Scene_HomeCardsAndVisibleCopyMatchTheirActions()
        {
            Assert.That(GetText(FindSceneObject("Emission Testing Text")), Is.EqualTo("Video Tutorials"));
            Assert.That(GetText(FindSceneObject("Videos Tutorials Text")), Is.EqualTo("Emission Testing"));

            string sceneSource = File.ReadAllText(ScenePath);
            string[] expectedCopy =
            {
                "m_text: Start Tutorial",
                "m_text: Begin Test",
                "m_text: Skip optional practice slides",
                "m_text: Password",
                "m_text: Sign In",
                "m_text: Open Results",
                "m_text: A signature is required.",
                "m_text: Continue to White Smoke Test",
                "m_text: Continue to Black Smoke Practice",
                "m_text: Continue to Black Smoke Test",
                "m_text: Continue to Signature"
            };
            foreach (string expected in expectedCopy)
            {
                Assert.That(sceneSource, Does.Contain(expected));
            }

            string[] forbiddenCopy =
            {
                "\\x03",
                "User ID",
                "User Email",
                "Open Result Pannel",
                "Signature are required",
                "Continue To",
                "Smoke Testing",
                "White smoke practice",
                "Maximum allowable Score",
                "Your Reading :",
                "We have sent a certification"
            };
            foreach (string forbidden in forbiddenCopy)
            {
                Assert.That(sceneSource, Does.Not.Contain(forbidden));
            }
        }

        [Test]
        public void Scene_PersistentButtonEventsHaveValidTargetsAndMethods()
        {
            Button[] buttons = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .ToArray();
            foreach (Button button in buttons)
            {
                SerializedProperty calls = GetPersistentCalls(button);
                for (int index = 0; index < calls.arraySize; index++)
                {
                    SerializedProperty call = calls.GetArrayElementAtIndex(index);
                    Assert.That(call.FindPropertyRelative("m_Target").objectReferenceValue, Is.Not.Null,
                        $"{GetPath(button.transform)} has a persistent event with no target.");
                    Assert.That(call.FindPropertyRelative("m_MethodName").stringValue, Is.Not.Empty,
                        $"{GetPath(button.transform)} has a persistent event with no method.");
                }
            }
        }

        [Test]
        public void Scene_TutorialViewsUseCorrectedSequenceOneDeliveries()
        {
            AssertTutorialMappings("White", 1);
            AssertTutorialMappings("Black", 2);
        }

        [Test]
        public void Scene_WhiteResultsPreviewDoesNotExposeBlackResultsOrAdvancePhase()
        {
            Button openResults = FindSceneObject("Open Result Panel Button").GetComponent<Button>();
            Button closeResults = FindSceneObject("Close Result Panel Button").GetComponent<Button>();
            Button continueToBlackPractice = FindSceneObject("Continue to Black Practice").GetComponent<Button>();
            GameObject blackHeading = FindSceneObject("Black Smoke Text");
            GameObject blackResults = FindSceneObject("Black smoke Remark Penal");
            GameObject blackIntro = FindSceneObject("ExamplePannellBlack");
            GameObject completionContent = FindSceneObject("Completaion Panel");

            SerializedProperty openCalls = GetPersistentCalls(openResults);
            AssertSetActiveCall(openCalls, blackHeading, false);
            AssertSetActiveCall(openCalls, blackResults, false);
            AssertSetActiveCall(openCalls, continueToBlackPractice.gameObject, false);
            AssertSetActiveCall(openCalls, closeResults.gameObject, true);
            AssertSetActiveCall(openCalls, completionContent, false);

            SerializedProperty closeCalls = GetPersistentCalls(closeResults);
            Assert.That(Enumerable.Range(0, closeCalls.arraySize)
                .Select(index => closeCalls.GetArrayElementAtIndex(index).FindPropertyRelative("m_MethodName").stringValue),
                Does.Not.Contain("ContinueToNextPhase"));
            AssertSetActiveCall(closeCalls, openResults.gameObject, true);
            AssertSetActiveCall(closeCalls, continueToBlackPractice.gameObject, true);
            AssertSetActiveCall(closeCalls, blackHeading, true);
            AssertSetActiveCall(closeCalls, blackResults, true);
            AssertSetActiveCall(closeCalls, completionContent, true);
            Component closeResultsText = closeResults.GetComponentsInChildren<Component>(true)
                .Single(component => component.GetType().Name == "TextMeshProUGUI");
            Assert.That(new SerializedObject(closeResultsText).FindProperty("m_text").stringValue, Is.EqualTo("Close Results"));
            RectTransform closeResultsRect = (RectTransform)closeResults.transform;
            Assert.That((closeResultsRect.anchorMin.x + closeResultsRect.anchorMax.x) * 0.5f,
                Is.EqualTo(0.5f).Within(0.001f));

            SerializedProperty continueCalls = GetPersistentCalls(continueToBlackPractice);
            AssertSetActiveCall(continueCalls, blackIntro, true);
            Assert.That(Enumerable.Range(0, continueCalls.arraySize)
                .Select(index => continueCalls.GetArrayElementAtIndex(index).FindPropertyRelative("m_MethodName").stringValue),
                Does.Not.Contain("ContinueToNextPhase"));
        }

        private void AssertTutorialMappings(string smokeType, int variant)
        {
            MonoBehaviour[] players = SceneBehaviours()
                .Where(component => component.GetType().Name == "SimpleVideoPlayer")
                .Where(component =>
                {
                    SerializedProperty urls = new SerializedObject(component).FindProperty("videoURLs");
                    return urls != null && urls.arraySize == 4 &&
                           urls.GetArrayElementAtIndex(0).stringValue.Contains("/" + smokeType);
                })
                .ToArray();
            Assert.That(players, Has.Length.EqualTo(2));

            int[] opacities = { 25, 50, 75, 100 };
            foreach (MonoBehaviour player in players)
            {
                SerializedProperty urls = new SerializedObject(player).FindProperty("videoURLs");
                Assert.That(urls, Is.Not.Null);
                Assert.That(urls.arraySize, Is.EqualTo(opacities.Length));
                for (int index = 0; index < opacities.Length; index++)
                {
                    string url = urls.GetArrayElementAtIndex(index).stringValue;
                    Assert.That(url, Does.Contain("/q_auto:best,f_mp4,vc_h264/"));
                    Assert.That(url, Does.Match($@"/{smokeType}{opacities[index]:D2}_V{variant}-0001_[A-Za-z0-9]+\.mp4$"));
                }
            }
        }

        private MonoBehaviour[] SceneBehaviours()
        {
            return Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .Where(component => component != null && component.gameObject.scene == scene)
                .ToArray();
        }

        private GameObject FindSceneObject(string objectName)
        {
            GameObject match = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == objectName)
                .Select(transform => transform.gameObject)
                .FirstOrDefault();
            Assert.That(match, Is.Not.Null, $"Missing production scene object {objectName}");
            return match;
        }

        private static SerializedProperty GetPersistentCalls(Button button)
        {
            return new SerializedObject(button).FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        }

        private static void AssertSetActiveCall(SerializedProperty calls, GameObject target, bool value)
        {
            bool found = Enumerable.Range(0, calls.arraySize)
                .Select(calls.GetArrayElementAtIndex)
                .Any(call => call.FindPropertyRelative("m_Target").objectReferenceValue == target &&
                             call.FindPropertyRelative("m_MethodName").stringValue == "SetActive" &&
                             call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue == value);
            Assert.That(found, Is.True, $"Missing {target.name}.SetActive({value}) persistent call.");
        }

        private static void AssertReference(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName}");
            Assert.That(property.objectReferenceValue, Is.Not.Null, $"ManagerTesting.{propertyName} is not assigned");
        }

        private static void AssertObjectArray(SerializedObject serialized, string propertyName, int expectedCount)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            Assert.That(property.arraySize, Is.EqualTo(expectedCount));
            for (int i = 0; i < property.arraySize; i++)
            {
                Assert.That(property.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null,
                    $"{propertyName}[{i}] is not assigned");
            }
        }

        private static void AssertTransitionButton(
            GameObject button,
            float anchorMinX,
            float anchorMaxX,
            float textWidth,
            float anchoredY = -180f)
        {
            RectTransform buttonRect = (RectTransform)button.transform;
            RectTransform buttonText = (RectTransform)FindChild(buttonRect, "Text (TMP)");
            Assert.That(buttonRect.anchorMin.x, Is.EqualTo(anchorMinX).Within(0.001f));
            Assert.That(buttonRect.anchorMin.y, Is.EqualTo(0.021441802f).Within(0.001f));
            Assert.That(buttonRect.anchorMax.x, Is.EqualTo(anchorMaxX).Within(0.001f));
            Assert.That(buttonRect.anchorMax.y, Is.EqualTo(0.07582342f).Within(0.001f));
            Assert.That(buttonRect.anchoredPosition.y, Is.EqualTo(anchoredY).Within(0.001f));
            Assert.That(buttonText.sizeDelta.x, Is.EqualTo(textWidth).Within(0.001f));
        }

        private static void AssertBottomNavigationButton(GameObject button, float anchorMinX, float anchorMaxX)
        {
            RectTransform buttonRect = (RectTransform)button.transform;
            Assert.That(buttonRect.anchorMin.x, Is.EqualTo(anchorMinX).Within(0.001f));
            Assert.That(buttonRect.anchorMin.y, Is.EqualTo(0.021441802f).Within(0.001f));
            Assert.That(buttonRect.anchorMax.x, Is.EqualTo(anchorMaxX).Within(0.001f));
            Assert.That(buttonRect.anchorMax.y, Is.EqualTo(0.07582342f).Within(0.001f));
            Assert.That(buttonRect.anchoredPosition.y, Is.EqualTo(-230f).Within(0.001f));
        }

        private static void AssertSameVerticalPlacement(Button first, Button second)
        {
            RectTransform firstRect = (RectTransform)first.transform;
            RectTransform secondRect = (RectTransform)second.transform;
            Assert.That(firstRect.anchorMin.y, Is.EqualTo(secondRect.anchorMin.y).Within(0.001f));
            Assert.That(firstRect.anchorMax.y, Is.EqualTo(secondRect.anchorMax.y).Within(0.001f));
            Assert.That(firstRect.anchoredPosition.y, Is.EqualTo(secondRect.anchoredPosition.y).Within(0.001f));
            Assert.That(firstRect.pivot.y, Is.EqualTo(secondRect.pivot.y).Within(0.001f));
        }

        private static string GetText(GameObject gameObject)
        {
            Component text = gameObject.GetComponents<Component>()
                .Single(component => component.GetType().Name == "TextMeshProUGUI");
            return new SerializedObject(text).FindProperty("m_text").stringValue;
        }

        private static Vector3 GetRectCenter(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root.name == objectName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChild(root.GetChild(i), objectName);
                if (match != null) return match;
            }
            return null;
        }

        private static string GetPath(Transform transform)
        {
            return transform.parent == null ? transform.name : GetPath(transform.parent) + "/" + transform.name;
        }
    }
}
