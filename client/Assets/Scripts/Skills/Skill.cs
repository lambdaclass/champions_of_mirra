using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;
using static MoreMountains.Tools.MMSoundManager;

public class Skill : CharacterAbility
{
    const float MAX_RANGE = 50f;
    const float INNER_RANGE = 2.5f;

    [SerializeField]
    public string skillId;

    // [SerializeField]
    // protected Communication.Protobuf.Action serverSkill;
    protected string serverSkill;

    [SerializeField]
    protected bool blocksMovementOnExecute = true;

    [SerializeField]
    protected SkillInfo skillInfo;

    // feedbackRotatePosition used to track the position to look at when executing the animation feedback
    private Vector2 feedbackRotatePosition;

    protected override void Start()
    {
        base.Start();
        if (blocksMovementOnExecute)
        {
            BlockingMovementStates = new CharacterStates.MovementStates[1];
            BlockingMovementStates[0] = CharacterStates.MovementStates.Attacking;
        }

        if (skillInfo)
        {
            _animator.SetFloat(skillId + "Speed", skillInfo.animationSpeedMultiplier);
        }
    }

    public void SetSkill(string serverSkill, SkillInfo skillInfo)
    {
        this.serverSkill = serverSkill;
        this.skillInfo = skillInfo;
        this.AbilityStartSfx = skillInfo.abilityStartSfx;
        if (skillInfo.sfxHasAbilityStop)
        {
            this.AbilityStopSfx = skillInfo.abilityStopSfx;
        }
    }

    public SkillInfo GetSkillInfo()
    {
        return skillInfo;
    }

    public GameObject GetProjectileFromSkill()
    {
        return skillInfo?.projectilePrefab;
    }

    public void TryExecuteSkill()
    {
        // if (AbilityAuthorized)
        // {
        //     Vector3 direction = this.GetComponent<Character>()
        //         .GetComponent<CharacterOrientation3D>()
        //         .ForcedRotationDirection;
        //     RelativePosition relativePosition = new RelativePosition { X = 0, Y = 0 };
        //     feedbackRotatePosition = new Vector2(direction.x, direction.z);
        //     ExecuteSkill(relativePosition);
        // }
    }

    public void TryExecuteSkill(Vector2 position)
    {
        if (AbilityAuthorized)
        {
            Direction direction = new Direction { X = position.x, Y = position.y };
            feedbackRotatePosition = new Vector2(position.x, position.y);
            ExecuteSkill(direction);
        }
    }

    private void ExecuteSkill(Direction direction)
    {
        if (AbilityAuthorized)
        {
            SendActionToBackend(direction);
        }
    }

    public void ExecuteFeedbacks(ulong duration, bool blockMovement)
    {
        ClearAnimator();

        // Setup
        AudioClip sfxClip;

        sfxClip = skillInfo.sfxHasAbilityStop ? skillInfo.abilityStopSfx : null;

        // State & animation
        ChangeCharacterState(skillId, blockMovement);
        if (skillInfo.animationList.Count > 0)
        {
            List<AnimationStep> animationList = new List<AnimationStep>(skillInfo.animationList);
            StartCoroutine(ExecuteChainedAnimation(animationList, (duration / 1000f)));
        }
        StartCoroutine(AutoEndSkillAnimation(skillId, duration / 1000f));

        // Visual effects
        foreach (var vfxStep in skillInfo.vfxList)
        {
            StartCoroutine(
                ExecuteFeedbackVfx(
                    vfxStep.vfx,
                    vfxStep.duration,
                    vfxStep.delay,
                    vfxStep.instantiateVfxOnModel
                )
            );
        }

        // Sound effects
        if (sfxClip)
        {
            Sound3DManager sound3DManager = GetComponentInChildren<Sound3DManager>();
            sound3DManager.SetSfxSound(sfxClip);
            sound3DManager.PlaySfxSound();
        }
    }

    public IEnumerator AutoEndSkillAnimation(string skillAnimationId, float duration)
    {
        yield return new WaitForSeconds(duration);
        EndSkillAnimation(skillAnimationId);
    }

    public void EndSkillAnimation(string animationId)
    {
        _movement.ChangeState(CharacterStates.MovementStates.Idle);
        _animator.SetBool(animationId, false);
    }

    IEnumerator ExecuteChainedAnimation(
        List<AnimationStep> pendingAnimations,
        float totalDuration,
        float previousAnimationStep = 0
    )
    {
        AnimationStep nextAnimation = pendingAnimations[0];
        pendingAnimations.RemoveAt(0);

        float animationDuration = nextAnimation.durationPercent * totalDuration;
        float animationStep = previousAnimationStep + 1;
        string animationStepId = skillId + "_s" + animationStep;

        string previousAnimationStepId = skillId + "_s" + previousAnimationStep;

        SetAnimation(previousAnimationStepId, false);
        SetAnimation(animationStepId, true);

        yield return new WaitForSeconds(animationDuration);

        if (pendingAnimations.Count > 0)
        {
            StartCoroutine(
                ExecuteChainedAnimation(pendingAnimations, totalDuration, animationStep)
            );
        }
        else
        {
            SetAnimation(animationStepId, false);
        }
    }

    IEnumerator ExecuteFeedbackVfx(
        GameObject vfx,
        float duration,
        float delay,
        bool instantiateVfxOnModel
    )
    {
        yield return new WaitForSeconds(delay);

        GameObject vfxInstance;
        if (instantiateVfxOnModel)
        {
            vfxInstance = Instantiate(vfx, _model.transform);
        }
        else
        {
            Vector3 vfxPosition = new Vector3(
                _model.transform.position.x,
                vfx.transform.position.y,
                _model.transform.position.z
            );
            vfxInstance = Instantiate(vfx, vfxPosition, vfx.transform.rotation);
        }

        Destroy(vfxInstance, duration);
    }

    private void ClearAnimator()
    {
        foreach (int skill in Enum.GetValues(typeof(UIControls)))
        {
            String skillName = Enum.GetName(typeof(UIControls), skill);
            _animator.SetBool(skillName, false);
            _animator.SetBool(skillName + "_start", false);
        }
    }

    private void SetAnimation(string animationId, bool value)
    {
        _animator.SetBool(animationId, value);
    }

    private void ChangeCharacterState(string animation, bool blockingMovement)
    {
        CharacterStates.MovementStates currentState = blockingMovement
            ? CharacterStates.MovementStates.Attacking
            : CharacterStates.MovementStates.Dashing;

        _movement.ChangeState(currentState);
        _animator.SetBool(animation, true);
    }

    private void ChangeCharacterStateToDash(string animation)
    {
        _movement.ChangeState(CharacterStates.MovementStates.Dashing);
        _animator.SetBool(animation, true);
    }

    private void SendActionToBackend(Direction direction)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        GameServerConnectionManager.Instance.SendSkill(serverSkill, direction, timestamp);
    }

    public virtual void StopAbilityStopFeedbacks()
    {
        AbilityStopFeedbacks?.StopFeedbacks();
    }

    public float GetAngle()
    {
        return this.skillInfo.angle;
    }

    public float GetSkillRadius()
    {
        float radius;
        switch (skillInfo.skillCircleRadius)
        {
            case 0:
                radius = MAX_RANGE;
                break;
            case -1:
                radius = INNER_RANGE;
                break;
            default:
                radius = skillInfo.skillCircleRadius;
                break;
        }
        return radius;
    }

    public void SetSkillRadius(float radius)
    {
        skillInfo.skillCircleRadius = radius;
    }

    public void SetSkillAreaRadius(float radius)
    {
        skillInfo.skillAreaRadius = radius;
    }

    public float GetIndicatorAngle()
    {
        return skillInfo.skillConeAngle;
    }

    public float GetArroWidth()
    {
        return skillInfo.arrowWidth;
    }

    public UIIndicatorType GetIndicatorType()
    {
        return skillInfo.indicatorType;
    }

    public String GetSkillName()
    {
        return skillInfo.name;
    }

    public bool ExecutesOnQuickTap()
    {
        return skillInfo.executeOnQuickTap;
    }

    public bool IsSelfTargeted()
    {
        return skillInfo.skillCircleRadius == -1;
    }
}
