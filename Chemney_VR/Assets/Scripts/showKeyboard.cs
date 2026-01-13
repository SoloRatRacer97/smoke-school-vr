using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using UnityEngine.EventSystems;
using Unity.Collections;

public class showKeyboard : MonoBehaviour
{
    private TMP_InputField InputField;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InputField = GetComponent<TMP_InputField>();
        InputField.onSelect.AddListener(x=> openKeyboard());
    }
    public void openKeyboard()
    {
        NonNativeKeyboard.Instance.InputField = InputField;
        NonNativeKeyboard.Instance.PresentKeyboard(InputField.text);
    }
}
