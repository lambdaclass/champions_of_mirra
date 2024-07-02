using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MoreMountains.Tools;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill Info", menuName = "CoM Skill")]
public class SkillInfo : ScriptableObject
{
    [NonSerialized] public ulong ownerId;
    public new string name;
    public string description;
    public UIType inputType;

    public UIControls skillSetType;
    public float angle;
    public ulong staminaCost;
    public UIIndicatorType indicatorType;
    public bool hasProjectile;
    public bool hasSkillPool;

    [MMCondition("hasProjectile", true)]
    public string projectileSkillKey;

    [MMCondition("hasSkillPool", true)]
    public string poolSkillKey;

    [MMCondition("hasProjectile", true)]
    public GameObject projectilePrefab;

    [MMCondition("hasSkillPool", true)]
    public GameObject poolPrefab;

    public float animationSpeedMultiplier;
    public AudioClip abilityStartSfx;

    public bool sfxHasAbilityStop;

    [MMCondition("sfxHasAbilityStop", true)]
    public AudioClip abilityStopSfx;
    public float skillCircleRange;

    [MMEnumCondition("indicatorType", (int)UIIndicatorType.Cone)]
    public float skillConeAngle;

    [MMEnumCondition("indicatorType", (int)UIIndicatorType.Arrow)]
    public float arrowWidth;

    [MMEnumCondition("indicatorType", (int)UIIndicatorType.Area)]
    public float skillAreaRadius;
    public bool usesHitboxAsArea;
    public bool useCooldown;
    public Sprite skillSprite;

    [Header("Feedbacks")]
    [SerializeField]
    public List<VfxStep> vfxList;
    public List<AnimationStep> animationList;

    public void InitWithBackend(ConfigSkill configSkill, string id)
    {
        this.skillCircleRange = configSkill.TargettingRange == 0 
            ? this.skillCircleRange  : Utils.TransformBackenUnitToClientUnit(configSkill.TargettingRange);
        this.skillAreaRadius =  Utils.TransformBackenUnitToClientUnit(configSkill.TargettingRadius);
        this.ownerId = Convert.ToUInt64(id);
        this.staminaCost = useCooldown ? 0 : configSkill.StaminaCost;
    }
}
