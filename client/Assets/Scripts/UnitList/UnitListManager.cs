using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitListManager : MonoBehaviour
{
    [SerializeField]
    UnitsUIContainer unitsContainer;

    [SerializeField]
    List<Character> characters;

    void Start() {
        StartCoroutine(
            BackendConnection.GetAvailableUnits(
                "user1",
                units => {
                    List<Unit> unitList = units.Select(unit => new Unit
                    {
                        unitId = unit.id,
                        level = unit.level,
                        character = characters.Find(character => unit.character.ToLower() == character.name.ToLower()),
                        slot = unit.slot,
                        selected = unit.selected
                    }).ToList();
                    this.unitsContainer.Populate(unitList);
                },
                error => {
                    Debug.LogError("Error when getting the available units: " + error);
                }
            )
        );

        unitsContainer.OnUnitSelected.AddListener(SelectUnit);
    }

    void SelectUnit(Unit unit) {
        UnitDetail.SelectUnit(unit);
    }
}
