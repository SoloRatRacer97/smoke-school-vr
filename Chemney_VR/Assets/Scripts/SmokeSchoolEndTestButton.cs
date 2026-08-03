using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class SmokeSchoolEndTestButton : MonoBehaviour
{
    [SerializeField] private ManagerTesting manager;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void HandleClick()
    {
        if (manager != null)
        {
            manager.OnEndTestButtonClicked();
        }
    }
}
