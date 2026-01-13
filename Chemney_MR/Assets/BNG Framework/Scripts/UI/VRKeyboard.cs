using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace BNG {
    public class VRKeyboard : MonoBehaviour {

        public UnityEngine.UI.InputField AttachedInputField;

        public bool UseShift = false;

        [Header("Sound FX")]
        public AudioClip KeyPressSound;

        List<VRKeyboardKey> KeyboardKeys;

        void Awake() {
            KeyboardKeys = transform.GetComponentsInChildren<VRKeyboardKey>().ToList();
        }

        public void PressKey(string key) {

            if(AttachedInputField != null) {
                UpdateInputField(key);
            }
            else {
                Debug.Log("Pressed Key : " + key);
            }
        }

        public void UpdateInputField(string key)
        {
            string currentText = AttachedInputField.text;

            // Formatted key based on short names
            string formattedKey = key;
            if (key.ToLower() == "space")
            {
                formattedKey = " ";
            }

            // Handle backspace key
            if (formattedKey.ToLower() == "backspace")
            {
                if (currentText.Length > 0)
                {
                    // Remove the last character
                    currentText = currentText.Remove(currentText.Length - 1, 1);
                }

                // Update the InputField text
                AttachedInputField.text = currentText;
                // Place caret at the end of the text
                AttachedInputField.caretPosition = currentText.Length;
            }
            else if (formattedKey.ToLower() == "enter")
            {
                // Handle enter key (if needed)
            }
            else if (formattedKey.ToLower() == "shift")
            {
                ToggleShift();
            }
            else
            {
                // Append the character to the end of the text
                currentText += formattedKey;

                // Update InputField text
                AttachedInputField.text = currentText;
                // Place caret at the end of the text
                AttachedInputField.caretPosition = currentText.Length;
            }

            PlayClickSound();

            // Keep InputField focused
            if (!AttachedInputField.isFocused)
            {
                AttachedInputField.Select();
            }
        }


        public virtual void PlayClickSound() {
            if(KeyPressSound != null) {
                VRUtils.Instance.PlaySpatialClipAt(KeyPressSound, transform.position, 1f, 0.5f);
            }
        }

        public void MoveCaretUp() {
            StartCoroutine(IncreaseInputFieldCareteRoutine());
        }

        public void MoveCaretBack() {
            StartCoroutine(DecreaseInputFieldCareteRoutine());
        }

        public void ToggleShift() {
            UseShift = !UseShift;

            foreach(var key in KeyboardKeys) {
                if(key != null) {
                    key.ToggleShift();
                }
            }
        }

        IEnumerator IncreaseInputFieldCareteRoutine() {
            yield return new WaitForEndOfFrame();
            AttachedInputField.caretPosition = AttachedInputField.caretPosition + 1;
            AttachedInputField.ForceLabelUpdate();
        }

        IEnumerator DecreaseInputFieldCareteRoutine() {
            yield return new WaitForEndOfFrame();
            AttachedInputField.caretPosition = AttachedInputField.caretPosition - 1;
            AttachedInputField.ForceLabelUpdate();
        }

        public void AttachToInputField(UnityEngine.UI.InputField inputField) {
            AttachedInputField = inputField;
        }
    }
}

