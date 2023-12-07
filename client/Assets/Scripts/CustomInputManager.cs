using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.TopDownEngine;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public enum UIControls
{
    Skill1,
    SkillBasic
}

public enum UIIndicatorType
{
    Cone,
    Area,
    Arrow,
    None
}

public enum UIType
{
    Tap,
    Area,
    Direction,
    Select
}

public class CustomInputManager : InputManager
{
    [SerializeField]
    CustomMMTouchButton SkillBasic;

    [SerializeField]
    CustomMMTouchButton Skill1;

    [SerializeField]
    GameObject SkillBasicCooldownContainer;

    [SerializeField]
    GameObject Skill1CooldownContainer;

    [SerializeField]
    GameObject disarmObjectSkill1;

    [SerializeField]
    GameObject disarmObjectSkill2;

    [SerializeField]
    GameObject disarmObjectSkill3;

    [SerializeField]
    GameObject cancelButton;

    [SerializeField]
    GameObject UIControlsWrapper;

    Dictionary<UIControls, CustomMMTouchButton> mobileButtons;
    Dictionary<UIControls, GameObject> buttonsCooldown;
    private AimDirection directionIndicator;
    private CustomMMTouchJoystick activeJoystick;
    private Vector3 initialLeftJoystickPosition;
    private bool disarmed = false;

    private float currentSkillRadius = 0;
    private bool activeJoystickStatus = false;

    private bool canceled = false;
    private GameObject _player;

    Color32 characterSkillColor;

    public Material material;

    protected override void Start()
    {
        base.Start();
        mobileButtons = new Dictionary<UIControls, CustomMMTouchButton>();
        mobileButtons.Add(UIControls.Skill1, Skill1);
        mobileButtons.Add(UIControls.SkillBasic, SkillBasic);

        // TODO: this could be refactored implementing a button parent linking button and cooldown text
        // or extending CustomMMTouchButton and linking its cooldown text
        buttonsCooldown = new Dictionary<UIControls, GameObject>();
        buttonsCooldown.Add(UIControls.Skill1, Skill1CooldownContainer);
        buttonsCooldown.Add(UIControls.SkillBasic, SkillBasicCooldownContainer);

        UIControlsWrapper.GetComponent<CanvasGroup>().alpha = 0;
    }

    public void Setup()
    {
        _player = Utils.GetPlayer(SocketConnectionManager.Instance.playerId);
        directionIndicator = _player.GetComponentInChildren<AimDirection>();
    }

    public void ActivateDisarmEffect(bool isDisarmed)
    {
        if (disarmed != isDisarmed)
        {
            if (isDisarmed)
            {
                DisableButtons();
                SkillBasic.GetComponent<CustomMMTouchButton>().Interactable = true;
            }
            else
            {
                EnableButtons();
            }
            disarmObjectSkill1.SetActive(isDisarmed);
            disarmObjectSkill2.SetActive(isDisarmed);
            disarmObjectSkill3.SetActive(isDisarmed);
            disarmed = isDisarmed;
        }
    }

    public void InitializeInputSprite(CoMCharacter characterInfo)
    {
        SkillBasic.SetInitialSprite(
            characterInfo.skillsInfo[0].skillSprite,
            characterInfo.skillBackground
        );
        Skill1.SetInitialSprite(
            characterInfo.skillsInfo[1].skillSprite,
            characterInfo.skillBackground
        );
        characterSkillColor = characterInfo.InputFeedbackColor;
    }

    public IEnumerator ShowInputs()
    {
        yield return new WaitForSeconds(.1f);

        UIControlsWrapper.GetComponent<CanvasGroup>().alpha = 1;
    }

    public void AssignSkillToInput(UIControls trigger, UIType triggerType, Skill skill)
    {
        CustomMMTouchButton button = mobileButtons[trigger];
        CustomMMTouchJoystick joystick = button.GetComponent<CustomMMTouchJoystick>();

        switch (triggerType)
        {
            case UIType.Tap:
                // button.ButtonReleased.AddListener(skill.TryExecuteSkill);
                if (joystick)
                {
                    joystick.enabled = false;
                }
                MapTapInputEvents(button, skill);
                break;

            case UIType.Area:
                if (joystick)
                {
                    joystick.enabled = true;
                }
                MapAreaInputEvents(joystick, skill);
                break;

            case UIType.Direction:
                if (joystick)
                {
                    joystick.enabled = true;
                }
                MapDirectionInputEvents(button, skill);
                break;
        }
    }

    private void MapAreaInputEvents(CustomMMTouchJoystick joystick, Skill skill)
    {
        UnityEvent<CustomMMTouchJoystick> aoeEvent = new UnityEvent<CustomMMTouchJoystick>();
        aoeEvent.AddListener(ShowAimAoeSkill);
        joystick.newPointerDownEvent = aoeEvent;

        UnityEvent<Vector2, CustomMMTouchJoystick> aoeDragEvent =
            new UnityEvent<Vector2, CustomMMTouchJoystick>();
        aoeDragEvent.AddListener(AimAoeSkill);
        joystick.newDragEvent = aoeDragEvent;

        UnityEvent<Vector2, Skill> aoeRelease = new UnityEvent<Vector2, Skill>();
        aoeRelease.AddListener(ExecuteAoeSkill);
        joystick.skill = skill;
        joystick.newPointerUpEvent = aoeRelease;
    }

    private void MapTapInputEvents(CustomMMTouchButton button, Skill skill)
    {
        button.skill = skill;

        UnityEvent<Skill> aoeEvent = new UnityEvent<Skill>();
        aoeEvent.AddListener(ShowTapSkill);
        button.newPointerTapDown = aoeEvent;

        UnityEvent<Skill> tapRelease = new UnityEvent<Skill>();
        tapRelease.AddListener(ExecuteTapSkill);
        button.newPointerTapUp = tapRelease;
    }

    public void ShowTapSkill(Skill skill)
    {
        ShowSkillRange(skill);
        directionIndicator.InitIndicator(skill, characterSkillColor);
    }

    public void ShowAimAoeSkill(CustomMMTouchJoystick joystick)
    {
        directionIndicator.InitIndicator(joystick.skill, characterSkillColor);

        // FIXME: Using harcoded value for testing, Value should be set dinamically
        //TODO : Add the spread area (amgle) depeding of the skill.json
        activeJoystick = joystick;

        ShowSkillRange(joystick.skill);
    }

    public void AimAoeSkill(Vector2 aoePosition, CustomMMTouchJoystick joystick)
    {
        //Multiply vector values according to the scale of the animation (in this case 12)
        float multiplier = joystick.skill.GetSkillRadius();
        directionIndicator.area.transform.localPosition = new Vector3(
            aoePosition.x * multiplier,
            aoePosition.y * multiplier,
            -1f
        );
        activeJoystickStatus = canceled;
    }

    public void ExecuteAoeSkill(Vector2 aoePosition, Skill skill)
    {
        directionIndicator.DeactivateIndicator();

        HideSkillRange();

        activeJoystick = null;
        EnableButtons();

        if (!canceled)
        {
            skill.TryExecuteSkill(aoePosition);
        }
    }

    public void ExecuteTapSkill(Skill skill)
    {
        if (!canceled)
        {
            skill.TryExecuteSkill();
        }

        directionIndicator.DeactivateIndicator();
        HideSkillRange();
    }

    private void MapDirectionInputEvents(CustomMMTouchButton button, Skill skill)
    {
        CustomMMTouchJoystick joystick = button.GetComponent<CustomMMTouchJoystick>();
        UnityEvent<CustomMMTouchJoystick> directionEvent = new UnityEvent<CustomMMTouchJoystick>();
        directionEvent.AddListener(ShowAimDirectionSkill);
        joystick.newPointerDownEvent = directionEvent;

        UnityEvent<Vector2, CustomMMTouchJoystick> directionDragEvent =
            new UnityEvent<Vector2, CustomMMTouchJoystick>();
        directionDragEvent.AddListener(AimDirectionSkill);
        joystick.newDragEvent = directionDragEvent;

        UnityEvent<Vector2, Skill> directionRelease = new UnityEvent<Vector2, Skill>();
        directionRelease.AddListener(ExecuteDirectionSkill);
        joystick.skill = skill;
        joystick.newPointerUpEvent = directionRelease;

        button.skill = skill;

        UnityEvent<Skill> aoeEvent = new UnityEvent<Skill>();
        aoeEvent.AddListener(ShowAimDirectionTargetsSkill);
        button.newPointerTapDown = aoeEvent;
    }

    private void ShowAimDirectionSkill(CustomMMTouchJoystick joystick)
    {
        directionIndicator.InitIndicator(joystick.skill, characterSkillColor);

        directionIndicator.SetConeIndicator();

        if (joystick.skill.ExecutesOnQuickTap())
        {
            CharacterOrientation3D characterOrientation =
                _player.GetComponent<CharacterOrientation3D>();
            directionIndicator.Rotate(
                characterOrientation.ForcedRotationDirection.x,
                characterOrientation.ForcedRotationDirection.z,
                joystick.skill
            );
            directionIndicator.ActivateIndicator(joystick.skill.GetIndicatorType());
        }

        activeJoystick = joystick;
    }

    private void ShowAimDirectionTargetsSkill(Skill skill)
    {
        ShowSkillRange(skill);
        directionIndicator.InitIndicator(skill, characterSkillColor);
    }

    private void AimDirectionSkill(Vector2 direction, CustomMMTouchJoystick joystick)
    {
        if (!canceled)
        {
            directionIndicator.Rotate(direction.x, direction.y, joystick.skill);
            directionIndicator.ActivateIndicator(joystick.skill.GetIndicatorType());
        }
        activeJoystickStatus = canceled;
    }

    private void ExecuteDirectionSkill(Vector2 direction, Skill skill)
    {
        directionIndicator.DeactivateIndicator();

        HideSkillRange();

        activeJoystick = null;
        EnableButtons();

        if (!canceled)
        {
            skill.TryExecuteSkill(direction);
        }
    }

    private Vector2 GetPlayerOrientation()
    {
        CharacterOrientation3D characterOrientation =
            _player.GetComponent<CharacterOrientation3D>();
        return new Vector2(
            characterOrientation.ForcedRotationDirection.x,
            characterOrientation.ForcedRotationDirection.z
        );
    }

    public void CheckSkillCooldown(UIControls control, float cooldown, bool showCooldown)
    {
        CustomMMTouchButton button = mobileButtons[control];
        GameObject cooldownContainer = buttonsCooldown[control];
        TMP_Text cooldownText = cooldownContainer.GetComponentInChildren<TMP_Text>();
        if (showCooldown)
        {
            if ((cooldown < 1f && cooldown > 0f) || cooldown > 0f)
            {
                button.DisableButton();
                cooldownContainer.SetActive(true);
                if (cooldown < 1f && cooldown > 0f)
                {
                    cooldownText.text = String.Format("{0:0.0}", cooldown);
                }
                else
                {
                    cooldownText.text = ((ulong)cooldown + 1).ToString();
                }
            }
            else
            {
                button.EnableButton();
                cooldownContainer.SetActive(false);
            }
        }
        else
        {
            cooldownContainer.gameObject.SetActive(false);
            button.EnableButton();
        }
    }

    // TODO: Reactor: avoid fetching player and SkillRange on every use
    public void ShowSkillRange(Skill skill)
    {
        float range = skill.GetSkillRadius();

        Transform skillRange = _player
            .GetComponent<CustomCharacter>()
            .characterBase.SkillRange.transform;
        skillRange.localScale = new Vector3(range * 2, skillRange.localScale.y, range * 2);

        if (skill.IsSelfTargeted())
        {
            material = skillRange.GetComponentInChildren<MeshRenderer>().material;
            material.SetColor("_Color", new Color32(255, 255, 255, 200));
        }
        else
        {
            material = skillRange.GetComponentInChildren<MeshRenderer>().material;
            material.SetColor("_Color", characterSkillColor);
        }
    }

    public void HideSkillRange()
    {
        Transform skillRange = _player
            .GetComponent<CustomCharacter>()
            .characterBase.SkillRange.transform;
        skillRange.localScale = new Vector3(0, skillRange.localScale.y, 0);
    }

    public void SetSkillRangeCancelable()
    {
        Material skillRangeMaterial = _player
            .GetComponent<CustomCharacter>()
            .characterBase.SkillRange.GetComponentInChildren<MeshRenderer>()
            .material;
        skillRangeMaterial.SetColor("_Color", characterSkillColor);

        //directionIndicator.DeactivateIndicator();
    }

    private void DisableButtons()
    {
        foreach (var (key, button) in mobileButtons)
        {
            if (button != activeJoystick)
            {
                button.GetComponent<CustomMMTouchButton>().Interactable = false;
            }
        }
    }

    private void EnableButtons()
    {
        foreach (var (key, button) in mobileButtons)
        {
            button.GetComponent<CustomMMTouchButton>().Interactable = true;
        }
    }

    public void SetCanceled(bool cancelValue, bool dragged, UIIndicatorType indicatorType)
    {
        canceled = cancelValue;
        if (directionIndicator && cancelValue && !dragged)
        {
            directionIndicator.DeactivateIndicator();
        }
        else if (directionIndicator && !cancelValue && dragged)
        {
            directionIndicator.ActivateIndicator(indicatorType);
        }
        if (_player)
        {
            SetSkillRangeCancelable();
        }
    }

    public void ToggleCanceled(bool value)
    {
        cancelButton.SetActive(value);
    }

    private List<GameObject> GetTargetsInSkillRange(Skill skill)
    {
        List<GameObject> inRangeTargets = new List<GameObject>();

        SocketConnectionManager.Instance.players.ForEach(p =>
        {
            if (PlayerIsInSkillRange(p, skill))
            {
                inRangeTargets.Add(p);
            }
        });
        return inRangeTargets;
    }

    private bool PlayerIsInSkillRange(GameObject player, Skill skill)
    {
        switch (skill.GetSkillName())
        {
            case "MULTISHOT":
            case "YUGEN'S MARK":
                return PlayerIsInSkillDirectionConeRange(player, skill);
            case "DISARM":
                return PlayerIsInSkillDirectionArrowRange(player, skill);
            default:
                return PlayerIsInSkillProximityRange(player, skill);
        }
    }

    private bool PlayerIsInSkillProximityRange(GameObject player, Skill skill)
    {
        return !IsSamePlayer(player) && directionIndicator.IsInProximityRange(player);
    }

    private bool PlayerIsInSkillDirectionConeRange(GameObject player, Skill skill)
    {
        return !IsSamePlayer(player) && directionIndicator.IsInsideCone(player);
    }

    private bool PlayerIsInSkillDirectionArrowRange(GameObject player, Skill skill)
    {
        return !IsSamePlayer(player) && directionIndicator.IsInArrowLine(player);
    }

    private bool IsSamePlayer(GameObject player)
    {
        return player.name == _player.name;
    }
}
