using System;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Random = UnityEngine.Random;
using UnityEngine.Events;
public class NewPracticeManager : MonoBehaviour
{

    public enum TestType { whitePractice, whiteTest, blackPractice, blackTest };
    public TestType currenttype;


    [SerializeField] VideoPlayer videoPlayer;

    //possible answer values based on index 
    private int[] answersValue = new int[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 };
    // random question values that usewr will answer initialize on start 




    [Header("QuestionsValues")]
    public int[] questionvalues_practice_white;
    public int[] questionvalues_test_white;
    public int[] questionvalues_practice_black;
    public int[] questionvalues_test_black;

    [Header("Answer Values")]
    public int[] answervalues_practice_white;
    public int[] answervalues_test_white;
    public int[] answervalues_practice_black;
    public int[] answervalues_test_black;

    [Header("Scratch Button")]

    public Button btn_Scratch;



    //Opacity Values Assigned to Questions 
    public int[] currentQuestionValues;

    public string[] videoFiles;
    // possible answers
    public Button[] btn_points;


    // questions interactable buttons 

    [Header("Questions")]
    public Button[] btn_questions;


    [Header("Current Text")]
    public TMP_Text CurrentTest_Text;

    public TMP_Text questionNmbr_text;
   
    // place for answers 
    public TMP_Text[] userSelectedValue; 
    public int currentQuestionIndex = 0;

    [Header("REMARKS PANNEL")]
    public GameObject RemarksPannel;
    public TextMeshProUGUI targetOpacityText;
    public TextMeshProUGUI yourReadingText;
    public TextMeshProUGUI resultSummaryText;


    [Header("REVIEWPHASE PANNEL")]
    public GameObject TestingCompletePannel;
    public Button Btn_Continue;
    public TMP_Text Txt_ContinueText;

    bool reviewphase = false;
    int REVIEWQUESTIONINDEX = -1;

    void Start()
    {
        currenttype = TestType.whitePractice;
        CurrentTest_Text.text = "White Smoke Practice";
        Debug.Log("Practice" + CurrentTest_Text);
    questionvalues_practice_white = new int [btn_questions.Length];
    questionvalues_test_white= new int [btn_questions.Length];
    questionvalues_practice_black= new int [btn_questions.Length];
    questionvalues_test_black= new int [btn_questions.Length];


     answervalues_practice_white= new int [btn_questions.Length];;
     answervalues_test_white= new int [btn_questions.Length];;
     answervalues_practice_black= new int [btn_questions.Length];;
     answervalues_test_black= new int [btn_questions.Length];;


        if (currenttype == TestType.whitePractice)
        {
            CurrentTest_Text.text = "White Smoke Practice";
            reviewphase = false;
            currentQuestionIndex = 0;
            currentQuestionValues = new int[questionvalues_practice_white.Length];
            Txt_ContinueText.text = "Continue To White Testing";
        }
        if (currenttype == TestType.whiteTest)
        {
            CurrentTest_Text.text = "White Smoke Test";            
            reviewphase = false;
            currentQuestionIndex=0;
             currentQuestionValues = new int[questionvalues_test_white.Length];
            Txt_ContinueText.text = "Continue To black practice";
        }
        if (currenttype == TestType.blackPractice)
        {
            CurrentTest_Text.text = "Black Smoke Practice";            
            reviewphase = false;
            currentQuestionIndex=0;
             currentQuestionValues = new int[questionvalues_practice_black.Length];
            Txt_ContinueText.text = "Continue To black Testing";
        }
           if (currenttype == TestType.blackTest)
        {
            CurrentTest_Text.text = "Black Smoke Test";
            reviewphase = false;
            currentQuestionIndex=0;
            currentQuestionValues = new int[questionvalues_test_black.Length];
            Txt_ContinueText.text = "Continue To Submition";
         }
        // disable all answers 
        DisableAnswers();
        // question buttons assign value of video 
        LoadQuestions();
       
        //Make answers 
        LoadAnswerListeners();



    }


    public void ContinueToNextPhase()
    {
        reviewphase = false;
        currentQuestionIndex = 0;
        LoadCurrentQuestion();
        TestingCompletePannel.SetActive(false);
        RemarksPannel.SetActive(false);
        foreach (TMP_Text tt in userSelectedValue)
            {
                tt.text = "";
            }

        if (currenttype == TestType.whitePractice)
        {
            Txt_ContinueText.text = "Continue To White Testing";
            currentQuestionValues = questionvalues_test_white;
            currenttype = TestType.whiteTest;
        }
        else if (currenttype == TestType.whiteTest)
        {
            Txt_ContinueText.text = "Continue To black practice";
            currentQuestionValues = questionvalues_practice_black;
            currenttype = TestType.blackPractice;
        }
        else if (currenttype == TestType.blackPractice)
        {
            Txt_ContinueText.text = "Continue To black Testing";

            currentQuestionValues = questionvalues_test_black;
            currenttype = TestType.blackTest;
        }
        else if (currenttype == TestType.blackTest)
        {
            Txt_ContinueText.text = "Continue To Submition";
            Debug.Log("OPEN RESULT PANNEL");
        }
    }


    void LoadAnswerListeners()
    {
         for (int i = 0; i < btn_points.Length; i++)
        {
            int index = i;
            btn_points[i].onClick.AddListener(new UnityAction(() => OnAnswer(index)));
        }
     }


    void LoadQuestions()
    {

 Debug.Log("Loading questions");
        // random value of possible video is assigned to the buttons 

        for (int z = 0; z < 4; z++)
        {
            for (int i = 0; i < currentQuestionValues.Length; i++)
            {
                Debug.Log("Initializing values");
                int x = Random.Range(0, answersValue.Length);
                if (z == 0) { questionvalues_practice_white[i] = answersValue[x]; }
                else if (z == 1) { questionvalues_test_white[i] = answersValue[x];}
                else if (z == 2) { questionvalues_practice_black[i] = answersValue[x];}
                else if (z == 3) { questionvalues_test_black[i] = answersValue[x];}
            }
        }
        

        if (currenttype == TestType.whitePractice)
        {
            currentQuestionValues = questionvalues_practice_white;
        }
        else if (currenttype == TestType.whiteTest)
        {
            currentQuestionValues = questionvalues_test_white;
        }
        else if (currenttype == TestType.blackPractice)
        {
            currentQuestionValues = questionvalues_practice_black;
        }
        else if (currenttype == TestType.blackTest)
        {
            currentQuestionValues = questionvalues_test_black;
        }


                

        //each question is assigned a listener 
        for (int i = 0; i < btn_questions.Length; i++)
        {
            int index = i;
            btn_questions[i].onClick.AddListener(new UnityAction(() => OnQuestion(index)));
            btn_questions[i].interactable = false;
        }
          LoadCurrentQuestion();
    }


/// <summary>
///  Main FUnCTIONS 
/// </summary>
/// <param name="i"></param>


    void OnQuestion(int i)
    {
        if (reviewphase)
        {
            TestingCompletePannel.SetActive(false);
            RemarksPannel.SetActive(false);

            REVIEWQUESTIONINDEX = i;
            Debug.Log("Question clicked " + i);
            int jvalue = currentQuestionValues[i];
            int opacityIndex = IndexOfOpacity(jvalue);
            Debug.Log("opacity index is " + opacityIndex);
            playVideoByIndex(opacityIndex);
            EnableAnswers();
        }
        else if (currentQuestionIndex == i)
        {
//            questionNmbr_text.text = currentQuestionValues[i].ToString();
            TestingCompletePannel.SetActive(false);
            RemarksPannel.SetActive(false);
            Debug.Log("Question clicked " + i);
            Debug.Log("Question Opacity Value  " + currentQuestionValues[i]);
            int jvalue = currentQuestionValues[i];
            //            userSelectedValue[i].text = questionvalues[i].ToString();
            //            Debug.Log("The value putted" + userSelectedValue[i]);

            int opacityIndex = IndexOfOpacity(jvalue);

            Debug.Log("opacity index is " + opacityIndex);
            playVideoByIndex(opacityIndex);
            EnableAnswers();
        }
        else
        {
            Debug.Log("invalid question" + currentQuestionIndex);
        }
    }

    void OnAnswer(int i)
    {

        Debug.Log("ANSWER SELECTED IS " + answersValue[i]);

        if (reviewphase)
        {
            userSelectedValue[REVIEWQUESTIONINDEX].text = "" + answersValue[i];

            if (currenttype == TestType.whitePractice) { answervalues_practice_white[REVIEWQUESTIONINDEX] = answersValue[i]; };
            if (currenttype == TestType.whiteTest) { answervalues_test_white[REVIEWQUESTIONINDEX] = answersValue[i]; }
            if (currenttype == TestType.blackPractice) { answervalues_practice_black[REVIEWQUESTIONINDEX] = answersValue[i]; }
            if (currenttype == TestType.blackTest) { answervalues_test_black[REVIEWQUESTIONINDEX] = answersValue[i]; }




            DisableAnswers();
            videoPlayer.Stop();
            OpenRemarksPannel();
            Invoke(nameof(ReOpenTestCompletePannel), 2);
        }
        else
        {
            userSelectedValue[currentQuestionIndex].text = "" + answersValue[i];

            if (currenttype == TestType.whitePractice) { answervalues_practice_white[currentQuestionIndex] = answersValue[i]; };
            if (currenttype == TestType.whiteTest) { answervalues_test_white[currentQuestionIndex] = answersValue[i]; }
            if (currenttype == TestType.blackPractice) { answervalues_practice_black[currentQuestionIndex] = answersValue[i]; }
            if (currenttype == TestType.blackTest) { answervalues_test_black[currentQuestionIndex] = answersValue[i]; }







            DisableAnswers();
            videoPlayer.Stop();

            if (currentQuestionIndex < btn_questions.Length - 1)
            {
                currentQuestionIndex++;
                if (currenttype == TestType.whitePractice || currenttype == TestType.blackPractice)
                {
                      OpenRemarksPannel();
                }
                LoadCurrentQuestion();
            }
            else
            {
                TestingCompletePannel.SetActive(true);
                Debug.Log("TestFinished ");
                reviewphase = true;
                foreach (Button X in btn_questions)
                {
                    X.interactable = true;
                }
            }

        }


       

    }







    void ReOpenTestCompletePannel()
    {
          TestingCompletePannel.SetActive(true);
    }

    public void OpenRemarksPannel()
    {
        int ogvalue = 0;
        int ansvalue = 0;
        RemarksPannel.SetActive(true);
        if (reviewphase)
        {
            ogvalue = currentQuestionValues[REVIEWQUESTIONINDEX];
            ansvalue = int.Parse(userSelectedValue[REVIEWQUESTIONINDEX].text);
         }
    else{
            ogvalue = currentQuestionValues[(currentQuestionIndex) - 1];
            ansvalue = int.Parse(userSelectedValue[(currentQuestionIndex) - 1].text);
        }
        targetOpacityText.text = "" + ogvalue;
        yourReadingText.text = "" + ansvalue;
        string remarks = "";

        if (ansvalue == ogvalue)
        {
            remarks = "Your Value was Perfect";
        }
        else if (ansvalue > ogvalue) //high
        {
            int x = ansvalue - ogvalue;
            remarks = "Your Value was " + x + "% too high";
        }
        else if (ansvalue < ogvalue)
        {
            int x = ogvalue - ansvalue;
            remarks = "Your Value was " + x + "% too low";

        }
        resultSummaryText.text = "" + remarks;
    }


    private void LoadCurrentQuestion()
    {



        for (int i = 0; i < btn_questions.Length; i++)
        {
            btn_questions[i].interactable = false;
        }
        btn_questions[currentQuestionIndex].interactable = true;
    }

    void playVideoByIndex(int index)
    {
        string filename = videoFiles[index].ToString();

        string path = Path.Combine(Application.streamingAssetsPath, filename);
#if UNITY_WEBGL
        string url = path;
#else
        string url = "file://" + path;
#endif

        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        videoPlayer.url = ""; // Force refresh
        videoPlayer.url = url;
        videoPlayer.isLooping = true;
        videoPlayer.Play();
    }



    public void DisableAnswers()
    {
        foreach (Button y in btn_points)
        {
            y.interactable = false;
        }
    }

    public void EnableAnswers()
    {
        foreach (Button y in btn_points)
        {
            y.interactable = true;
        }

    }



// help[er function to fins the index in terms of 0 15 20  to 0 1 2
    public int IndexOfOpacity(int x)
    {
        int index = Array.IndexOf(answersValue, x);
        if (index == -1)
        {
            // Value not found - you can return -1 or throw an exception
            Debug.LogWarning($"Value {x} not found in answersValue array");
            return -1;
        }
        return index;
    }

}
