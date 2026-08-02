using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(Button))]
public sealed class SmokeSchoolReturnHome : MonoBehaviour
{
    private Button returnButton;

    private void Awake()
    {
        returnButton = GetComponent<Button>();
        returnButton.onClick.AddListener(ReturnToHome);
    }

    private void OnDestroy()
    {
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnToHome);
        }
    }

    public void ReturnToHome()
    {
        VideoPlayer[] players = FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (VideoPlayer player in players)
        {
            player.Stop();
        }

        SmokeSchoolAppState.ResetCertificationState();
        DataInput_Fields.checkSceneReload = 1;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
