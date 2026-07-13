using UnityEngine;

public class ManageWhitePracticeTest : MonoBehaviour
{
    public GameObject WhiteButton;
    public GameObject BegainPracticPanl;
    public GameObject TestPane;
    public SimpleVideoPlayer simpleVideoPlayer;
    public ManagerTesting managerTesting;
    public GameObject TestingCompletePanl;
    public GameObject RemarksPanel;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GoToWhiteTutorial()
    {
        WhiteButton.SetActive(false);
        BegainPracticPanl.SetActive(true);
        TestPane.SetActive(false);
        simpleVideoPlayer.playVideoURL(0);
        WarmWhitePracticePreload();
        TestingCompletePanl.SetActive(false);
        RemarksPanel.SetActive(false);
    }

    private void WarmWhitePracticePreload()
    {
        ManagerTesting manager = managerTesting;
        if (manager == null)
        {
            manager = FindFirstObjectByType<ManagerTesting>(FindObjectsInactive.Include);
        }

        if (manager != null)
        {
            manager.WarmFirstQuestionPreload(ManagerTesting.TestType.whitePractice);
        }
    }
}


    
