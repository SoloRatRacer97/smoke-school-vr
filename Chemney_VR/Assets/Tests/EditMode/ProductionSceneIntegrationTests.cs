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
            RectTransform questionNumber = (RectTransform)FindChild(videoImage, "Question Number");
            RectTransform testType = (RectTransform)FindChild(videoImage, "Test Type");
            SerializedObject managerData = new SerializedObject(managerComponent);
            GameObject remarksPanel = (GameObject)managerData.FindProperty("RemarksPannel").objectReferenceValue;
            GameObject testingCompletePanel = (GameObject)managerData.FindProperty("TestingCompletePannel").objectReferenceValue;
            GameObject whiteTestButton = (GameObject)managerData.FindProperty("WhiteTestButton").objectReferenceValue;
            GameObject blackPracticeButton = (GameObject)managerData.FindProperty("BlackPracticeButton").objectReferenceValue;
            GameObject blackTestButton = (GameObject)managerData.FindProperty("BlackTestButton").objectReferenceValue;
            GameObject submissionButton = (GameObject)managerData.FindProperty("SubmissionButton").objectReferenceValue;
            Button openResultsButton = (Button)managerData.FindProperty("openresultPannelButton").objectReferenceValue;

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
            Assert.That(questionNumber.gameObject.activeSelf, Is.True);
            Assert.That(questionNumber.anchoredPosition.x, Is.EqualTo(16.799988f).Within(0.001f));
            Assert.That(questionNumber.anchoredPosition.y, Is.EqualTo(-16.100006f).Within(0.001f));
            Assert.That(testType.gameObject.activeSelf, Is.True);
            Assert.That(testType.anchoredPosition.x, Is.EqualTo(-16f).Within(0.001f));
            Assert.That(testType.anchoredPosition.y, Is.EqualTo(-13f).Within(0.001f));
            Assert.That(((RectTransform)remarksPanel.transform).anchoredPosition.y, Is.EqualTo(165f).Within(0.001f));
            Assert.That(((RectTransform)testingCompletePanel.transform).anchoredPosition.y, Is.EqualTo(165f).Within(0.001f));
            AssertTransitionButton(whiteTestButton, 0.07944743f, 0.32f, 300f);
            AssertTransitionButton(blackPracticeButton, 0.07944743f, 0.32f, 300f);
            AssertTransitionButton(blackTestButton, 0.07944743f, 0.32f, 300f);
            AssertTransitionButton(submissionButton, 0.42430055f, 0.66485312f, 300f);
            AssertTransitionButton(openResultsButton.gameObject, 0.35f, 0.49501812f, 200f);
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

        private static void AssertTransitionButton(GameObject button, float anchorMinX, float anchorMaxX, float textWidth)
        {
            RectTransform buttonRect = (RectTransform)button.transform;
            RectTransform buttonText = (RectTransform)FindChild(buttonRect, "Text (TMP)");
            Assert.That(buttonRect.anchorMin.x, Is.EqualTo(anchorMinX).Within(0.001f));
            Assert.That(buttonRect.anchorMin.y, Is.EqualTo(0.021441802f).Within(0.001f));
            Assert.That(buttonRect.anchorMax.x, Is.EqualTo(anchorMaxX).Within(0.001f));
            Assert.That(buttonRect.anchorMax.y, Is.EqualTo(0.07582342f).Within(0.001f));
            Assert.That(buttonRect.anchoredPosition.y, Is.EqualTo(-180f).Within(0.001f));
            Assert.That(buttonText.sizeDelta.x, Is.EqualTo(textWidth).Within(0.001f));
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
