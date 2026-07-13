using UnityEngine;

public class MangerBlackPractice : MonoBehaviour
{
    public GameObject WhiteButton;
   
    public GameObject TestPane;
    public SimpleVideoPlayer simpleVideoPlayer;
    public ManagerTesting managerTesting;
    public GameObject TestingCompletePanl;
    public GameObject RemarksPanel;

    public GameObject blackBegainPracticPanl;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoToblackTutorial()
    {
        WhiteButton.SetActive(false);
        blackBegainPracticPanl.SetActive(true);
        TestPane.SetActive(false);
        simpleVideoPlayer.playVideoURL(0);
        WarmBlackPracticePreload();
        TestingCompletePanl.SetActive(false);
        RemarksPanel.SetActive(false);
    }

    private void WarmBlackPracticePreload()
    {
        ManagerTesting manager = managerTesting;
        if (manager == null)
        {
            manager = FindFirstObjectByType<ManagerTesting>(FindObjectsInactive.Include);
        }

        if (manager != null)
        {
            manager.WarmFirstQuestionPreload(ManagerTesting.TestType.blackPractice);
        }
    }
}
