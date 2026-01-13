using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SignatureRequired : MonoBehaviour
{
    [Header("References")]
    public InputField inputField;
    public TextMeshProUGUI errorText;
    public GameObject nextPanel;
    public GameObject currentPanel;
    public GameObject fakeNextButton; // 🔹 assign your fake button here

    [Header("Optional Actions")]
    public UnityEngine.Events.UnityEvent onValidSubmit;

    private Button fakeBtn;

    private void Start()
    {
        if (errorText) errorText.gameObject.SetActive(false);
        if (inputField) inputField.onValueChanged.AddListener(OnInputChanged);

        // Attach click event to fake button if it has one
        if (fakeNextButton)
        {
            fakeBtn = fakeNextButton.GetComponent<Button>();
            if (fakeBtn != null)
                fakeBtn.onClick.AddListener(OnFakeNextClicked);
        }

        UpdateFakeButton();
    }

    private void OnInputChanged(string text)
    {
        UpdateFakeButton();
    }

    private void UpdateFakeButton()
    {
        bool isEmpty = string.IsNullOrWhiteSpace(inputField.text);
        if (fakeNextButton)
            fakeNextButton.SetActive(isEmpty);
    }

    private void OnFakeNextClicked()
    {
        // Show "required" message when fake button is clicked
        if (errorText)
        {
            errorText.text = "This field is required.";
            errorText.gameObject.SetActive(true);
        }
    }

    public void OnNextButtonClicked()
    {
        string input = inputField.text.Trim();

        if (string.IsNullOrEmpty(input))
        {
            // ❌ Invalid: show error and keep fake button visible
            if (errorText)
            {
                errorText.text = "This field is required.";
                errorText.gameObject.SetActive(true);
            }
            UpdateFakeButton();
            return;
        }

        // ✅ Valid: hide error and move forward
        if (errorText) errorText.gameObject.SetActive(false);
        if (fakeNextButton) fakeNextButton.SetActive(false);

        if (nextPanel) nextPanel.SetActive(true);
        if (currentPanel) currentPanel.SetActive(false);

        onValidSubmit?.Invoke();
    }
}
