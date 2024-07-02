using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Collections;
using NativeWebSocket;
using UnityEngine;

public class GameServerConnectionManager : MonoBehaviour
{
    public List<GameObject> players;
    public Dictionary<int, GameObject> projectiles = new Dictionary<int, GameObject>();

    [Tooltip("Session ID to connect to. If empty, a new session will be created")]
    public string sessionId = "";

    [Tooltip("IP to connect to. If empty, localhost will be used")]
    public string serverIp = "localhost";

    public static GameServerConnectionManager Instance;

    public Dictionary<ulong, Position> playersIdPosition = new Dictionary<ulong, Position>();

    public List<Entity> gamePlayers;
    public List<Entity> gameProjectiles;
    public List<Entity> gamePowerUps;
    public List<Entity> gamePools;
    public List<Entity> gameLoots;
    public List<Entity> gameCrates;
    public List<BountyInfo> bounties;

    public List<Entity> obstacles;
    public Dictionary<ulong, ulong> damageDone = new Dictionary<ulong, ulong>();
    public ulong playerId;
    public uint currentPing;

    public float serverTickRate_ms;
    public float bountyPickTime_ms;
    public string serverHash;
    public GameStatus gameStatus;
    public float gameCountdown;

    public (Entity, ulong) winnerPlayer = (null, 0);
    public Dictionary<ulong, string> playersIdName = new Dictionary<ulong, string>();
    public ClientPrediction clientPrediction = new ClientPrediction();
    public EventsBuffer eventsBuffer = new EventsBuffer { deltaInterpolationTime = 100 };
    public bool allSelected = false;
    public float playableRadius;
    public long zoneShrinkTime;

    public bool zoneEnabled = false;
    public bool cinematicDone;
    public bool connected = false;
    private string clientId;
    private bool reconnect;

    public int timestampDifferenceSamplesToCheckWarning;
    public int timestampDifferencesSamplesMaxLength;
    public int showWarningThreshold;
    public int stopWarningThreshold;
    public int msWithoutUpdateShowWarning;
    public int msWithoutUpdateDisconnect;

    public bool shrinking;
    WebSocket ws;

    public BountyInfo bountySelected;
    public Configuration config;

    public static Action<long> OnGameEventTimestampChanged;
    public static Action OnMatchFinished;

    void Start()
    {
        Init();
        StartCoroutine(IsGameCreated());
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
            this.sessionId = ServerConnection.Instance.GameSession;
            this.serverIp = ServerConnection.Instance.serverIp;
            this.clientId = ServerConnection.Instance.clientId;
            this.reconnect = ServerConnection.Instance.reconnect;
            this.playersIdName = ServerConnection.Instance.playersIdName;
            DontDestroyOnLoad(gameObject);
        }
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

    private IEnumerator IsGameCreated()
    {
        yield return new WaitUntil(() => !String.IsNullOrEmpty(SessionParameters.GameId));

        // this.sessionId = ServerConnection.Instance.GameSession;
        // this.serverIp = ServerConnection.Instance.serverIp;
        // this.serverTickRate_ms = ServerConnection.Instance.serverTickRate_ms;
        // this.serverHash = ServerConnection.Instance.serverHash;
        // this.clientId = ServerConnection.Instance.clientId;
        // this.reconnect = ServerConnection.Instance.reconnect;

        if (!connected)
        {
            connected = true;
            ConnectToSession(this.sessionId);
        }
    }

    private void ConnectToSession(string sessionId)
    {
        string url = makeWebsocketUrl(
            "/play/" + SessionParameters.GameId + "/" + SessionParameters.PlayerId
        );
        print(url);
        ws = ServerUtils.CreateWebSocket(url);
        ws.OnMessage += OnWebSocketMessage;
        ws.OnClose += OnWebsocketClose;
        ws.OnError += (e) =>
        {
            Debug.Log("Received error: " + e);
        };
        ws.OnOpen += () =>
        {
            // Once the connection is established we reset so when we try to load the scenes again
            // it waits to fetch it from the Lobby websocket and not reuse
            SessionParameters.GameId = null;
        };
        ws.Connect();
    }

    private void OnWebsocketClose(WebSocketCloseCode closeCode)
    {
        if (closeCode != WebSocketCloseCode.Normal)
        {
            // TODO: Add some error handle for when websocket closes unexpectedly
        }
        else
        {
            Debug.Log("Game websocket closed normally");
        }
    }

    private void OnWebSocketMessage(byte[] data)
    {
        try
        {
            GameEvent gameEvent = GameEvent.Parser.ParseFrom(data);

            switch (gameEvent.EventCase)
            {
                case GameEvent.EventOneofCase.Joined:
                    this.serverTickRate_ms = gameEvent.Joined.Config.Game.TickRateMs;
                    this.playerId = gameEvent.Joined.PlayerId;
                    this.config = gameEvent.Joined.Config;
                    this.bounties = gameEvent.Joined.Bounties.ToList();
                    this.bountyPickTime_ms = gameEvent.Joined.Config.Game.BountyPickTimeMs;
                    this.timestampDifferenceSamplesToCheckWarning = (int)gameEvent.Joined.Config.ClientConfig.ServerUpdate.TimestampDifferenceSamplesToCheckWarning;
                    this.timestampDifferencesSamplesMaxLength = (int)gameEvent.Joined.Config.ClientConfig.ServerUpdate.TimestampDifferencesSamplesMaxLength;
                    this.showWarningThreshold = (int)gameEvent.Joined.Config.ClientConfig.ServerUpdate.ShowWarningThreshold;
                    this.stopWarningThreshold = (int)gameEvent.Joined.Config.ClientConfig.ServerUpdate.StopWarningThreshold;
                    this.msWithoutUpdateShowWarning = (int)gameEvent.Joined.Config.ClientConfig.ServerUpdate.MsWithoutUpdateShowWarning;
                    this.msWithoutUpdateDisconnect = (int)gameEvent.Joined.Config.ClientConfig.ServerUpdate.MsWithoutUpdateDisconnect;
                    break;
                case GameEvent.EventOneofCase.Ping:
                    currentPing = (uint)gameEvent.Ping.Latency;
                    break;
                case GameEvent.EventOneofCase.Update:
                    GameState gameState = gameEvent.Update;

                    eventsBuffer.AddEvent(gameState);

                    KillFeedManager.instance.PutEvents(gameState.Killfeed.ToList());
                    this.playableRadius = gameState.Zone.Radius;
                    this.zoneShrinkTime =
                        gameState.Zone.NextZoneChangeTimestamp - gameState.ServerTimestamp;
                    this.zoneEnabled = gameState.Zone.Enabled;
                    this.gameStatus = gameState.Status;
                    this.gameCountdown = gameState.StartGameTimestamp - gameState.ServerTimestamp;
                    var position = gameState.Players[this.playerId].Position;
                    this.gamePlayers = gameState.Players.Values.ToList();
                    this.gameProjectiles = gameState.Projectiles.Values.ToList();
                    this.gamePowerUps = gameState.PowerUps.Values.ToList();
                    this.gamePools = gameState.Pools.Values.ToList();
                    this.gameLoots = gameState.Items.Values.ToList();
                    this.gameCrates = gameState.Crates.Values.ToList();
                    this.obstacles = gameState.Obstacles.Values.ToList();
                    this.damageDone = gameState.DamageDone.ToDictionary(x => x.Key, x => x.Value);
                    this.shrinking = gameState.Zone.Shrinking;
                    this.playersIdPosition = new Dictionary<ulong, Position>
                    {
                        [this.playerId] = position
                    };
                    OnGameEventTimestampChanged?.Invoke(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    break;
                case GameEvent.EventOneofCase.Finished:
                    winnerPlayer.Item1 = gameEvent.Finished.Winner;
                    winnerPlayer.Item2 = gameEvent.Finished.Winner.Player.KillCount;
                    this.gamePlayers = gameEvent.Finished.Players.Values.ToList();
                    OnMatchFinished?.Invoke();
                    break;
                // This event is for bot clients only, we'll do nothing.
                case GameEvent.EventOneofCase.ToggleBots:
                    break;
                default:
                    print("Message received is: " + gameEvent.EventCase);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log("InvalidProtocolBufferException: " + e);
        }
    }

    public void SendMove(float x, float y, long timestamp)
    {
        Direction direction = new Direction { X = x, Y = y };
        Move moveAction = new Move { Direction = direction };
        GameAction gameAction = new GameAction { Move = moveAction, Timestamp = timestamp };
        SendGameAction(gameAction);
    }

    public void SendSkill(string skill, Direction direction, long timestamp)
    {
        // TODO: This function should receive they type of aiming the skill is using and assign it to a different
        //      field in AttackParameters based on it. For now we hardcode to `target` field and also always hardcode if null
        Direction target = direction == null ? new Direction { X = 0.0f, Y = 0.0f } : direction;
        AttackParameters parameters = new AttackParameters { Target = target };
        Attack attackAction = new Attack { Skill = skill, Parameters = parameters };
        GameAction gameAction = new GameAction { Attack = attackAction, Timestamp = timestamp };
        SendGameAction(gameAction);
    }

    public void SendUseItem(long timestamp)
    {
        // TODO: Hardcode this to 0 because for now we don't have a configurable inventory
        //      Once that is a reality we should receive as part of the parameters
        UseItem useItem = new UseItem { Item = 0 };
        GameAction gameAction = new GameAction { UseItem = useItem, Timestamp = timestamp };
        SendGameAction(gameAction);
    }

    public void SendSelectBounty(string bountyId)
    {
        SelectBounty selectBounty = new SelectBounty { BountyQuestId=bountyId };
        GameAction gameAction = new GameAction { SelectBounty = selectBounty };
        SendGameAction(gameAction);
    }

    public void SendToggleZone(long timestamp)
    {
        ToggleZone toggleZone = new ToggleZone { };
        GameAction gameAction = new GameAction { ToggleZone = toggleZone, Timestamp = timestamp };
        SendGameAction(gameAction);
    }

    public void SendChangeTickrate(long tickrate, long timestamp)
    {
        ChangeTickrate changeTickrate = new ChangeTickrate { Tickrate = tickrate };
        GameAction gameAction = new GameAction { ChangeTickrate = changeTickrate, Timestamp = timestamp };
        SendGameAction(gameAction);
    }
    
    public void SendToggleBots(long timestamp)
    {
        ToggleBots toggleBots = new ToggleBots { };
        GameAction gameAction = new GameAction { ToggleBots = toggleBots, Timestamp = timestamp };
        SendGameAction(gameAction);
    }

    private void SendGameAction<T>(IMessage<T> action)
        where T : IMessage<T>
    {
        using (var stream = new MemoryStream())
        {
            action.WriteTo(stream);
            var msg = stream.ToArray();
            ws.Send(msg);
        }
    }

    private string makeWebsocketUrl(string path)
    {
        int port = 4000;

        if (serverIp.Contains("localhost"))
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
}
