using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Collections;
using NativeWebSocket;
using UnityEngine;

public class SocketConnectionManager : MonoBehaviour
{
    public List<GameObject> players;

    public Dictionary<int, GameObject> projectiles = new Dictionary<int, GameObject>();
    public static Dictionary<int, GameObject> projectilesStatic;

    [Tooltip("Session ID to connect to. If empty, a new session will be created")]
    public string sessionId = "";

    [Tooltip("IP to connect to. If empty, localhost will be used")]
    public string serverIp = "localhost";
    public static SocketConnectionManager Instance;
    public List<Player> gamePlayers;
    public GameEvent gameEvent;
    public List<Projectile> gameProjectiles;
    public Dictionary<ulong, string> selectedCharacters;
    public ulong playerId;
    public uint currentPing;
    public uint serverTickRate_ms;
    public string serverHash;
    public (Player, ulong) winnerPlayer = (null, 0);

    public List<Player> winners = new List<Player>();

    public ClientPrediction clientPrediction = new ClientPrediction();

    public List<GameEvent> gameEvents = new List<GameEvent>();
    private Boolean botsActive = true;

    public EventsBuffer eventsBuffer;
    public bool allSelected = false;

    public float playableRadius;
    public Position shrinkingCenter;

    public List<Player> alivePlayers = new List<Player>();
    public List<LootPackage> updatedLoots = new List<LootPackage>();

    public struct BotSpawnEventData
    {
        public List<Player> gameEventPlayers;
        public List<Player> gamePlayers;
    }

    public event Action<BotSpawnEventData> BotSpawnRequested;

    WebSocket ws;

    private string clientId;
    private bool reconnect;

    public class Session
    {
        public string sessionId { get; set; }
    }

    public void Awake()
    {
        Init();
    }

    public void Init()
    {
        if (Instance != null)
        {
            if (this.ws != null)
            {
                this.ws.Close();
            }
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            this.sessionId = LobbyConnection.Instance.GameSession;
            this.serverIp = LobbyConnection.Instance.serverIp;
            this.serverTickRate_ms = LobbyConnection.Instance.serverTickRate_ms;
            this.serverHash = LobbyConnection.Instance.serverHash;
            this.clientId = LobbyConnection.Instance.clientId;
            this.reconnect = LobbyConnection.Instance.reconnect;
            projectilesStatic = this.projectiles;
            DontDestroyOnLoad(gameObject);

            if (this.reconnect)
            {
                this.selectedCharacters = LobbyConnection.Instance.reconnectPlayers;
                this.allSelected = !LobbyConnection.Instance.reconnectToCharacterSelection;
            }
        }
    }

    void Start()
    {
        playerId = LobbyConnection.Instance.playerId;
        ConnectToSession(this.sessionId);
        eventsBuffer = new EventsBuffer { deltaInterpolationTime = 100 };
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null)
        {
            ws.DispatchMessageQueue();
        }
#endif
    }

    private void ConnectToSession(string sessionId)
    {
        string url = makeWebsocketUrl("/play/" + sessionId + "/" + this.clientId + "/" + playerId);
        print(url);
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("dark-worlds-client-hash", GitInfo.GetGitHash());
        ws = new WebSocket(url, headers);
        ws.OnMessage += OnWebSocketMessage;
        ws.OnClose += onWebsocketClose;
        ws.OnError += (e) =>
        {
            Debug.Log("Received error: " + e);
        };
        ws.Connect();
    }

    private void OnWebSocketMessage(byte[] data)
    {
        try
        {
            GameEvent gameEvent = GameEvent.Parser.ParseFrom(data);
            switch (gameEvent.Type)
            {
                case GameEventType.StateUpdate:
                    this.playableRadius = gameEvent.PlayableRadius;
                    this.shrinkingCenter = gameEvent.ShrinkingCenter;
                    KillFeedManager.instance.putEvents(gameEvent.Killfeed.ToList());
                    if (
                        this.gamePlayers != null
                        && this.gamePlayers.Count < gameEvent.Players.Count
                        && SpawnBot.Instance != null
                    )
                    {
                        OnBotSpawnRequested(gameEvent.Players.ToList());
                        // gameEvent.Players
                        //     .ToList()
                        //     .FindAll((player) => !this.gamePlayers.Any((p) => p.Id == player.Id))
                        //     .ForEach(
                        //         (player) =>
                        //         {
                        //             var spawnPosition =
                        //                 Utils.transformBackendPositionToFrontendPosition(
                        //                     player.Position
                        //                 );
                        //             var botId = player.Id.ToString();
                        //             SpawnBot.Instance.playerPrefab
                        //                 .GetComponent<CustomCharacter>()
                        //                 .PlayerID = "";

                        //             CustomCharacter newPlayer = Instantiate(
                        //                 SpawnBot.Instance.playerPrefab.GetComponent<CustomCharacter>(),
                        //                 spawnPosition,
                        //                 Quaternion.identity
                        //             );
                        //             newPlayer.PlayerID = botId.ToString();
                        //             newPlayer.name = "BOT" + botId;
                        //             this.players.Add(newPlayer.gameObject);
                        //         }
                        //     );
                    }
                    this.gamePlayers = gameEvent.Players.ToList();
                    eventsBuffer.AddEvent(gameEvent);
                    this.gameProjectiles = gameEvent.Projectiles.ToList();
                    alivePlayers = gameEvent.Players.ToList().FindAll(el => el.Health > 0);
                    updatedLoots = gameEvent.Loots.ToList();
                    break;
                case GameEventType.PingUpdate:
                    currentPing = (uint)gameEvent.Latency;
                    break;
                case GameEventType.GameFinished:
                    winnerPlayer.Item1 = gameEvent.WinnerPlayer;
                    winnerPlayer.Item2 = gameEvent.WinnerPlayer.KillCount;
                    this.gamePlayers = gameEvent.Players.ToList();
                    break;
                case GameEventType.InitialPositions:
                    this.gamePlayers = gameEvent.Players.ToList();
                    break;
                case GameEventType.SelectedCharacterUpdate:
                    this.selectedCharacters = fromMapFieldToDictionary(
                        gameEvent.SelectedCharacters
                    );
                    break;
                case GameEventType.FinishCharacterSelection:
                    this.selectedCharacters = fromMapFieldToDictionary(
                        gameEvent.SelectedCharacters
                    );
                    this.allSelected = true;
                    this.gamePlayers = gameEvent.Players.ToList();
                    break;
                default:
                    print("Message received is: " + gameEvent.Type);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log("InvalidProtocolBufferException: " + e);
        }
    }

    private void onWebsocketClose(WebSocketCloseCode closeCode)
    {
        if (closeCode != WebSocketCloseCode.Normal)
        {
            LobbyConnection.Instance.errorConnection = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobbies");
            this.Init();
            LobbyConnection.Instance.Init();
        }
    }

    public Dictionary<ulong, string> fromMapFieldToDictionary(MapField<ulong, string> dict)
    {
        Dictionary<ulong, string> result = new Dictionary<ulong, string>();

        foreach (KeyValuePair<ulong, string> element in dict)
        {
            result.Add(element.Key, element.Value);
        }

        return result;
    }

    public static Player GetPlayer(ulong id, List<Player> playerList)
    {
        return playerList.Find(el => el.Id == id);
    }

    public void SendAction(ClientAction action)
    {
        using (var stream = new MemoryStream())
        {
            action.WriteTo(stream);
            var msg = stream.ToArray();
            ws.Send(msg);
        }
    }

    public void CallSpawnBot()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        ClientAction clientAction = new ClientAction
        {
            Action = Action.AddBot,
            Timestamp = timestamp
        };
        SendAction(clientAction);
    }

    public void ToggleBots()
    {
        ClientAction clientAction;
        if (this.botsActive)
        {
            clientAction = new ClientAction { Action = Action.DisableBots };
        }
        else
        {
            clientAction = new ClientAction { Action = Action.EnableBots };
        }

        this.botsActive = !this.botsActive;
        SendAction(clientAction);
    }

    private string makeUrl(string path)
    {
        var useProxy = LobbyConnection.Instance.serverSettings.RunnerConfig.UseProxy;
        int port;

        if (useProxy == "true")
        {
            port = 5000;
        }
        else
        {
            port = 4000;
        }

        if (serverIp.Contains("localhost"))
        {
            return "http://" + serverIp + ":" + port + path;
        }
        else if (serverIp.Contains("10.150.20.186"))
        {
            return "http://" + serverIp + ":" + port + path;
        }
        else
        {
            return "https://" + serverIp + path;
        }
    }

    private string makeWebsocketUrl(string path)
    {
        var useProxy = LobbyConnection.Instance.serverSettings.RunnerConfig.UseProxy;

        int port;

        if (useProxy == "true")
        {
            port = 5000;
        }
        else
        {
            port = 4000;
        }

        if (serverIp.Contains("localhost"))
        {
            return "ws://" + serverIp + ":" + port + path;
        }
        else if (serverIp.Contains("10.150.20.186"))
        {
            return "ws://" + serverIp + ":" + port + path;
        }
        else
        {
            return "wss://" + serverIp + path;
        }
    }

    public void closeConnection()
    {
        ws.Close();
    }

    public bool isConnectionOpen()
    {
        return ws.State == NativeWebSocket.WebSocketState.Open;
    }

    public bool GameHasEnded()
    {
        return winnerPlayer.Item1 != null;
    }

    public bool PlayerIsWinner(ulong playerId)
    {
        return GameHasEnded() && winnerPlayer.Item1.Id == playerId;
    }

    private void OnBotSpawnRequested(List<Player> gameEventPlayers)
    {
        BotSpawnEventData botSpawnEventData = new BotSpawnEventData();
        botSpawnEventData.gameEventPlayers = gameEventPlayers;
        botSpawnEventData.gamePlayers = this.gamePlayers;
        BotSpawnRequested?.Invoke(botSpawnEventData);
    }
}
