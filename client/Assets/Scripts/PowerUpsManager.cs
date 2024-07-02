using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerUpsManager : MonoBehaviour
{
    [SerializeField]
    GameObject powerUpItem;
    private Dictionary<ulong, GameObject> availablePowerUps = new Dictionary<ulong, GameObject>();

    [SerializeField]
    GameObject powerUpPickedVfx;

    [SerializeField]
    float spawnAnimationDuration = .4f;

    [SerializeField]
    float baseAltitude = 1.1f;

    [SerializeField]
    float maxAltitudeDifference = 2.5f;

    public void UpdatePowerUps()
    {
        List<Entity> powerUpslist = GameServerConnectionManager
            .Instance
            .gamePowerUps;

        for (int i = 0; i < powerUpslist.Count; i++)
        {
            Entity powerupEntity = powerUpslist[i];
            PowerUp powerUp = powerupEntity.PowerUp;

            if (
                powerUp.Status == PowerUpstatus.Unavailable
                && !availablePowerUps.Keys.Contains(powerupEntity.Id)
            )
            {
                CreateNewPowerUp(powerupEntity, powerUp);
            }

            if (
                powerUp.Status == PowerUpstatus.Taken
                && availablePowerUps.Keys.Contains(powerupEntity.Id)
            )
            {
                RemovePowerUp(powerupEntity);
            }
        };
    }

    private void CreateNewPowerUp(Entity powerupEntity, PowerUp powerUp)
    {
        Vector3 powerUpPosition = Utils.transformBackendOldPositionToFrontendPosition(
            powerupEntity.Position
        );

        if (Utils.GetPlayer(powerUp.OwnerId) != null)
        {
            Vector3 previusOwnerPosition = Utils.GetPlayer(powerUp.OwnerId).transform.position;
            GameObject powerupGameObject = Instantiate(
            powerUpItem,
            previusOwnerPosition,
            Quaternion.identity
        );
            StartCoroutine(AnimatePowerUpPosition(powerupGameObject, powerUpPosition));
            availablePowerUps.Add(powerupEntity.Id, powerupGameObject);
        }
        if (Utils.GetCrate(powerUp.OwnerId) != null)
        {
            Vector3 previusOwnerPosition = Utils.transformBackendOldPositionToFrontendPosition(Utils.GetCrate(powerUp.OwnerId).Position);
            GameObject powerupGameObject = Instantiate(
                powerUpItem,
                previusOwnerPosition,
                Quaternion.identity
            );
            StartCoroutine(AnimatePowerUpPosition(powerupGameObject, powerUpPosition));
            availablePowerUps.Add(powerupEntity.Id, powerupGameObject);
        }
    }

    private void RemovePowerUp(Entity powerupEntity)
    {
        GameObject powerUpObject = availablePowerUps[powerupEntity.Id];
        PlayPickUpFeedbacks(powerUpObject);
        Destroy(powerUpObject);
        availablePowerUps.Remove(powerupEntity.Id);
    }

    private void PlayPickUpFeedbacks(GameObject powerUp)
    {
        Vector3 position = new Vector3(
            powerUp.transform.position.x,
            powerUpPickedVfx.transform.position.y,
            powerUp.transform.position.z
        );

        GameObject feedbackVfx = Instantiate(powerUpPickedVfx, position, Quaternion.identity);
    }

    IEnumerator AnimatePowerUpPosition(GameObject powerUp, Vector3 targetPosition)
    {
        Vector3 startPosition = powerUp.transform.position;
        float maxAltitude = powerUp.transform.position.y + maxAltitudeDifference;

        float time = 0;
        while (time < spawnAnimationDuration)
        {
            float currentAltitude =
                baseAltitude
                + maxAltitude * Mathf.Sin(Mathf.Lerp(0, Mathf.PI, time / spawnAnimationDuration));

            targetPosition = new Vector3(targetPosition.x, currentAltitude, targetPosition.z);

            if (powerUp)
            {
                powerUp.transform.position = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    time / spawnAnimationDuration
                );
            }
            time += Time.deltaTime;
            yield return null;
        }
        if (powerUp) powerUp.transform.position = targetPosition;
    }
}
