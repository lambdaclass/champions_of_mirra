using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerCounter : MonoBehaviour
{
    protected TMP_Text _totalLobbyPlayersText;
    // Start is called before the first frame update
    void Start()
    {
        if (GetComponent<Text>() == null)
        {
            Debug.LogWarning("PlayerCounter requires a GUIText component.");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_totalLobbyPlayersText == null) {
            _totalLobbyPlayersText = gameObject.GetComponent<TMP_Text>();
        }
        _totalLobbyPlayersText.text = LobbyConnection.Instance.playerCount.ToString() + " / " + LobbyConnection.Instance.lobbyCapacity.ToString();
    }
}
