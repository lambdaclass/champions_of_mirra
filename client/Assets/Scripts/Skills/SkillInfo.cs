using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill Info", menuName = "CoM Skill")]
public class SkillInfo : ScriptableObject
{
    public new string name;
    public UIType inputType;
    public GameObject feedbackAnimation;
    public float feedbackVfxTime;
    public GameObject startFeedbackVfx;
    public float startFeedbackVfxTime;
    public bool instantiateAnimationOnModel;
    public float animationSpeedMultiplier;
    public bool hasModelAnimation;
    public AudioClip abilityStartSfx;
}
