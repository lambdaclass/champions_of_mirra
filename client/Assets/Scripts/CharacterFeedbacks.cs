using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CandyCoded.HapticFeedback;
using MoreMountains.Feedbacks;

public class CharacterFeedbacks : MonoBehaviour
{
    [Header("Setup")]
    public GameObject characterModel;
    public GameObject weaponModel;

    [Header("Feedbacks")]
    [SerializeField]
    List<GameObject> feedbacksStatesPrefabs;

    [SerializeField]
    List<GameObject> feedbacksPrefabs;

    [SerializeField]
    GameObject deathFeedback;

    [SerializeField]
    Color32 damageOverlayColor = new Color32(255, 255, 255, 255);

    [SerializeField]
    Color32 healOverlayColor = new Color32(68, 173, 68, 255);

    Color32 baseOverlayColor = new Color32(0, 0, 0, 0);
    Color32 currentOverlayColor;
    float overlayTime = 0;
    float overlayDuration = 1f;
    bool restoreBaseOverlayColor = true;

    void Update()
    {
        if (restoreBaseOverlayColor && !currentOverlayColor.Equals(baseOverlayColor))
        {
            if (overlayTime < 1)
            {
                overlayTime += (Time.deltaTime / overlayDuration);
            }
            Color32 nextColor = Color.Lerp(currentOverlayColor, baseOverlayColor, overlayTime);
            ChangeModelsOverlayColor(currentOverlayColor);
            currentOverlayColor = nextColor;
        }
    }

    public void SetActiveStateFeedback(string name, bool active)
    {
        GameObject feedbackToActivate = feedbacksStatesPrefabs.Find(el => el.name == name);
        feedbackToActivate?.SetActive(active);
    }

    public List<GameObject> GetFeedbackStateList()
    {
        return feedbacksStatesPrefabs;
    }

    public GameObject GetFeedback(string name)
    {
        GameObject feedback = feedbacksPrefabs.Find(el => el.name == name);
        return feedback;
    }

    public List<GameObject> GetFeedbackList()
    {
        return feedbacksPrefabs;
    }

    public void PlayDeathFeedback()
    {
        if (characterModel.activeSelf == true)
        {
            deathFeedback.SetActive(true);
        }
    }

    public void DamageFeedback(float clientHealth, float playerHealth, ulong playerId)
    {
        if (playerHealth < clientHealth && playerId == SocketConnectionManager.Instance.playerId)
        {
            this.GetFeedback("DamageFeedback").GetComponent<MMF_Player>().PlayFeedbacks();
            this.HapticFeedbackOnDamage();
            this.ChangePlayerTextureOnDamage(clientHealth, playerHealth);
        }
    }

    public void ChangePlayerTextureOnDamage(float clientHealth, float playerHealth)
    {
        if (clientHealth != playerHealth)
        {
            if (playerHealth < clientHealth)
            {
                ApplyColorFeedback(damageOverlayColor);
            }
            if (playerHealth > clientHealth)
            {
                ApplyColorFeedback(healOverlayColor);
            }
        }
    }

    public void HapticFeedbackOnDamage()
    {
        HapticFeedback.HeavyFeedback();
    }

    public void ApplyZoneDamage()
    {
        ChangeModelsOverlayColor(damageOverlayColor);
        currentOverlayColor = damageOverlayColor;
    }

    private void ApplyColorFeedback(Color32 color)
    {
        ChangeModelsOverlayColor(color);
        currentOverlayColor = color;
        ResetOverlay();
    }

    public void ResetOverlay()
    {
        overlayTime = 0f;
        restoreBaseOverlayColor = false;
        StartCoroutine(RemoveModelFeedback());
    }

    public void ChangeModelsOverlayColor(Color color)
    {
        SkinnedMeshRenderer[] skinnedMeshFilter =
            characterModel.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var meshFilter in skinnedMeshFilter)
        {
            Material material = meshFilter.GetComponent<Renderer>().material;
            material.SetColor("_Overlay_Color", color);
        }
        if (weaponModel)
        {
            Renderer[] meshes = weaponModel.GetComponentsInChildren<Renderer>();
            foreach (var mesh in meshes)
            {
                foreach (var material in mesh.materials)
                {
                    material.SetColor("_Overlay_Color", color);
                }
            }
        }
    }

    IEnumerator RemoveModelFeedback()
    {
        yield return new WaitForSeconds(.1f);
        restoreBaseOverlayColor = true;
    }

    public void SetActiveFeedback(GameObject player, string feedbackName, bool value)
    {
        SetActiveStateFeedback(feedbackName, value);
    }

    public void ClearAllFeedbacks(GameObject player)
    {
        GetFeedbackStateList().ForEach(el => el.SetActive(false));
    }
}
