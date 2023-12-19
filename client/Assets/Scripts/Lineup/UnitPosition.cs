using UnityEngine;
using TMPro;
using System;

public class UnitPosition : MonoBehaviour
{
    [SerializeField]
    TMP_Text unitName;

    [SerializeField]
    UIModelManager UIModelManager;

    private bool isOccupied;
    public bool IsOccupied => isOccupied;

    public void SetCharacter(Character character) {
        unitName.text = character.name;
        isOccupied = true;
        unitName.gameObject.SetActive(true);
        // GameObject newUnit = Instantiate(character.prefab, modelContainer.transform);
        // modelContainer.SetActive(true);
        UIModelManager.SetModel(character.prefab);
    }
}
