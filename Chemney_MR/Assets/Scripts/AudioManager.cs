using UnityEngine;

public class AudioButtonPlayer : MonoBehaviour
{
    public AudioSource audioSource;   // Assign in Inspector
    public AudioClip clipToPlay;      // Assign in Inspector

    // This function can be assigned to a Button OnClick
    public void PlayAudio()
    {
        if (audioSource != null && clipToPlay != null)
        {
            audioSource.clip = clipToPlay;
            audioSource.Play();
            Debug.LogWarning("AudioSource or AudioClip is play!");
        }
        else
        {
            Debug.LogWarning("AudioSource or AudioClip is missing!");
        }
    }
}
