using System.Collections;
using MoreMountains.TopDownEngine;
using UnityEngine;

public class PlayerFeedbacks : MonoBehaviour
{
    [SerializeField]
    CustomInputManager InputManager;

    public void PlayDeathFeedback(Character player)
    {
        if (player.CharacterModel.activeSelf == true)
        {
            player.GetComponent<Health>().DeathMMFeedbacks.PlayFeedbacks();
        }
    }

    public void DisplayDamageRecieved(
        GameObject player,
        Health healthComponent,
        float playerHealth,
        ulong id
    )
    {
        if (
            healthComponent.CurrentHealth != playerHealth
            && SocketConnectionManager.Instance.playerId == id
        )
        {
            healthComponent.Damage(0.001f, this.gameObject, 0, 0, Vector3.up);
        }
    }

    public void ChangePlayerTextureOnDamage(GameObject player, float auxHealth, float playerHealth)
    {
        // player.GetComponentInChildren<OverlayEffect>().enabled = true;

        if (auxHealth != playerHealth)
        {
            player.GetComponentInChildren<OverlayEffect>().enabled = true;
        }
        else
        {
            if (player.GetComponentInChildren<OverlayEffect>().enabled)
            {
                StartCoroutine(WaitToRemoveShader(player));
            }
        }
    }

    IEnumerator WaitToRemoveShader(GameObject player)
    {
        yield return new WaitForSeconds(0.2f);
        player.GetComponentInChildren<OverlayEffect>().enabled = false;
    }

    public void ExecuteH4ckDisarmFeedback(bool disarmed)
    {
        InputManager.ActivateDisarmEffect(disarmed);
    }

    public void SetActiveFeedback(GameObject player, string feedbackName, bool value)
    {
        player.GetComponentInChildren<FeedbackContainer>().SetActiveFeedback(feedbackName, value);
    }

    public void ClearAllFeedbacks(GameObject player)
    {
        player
            .GetComponentInChildren<FeedbackContainer>()
            .GetFeedbackList()
            .ForEach(el => el.SetActive(false));
    }
}
