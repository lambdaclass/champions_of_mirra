using System.Linq;
using UnityEngine;

public class DeathSplashEndMessage : MonoBehaviour
{
    private const string WINNER_MESSAGE = "THE KING OF ARABAN!";
    private const string LOSER_MESSAGE = "BETTER LUCK NEXT TIME!";

    private void OnEnable()
    {
        var endMessage = SocketConnectionManager.Instance.PlayerIsWinner(
            LobbyConnection.Instance.playerId
        )
            ? WINNER_MESSAGE
            : LOSER_MESSAGE;
        gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = endMessage;
    }
}
