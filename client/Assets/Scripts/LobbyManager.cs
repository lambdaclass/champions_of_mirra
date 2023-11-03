using System;
using System.Collections;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : LevelSelector
{
    private const string CHARACTER_SELECTION_SCENE_NAME = "Battle";
    private const string LOBBY_SCENE_NAME = "Lobby";
    private const string LOBBIES_SCENE_NAME = "Lobbies";
    private const string LOBBIES_BACKGROUND_MUSIC = "LobbiesBackgroundMusic";

    [SerializeField]
    GameObject playButton;

    [SerializeField]
    GameObject waitingText;

    public static string LevelSelected;

    public override void GoToLevel()
    {
        base.GoToLevel();
        gameObject.GetComponent<MMTouchButton>().DisableButton();
    }

    void Start()
    {
        if (playButton != null && waitingText != null)
        {
            if (LobbyConnection.Instance.isHost)
            {
                playButton.SetActive(true);
                waitingText.SetActive(false);
            }
            else
            {
                playButton.SetActive(false);
                waitingText.SetActive(true);
            }
        }
    }

    public void GameStart()
    {
        // StartCoroutine(CreateGame());
        this.LevelName = CHARACTER_SELECTION_SCENE_NAME;
        StartCoroutine(Utils.WaitForGameCreation(this.LevelName));
    }
    public void Back()
    {
        LobbyConnection.Instance.Init();
        this.LevelName = LOBBIES_SCENE_NAME;
        SceneManager.LoadScene(this.LevelName);
    }

    public void BackToLobbyAndCloseConnection()
    {
        // Websocket connection is closed as part of Init() destroy;
        SocketConnectionManager.Instance.Init();
        DestroySingletonInstances();
        Back();
    }

    public void BackToLobbyFromGame()
    {
        Destroy(GameObject.Find(LOBBIES_BACKGROUND_MUSIC));
        BackToLobbyAndCloseConnection();
    }

    public void SelectMap(string mapName)
    {
        this.LevelName = mapName;
        LevelSelected = mapName;
    }

    private void DestroySingletonInstances()
    {
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }
    }

    private void Update()
    {
        if (
            !String.IsNullOrEmpty(LobbyConnection.Instance.GameSession)
            && SceneManager.GetActiveScene().name == LOBBY_SCENE_NAME
        )
        {
            LobbyConnection.Instance.StartGame();
            SceneManager.LoadScene(CHARACTER_SELECTION_SCENE_NAME);
        }

        if (this.playButton)
        {
            if (LobbyConnection.Instance.isHost && !this.playButton.activeSelf)
            {
                this.playButton.SetActive(true);
                this.waitingText.SetActive(false);
            }
        }
    }
}
