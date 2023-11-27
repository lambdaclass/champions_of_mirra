using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Linq;

public class CharacterInfoManager : MonoBehaviour
{
    [SerializeField]
    List<CoMCharacter> comCharacters;

    [SerializeField]
    private UIModelManager ModelManager;

    [Header("Character info")]
    [SerializeField]
    TextMeshProUGUI nameText;

    [SerializeField]
    Image classImage;

    [SerializeField]
    TextMeshProUGUI subTitle;

    [Header("Character skills")]
    [SerializeField]
    List<SkillDescription> skillDescriptions;

    [Header("Arrows")]
    [SerializeField]
    GameObject leftButton;

    [SerializeField]
    GameObject rightButton;

    public static int selectedCharacterPosition;

    private List<CoMCharacter> avaiblesCharacters = new List<CoMCharacter>();

    void Start()
    {
        avaiblesCharacters = Utils.GetOnlyAvaibleCharacterInfo(comCharacters);
        SetCharacterInfo(selectedCharacterPosition);
    }

    public void RightArrowFunc()
    {
        if (selectedCharacterPosition == avaiblesCharacters.Count - 1)
        {
            selectedCharacterPosition = 0;
        }
        else
        {
            selectedCharacterPosition = selectedCharacterPosition + 1;
        }

        SetCharacterInfo(selectedCharacterPosition);
    }

    public void LeftArrowFunc()
    {
        if (selectedCharacterPosition == 0)
        {
            selectedCharacterPosition = avaiblesCharacters.Count - 1;
        }
        else
        {
            selectedCharacterPosition = selectedCharacterPosition - 1;
        }
        SetCharacterInfo(selectedCharacterPosition);
    }

    public void SetCharacterInfo(int currentPosition)
    {
        CoMCharacter comCharacter = avaiblesCharacters[currentPosition];
        ModelManager.RemoveCurrentModel();
        ModelManager.SetModel(comCharacter);
        nameText.text = comCharacter.name;
        subTitle.text = comCharacter.description;
        classImage.sprite = comCharacter.classImage;
        skillDescriptions[0].SetSkillDescription(comCharacter.skillsInfo[0]);
        skillDescriptions[1].SetSkillDescription(comCharacter.skillsInfo[1]);
    }
}
