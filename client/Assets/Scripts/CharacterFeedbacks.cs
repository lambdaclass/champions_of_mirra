using System.Collections.Generic;
using CandyCoded.HapticFeedback;
using DG.Tweening;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

public enum HapticFeedbackType
{
    Light,
    Heavy
}

public class CharacterFeedbacks : MonoBehaviour
{
    [Header("Setup")]
    public GameObject characterModelBody;

    [Header("Feedbacks")]
    [SerializeField]
    List<GameObject> feedbacksStatesPrefabs;

    [SerializeField]
    GameObject deathFeedback,
        damageFeedback,
        healFeedback,
        hitFeedback,
        pickUpFeedback,
        useItemFeedback;

    [SerializeField]
    GameObject goldenClockVFX,
        magicBootsVFX,
        myrrasBlessingVFX,
        giantFruitVFX;

    [SerializeField]
    MMProgressBar healthBar;
    private float previousPlayerRadius;
    private bool didPickUp = false;
    private ulong playerID;
    private Material characterMaterial;
    private Animator modelAnimator;
    private float overlayMultiplier = 0f;
    private float overlayEffectSpeed = 3f;

    // didPickUp value should ideally come from backend
    public bool DidPickUp()
    {
        return didPickUp;
    }

    void Start()
    {
        playerID = GameServerConnectionManager.Instance.playerId;
        characterMaterial = characterModelBody.GetComponent<SkinnedMeshRenderer>().materials[0];
        modelAnimator = GetComponent<CustomCharacter>().CharacterModel.GetComponent<Animator>();
    }

    void Update()
    {
        if (overlayMultiplier > 0)
        {
            overlayMultiplier -= Time.deltaTime * overlayEffectSpeed;
            overlayMultiplier = Mathf.Clamp01(overlayMultiplier);
            characterMaterial.SetFloat("_OverlayColorIntensity", overlayMultiplier);
        }
    }

    private HapticFeedbackType GetHapticTypeByDamage(ulong damage) =>
        damage switch
        {
            < 70 => HapticFeedbackType.Light,
            >= 70 => HapticFeedbackType.Heavy,
        };

    public void SetActiveStateFeedback(string name, bool active)
    {
        GameObject feedbackToActivate = feedbacksStatesPrefabs.Find(el => el.name == name);
        feedbackToActivate?.SetActive(active);
    }

    public List<GameObject> GetFeedbackStateList()
    {
        return feedbacksStatesPrefabs;
    }

    public void PlayDeathFeedback()
    {
        if (characterModelBody.activeSelf == true)
        {
            deathFeedback.SetActive(true);
        }
    }

    public GameObject SelectGO(string name)
    {
        switch (name)
        {
            case "mirra_blessing_effect":
                return myrrasBlessingVFX;
            case "magic_boots_effect":
                return magicBootsVFX;
            case "golden_clock_effect":
                return goldenClockVFX;
            case "giant_effect":
                return giantFruitVFX;
            default:
                return null;
        }
    }

    public void ExecutePickUpItemFeedback(bool state)
    {
        didPickUp = state;
        pickUpFeedback.SetActive(state);
    }

    public void ExecuteHealthFeedback(float clientHealth, float serverPlayerHealth, ulong playerId)
    {
        if (serverPlayerHealth < clientHealth)
        {
            if (playerId == GameServerConnectionManager.Instance.playerId)
            {
                damageFeedback.GetComponent<MMF_Player>().PlayFeedbacks();
                HapticFeedbackType feedbackType = GetHapticTypeByDamage(
                    (ulong)(clientHealth - serverPlayerHealth)
                );
                TriggerHapticFeedback(feedbackType);
            }
            ApplyDamageOverlay();
            this.healthBar.BumpOnDecrease = true;
        }
        if (clientHealth < serverPlayerHealth)
        {
            healFeedback.GetComponent<ParticleSystem>().Play();
            if (playerId == GameServerConnectionManager.Instance.playerId)
            {
                TriggerHapticFeedback(HapticFeedbackType.Light);
            }
        }
    }

    public void ApplyDamageOverlay()
    {
        overlayMultiplier = 1f;
        UpdateOverlayColor(overlayMultiplier);
    }

    public void TriggerHapticFeedback(HapticFeedbackType hapticType)
    {
        switch (hapticType)
        {
            case HapticFeedbackType.Heavy:
                HapticFeedback.HeavyFeedback();
                break;
            case HapticFeedbackType.Light:
                HapticFeedback.LightFeedback();
                break;
        }
    }

    private void UpdateOverlayColor(float colorIntensity)
    {
        characterMaterial.SetFloat("_OverlayColorIntensity", colorIntensity);
    }

    public void SetActiveFeedback(GameObject player, string feedbackName, bool value)
    {
        SetActiveStateFeedback(feedbackName, value);
    }

    public void ClearAllFeedbacks(GameObject player)
    {
        GetFeedbackStateList().ForEach(el => el.SetActive(false));
    }

    public void PlayHitFeedback()
    {
        hitFeedback.GetComponent<MMF_Player>().PlayFeedbacks();
        HapticFeedback.LightFeedback();
    }

    public void UpdateCharacterScale(float serverPlayerRadius)
    {
        // scale logic
        if (serverPlayerRadius != previousPlayerRadius)
        {
            float radiusDiff = serverPlayerRadius - previousPlayerRadius;

            if (previousPlayerRadius != 0)
            {
                float multiplier = CalculateScaleMultiplier(serverPlayerRadius, previousPlayerRadius);
                float newScale = this.transform.localScale.x * multiplier;
                float scaleAnimationDuration = 0.5f;
                // scale animation
                this.transform.DOScale(new Vector3(newScale, newScale, newScale), scaleAnimationDuration);
                if (radiusDiff > 0)
                {
                    // scale up
                    modelAnimator.SetFloat("Giant_Speed_Multiplier", 0.5f);
                }
                else
                {
                    // scale down
                    modelAnimator.SetFloat("Giant_Speed_Multiplier", 1f);
                }
            }
            previousPlayerRadius = serverPlayerRadius;
        }
    }

    float CalculateScaleMultiplier(float newValue, float previousValue)
    {
        float diffPercentage = (newValue * 100) / previousValue;
        float scaleMultiplier = diffPercentage / 100;
        return scaleMultiplier;
    }
}
