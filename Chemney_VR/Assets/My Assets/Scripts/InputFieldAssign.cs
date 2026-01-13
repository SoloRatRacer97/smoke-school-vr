using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.UI;

public class InputFieldAssign : MonoBehaviour
{
    //[SerializeField] OVRVirtualKeyboardInputFieldTextHandler oVRVirtualKeyboardInputFieldTextHandler;

    [SerializeField] InputField inputField1;
    [SerializeField] InputField inputField2;

    public void AssignText1()
    {
        //oVRVirtualKeyboardInputFieldTextHandler.inputField = inputField1;
    }

    public void AssignText2()
    {
       // oVRVirtualKeyboardInputFieldTextHandler.inputField = inputField2;
    }
}
