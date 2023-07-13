using System;
using System.Collections;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : LevelSelector
{
    private const string CHARACTER_SELECTION_SCENE_NAME = "CharacterSelection";
    private const string LOBBY_SCENE_NAME = "Lobby";
    private const string LOBBIES_SCENE_NAME = "Lobbies";

    [SerializeField]
    GameObject playButton;

    [SerializeField]
    GameObject mapList;

    public static string LevelSelected;

    public override void GoToLevel()
    {
        base.GoToLevel();
        gameObject.GetComponent<MMTouchButton>().DisableButton();
    }

    void Start()
    {
        if (playButton != null && mapList != null)
        {
            if (LobbyConnection.Instance.playerId == 1)
            {
                playButton.SetActive(true);
            }
            else
            {
                playButton.SetActive(false);
                mapList.SetActive(false);
            }
        }
    }

    public void GameStart()
    {
        StartCoroutine(CreateGame());
        this.LevelName = CHARACTER_SELECTION_SCENE_NAME;
        StartCoroutine(Utils.WaitForGameCreation(this.LevelName));
    }

    public IEnumerator CreateGame()
    {
        yield return LobbyConnection.Instance.StartGame();
    }

    public void Back()
    {
        LobbyConnection.Instance.Init();
        this.LevelName = LOBBIES_SCENE_NAME;
        SceneManager.LoadScene(this.LevelName);
    }

    public void BackToLobbyAndCloseConnection()
    {
        SocketConnectionManager.Instance.closeConnection();
        SocketConnectionManager.Instance.Init();
        Back();
    }

    public void SelectMap(string mapName)
    {
        this.LevelName = mapName;
        LevelSelected = mapName;
    }

    private void Update()
    {
        if (
            !String.IsNullOrEmpty(LobbyConnection.Instance.GameSession)
            && LobbyConnection.Instance.playerId != 1
            && SceneManager.GetActiveScene().name == LOBBY_SCENE_NAME
        )
        {
            LobbyConnection.Instance.StartGame();
            SceneManager.LoadScene(CHARACTER_SELECTION_SCENE_NAME);
        }
    }
}
