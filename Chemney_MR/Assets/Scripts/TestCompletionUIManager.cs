using UnityEngine;

public class TestCompletionUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject testCompletePanel;
    public GameObject pointsTable;
    public GameObject pointsButton;
    public GameObject remarksPanel;

    [Header("Other UI Elements to Hide")]
    public GameObject[] elementsToHide;

    public void ShowTestCompletePanel()
    {
        // Enable test complete and related elements
        testCompletePanel.SetActive(true);
        pointsTable.SetActive(true);
        pointsButton.SetActive(true);

        // Hide remarks panel
        if (remarksPanel != null)
            remarksPanel.SetActive(false);

        // Hide other UI elements
        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}
