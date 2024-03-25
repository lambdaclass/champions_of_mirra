using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ClosePopupWithSound : MonoBehaviour
{
	[SerializeField]
	Button confirmButton;

	public Button ConfirmButton {
		get { return confirmButton; }
	}

    public AudioSource audioSource;

    public void OnPopupButtonClick()
    {
        StartCoroutine(PlaySoundAndDeactivatePopup());
    }

    IEnumerator PlaySoundAndDeactivatePopup()
    {
        audioSource.Play();
        yield return new WaitForSecondsRealtime(audioSource.clip.length);
        gameObject.SetActive(false);
    }
}
