using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillDescription : MonoBehaviour, IPointerDownHandler
{
    SkillInfo skillData;
    Sprite skillSprite;

    [SerializeField]
    public CanvasGroup skillBorder;

    [SerializeField]
    SkillsDetailHandler skillsDetailHandler;

    public void SetSkillDescription(SkillInfo skillInfo)
    {
        skillData = skillInfo;
        skillSprite = skillInfo.skillSprite;

        GetComponent<Image>().sprite = skillSprite;

        // The first list element always starts with a selected display
        GameObject firstGameObject = skillsDetailHandler.skillsList[0].gameObject;
        string skillSetType = GetSkillSetType();
        if (this.gameObject == firstGameObject)
        {
            skillsDetailHandler.ResetSelectSkill(this);
            skillsDetailHandler.SetSkillDetaill(
                skillSetType,
                skillData.name,
                skillData.description
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        string skillSetType = GetSkillSetType();
        skillsDetailHandler.SetSkillDetaill(skillSetType, skillData.name, skillData.description);
        skillsDetailHandler.ResetSelectSkill(this);
        skillsDetailHandler.characterInfoManager.PlaySkillAnimation(skillSetType);
    }

    private string GetSkillSetType()
    {
        string skillSetTypeName = "";
        switch (skillData.skillSetType)
        {
            case UIControls.Skill1:
                skillSetTypeName = "Basic";
                break;
            case UIControls.Skill2:
                skillSetTypeName = "Ultimate";
                break;
            case UIControls.Skill3:
                skillSetTypeName = "Dash";
                break;
        }

        return skillSetTypeName;
    }
}
