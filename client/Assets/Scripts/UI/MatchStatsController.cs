using System;
using System.Linq;
using System.Timers;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;

public class MatchStatsController : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI alivePlayers;

    [SerializeField]
    TextMeshProUGUI zomeTimer;

    [SerializeField]
    TextMeshProUGUI killCount;
    private float nextActionTime = 0.0f;
    public float period = 1f;

    public float time = 0f;

    ulong seconds = LobbyConnection.Instance.serverSettings.RunnerConfig.MapShrinkWaitMs / 1000;

    void Start()
    {
        zomeTimer.text = seconds.ToString();
    }

    void FixedUpdate()
    {
        alivePlayers.text = SocketConnectionManager.Instance.alive.Count().ToString();
        killCount.text = Utils
            .GetGamePlayer(SocketConnectionManager.Instance.playerId)
            .KillCount.ToString();

        time += Time.deltaTime;

        if (time >= period && seconds > 0)
        {
            time = time - period;
            seconds--;
            zomeTimer.text = seconds.ToString();
        }
    }
}
