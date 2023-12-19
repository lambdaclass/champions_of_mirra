using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerLineupManager : MonoBehaviour
{
    [SerializeField]
    CharacterList characterList;

    [SerializeField]
    List<UnitPosition> unitPositions;

    void Start() {
        characterList.OnCharacterSelected.AddListener(AddCharacterToLineup);
    }

    private void AddCharacterToLineup(Character character)
    {
        UnitPosition up = unitPositions.FirstOrDefault(unitPosition => !unitPosition.IsOccupied);
        if(up)
        {
            up.SetCharacter(character);
        }
    }
}
