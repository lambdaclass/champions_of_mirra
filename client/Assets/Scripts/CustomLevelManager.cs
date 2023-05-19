using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.TopDownEngine;
using UnityEngine;

public class CustomLevelManager : LevelManager
{
    private int totalPlayers;
    private int playerCount = 0;
    private int playerId;
    public Character prefab;
    public Camera UiCamera;
    public CinemachineCameraController camera;

    protected override void Awake()
    {
        base.Awake();
        this.totalPlayers = LobbyConnection.Instance.playerCount;
    }

    protected override void Start()
    {
        base.Start();
        GeneratePlayer();
        playerId = LobbyConnection.Instance.playerId;
        setCameraToPlayer(playerId);
    }

    public void GeneratePlayer()
    {
        for (int i = 0; i < totalPlayers; i++)
        {
            if (LobbyConnection.Instance.playerId == i + 1)
            {
                // Player1 is the ID to match with the client InputManager
                prefab.PlayerID = "Player1";
            }
            else
            {
                prefab.PlayerID = "";
            }
            Character newPlayer = Instantiate(
                prefab,
                this.InitialSpawnPoint.transform.position,
                Quaternion.identity
            );
            newPlayer.name = "Player" + " " + (i + 1);
            newPlayer.PlayerID = (i + 1).ToString();

            SocketConnectionManager.playersStatic.Add(newPlayer.gameObject);
            this.Players.Add(newPlayer);
        }
        this.PlayerPrefabs = (this.Players).ToArray();
    }

    private void setCameraToPlayer(int playerID)
    {
        foreach (Character player in this.PlayerPrefabs)
        {
            if (Int32.Parse(player.PlayerID) == playerID)
            {
                this.camera.SetTarget(player);
                this.camera.StartFollowing();
            }
        }
    }
}
