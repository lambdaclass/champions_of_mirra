using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Errors : MonoBehaviour
{
    [SerializeField]
    public GameObject networkContainer;

    [SerializeField]
    public GameObject reconnectContainer;

    [SerializeField]
    public GameObject versionHashesContainer;

    [SerializeField]
    public TextMeshProUGUI networkError;

    [SerializeField]
    public TextMeshProUGUI networkDescription;

    [SerializeField]
    public TextMeshProUGUI reconnectError;

    [SerializeField]
    public TextMeshProUGUI reconnectDescription;

    [SerializeField]
    public MMTouchButton yesButton;

    [SerializeField]
    public TextMeshProUGUI versionHashesWarning;

    [SerializeField]
    public TextMeshProUGUI versionHashesDescription;

    public static Errors Instance;

    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(transform.parent.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.parent.gameObject);

        UnityEvent reconnectEvent = new UnityEvent();
        reconnectEvent.AddListener(Reconnect);
        yesButton.ButtonPressedFirstTime = reconnectEvent;
    }

    public void Reconnect()
    {
        // FIXME: Reimplemnt recconection

        // TODO: This is what LobbiesManager.Reconnect() does
        // whe should leave this here or instantiate that
        // ServerConnection.Instance.Reconnect();
        // TODO: it should go directly to the Battle if I want to go back to it
        // SceneManager.LoadScene("CharacterSelection");
        HideOngoingGameError();
    }

    public void HandleReconnectError(string title, string description)
    {
        reconnectContainer.SetActive(true);
        reconnectError.text = title;
        reconnectDescription.text = description;
    }

    public void HideOngoingGameError()
    {
        reconnectContainer.SetActive(false);
    }

    public void HandleNetworkError(string title, string description)
    {
        networkContainer.SetActive(true);
        networkError.text = title;
        networkDescription.text = description;
    }

    public void HideConnectionError()
    {
        networkContainer.SetActive(false);
    }

    public void HandleVersionHashesError(string title, string description)
    {
        versionHashesContainer.SetActive(true);
        versionHashesWarning.text = title;
        versionHashesDescription.text = description;
    }

    public void HideVersionHashesError()
    {
        versionHashesContainer.SetActive(false);
    }
}
