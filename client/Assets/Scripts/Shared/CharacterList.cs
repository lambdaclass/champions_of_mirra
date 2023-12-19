using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterList : MonoBehaviour
{
    [SerializeField]
    GameObject CharacterItemUIPrefab;

    [SerializeField]
    GameObject CharacterListContainer;

    [SerializeField]
    List<Character> characters;

    [NonSerialized]
    public UnityEvent<Character> OnCharacterSelected = new UnityEvent<Character>();

    void Start()
    {
        PopulateList();
    }

    private void PopulateList()
    {
        characters.ForEach(character =>
        {
            GameObject characterItem = Instantiate(CharacterItemUIPrefab, CharacterListContainer.transform);
            characterItem.GetComponent<Image>().sprite = character.characterSprite;
            characterItem.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(character));
        });
    }

    public void SelectCharacter(Character character)
    {
        OnCharacterSelected.Invoke(character);
    }
}
