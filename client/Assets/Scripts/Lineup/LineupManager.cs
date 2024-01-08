using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LineupManager : MonoBehaviour, IUnitPopulator
{
    [SerializeField]
    UnitsUIContainer unitsContainer;

    [SerializeField]
    UnitPosition[] playerUnitPositions;

    [SerializeField]
    UnitPosition[] opponentUnitPositions;

    [SerializeField]
    List<Character> characters;

    void Start()
    {
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
                    this.unitsContainer.Populate(unitList, this);
                    SetUpSelectedUnits(unitList, true);
                },
                error => {
                    Debug.LogError("Error when getting the available units: " + error);
                }
            )
        );

        unitsContainer.OnUnitSelected.AddListener(AddUnitToLineup);

        StartCoroutine(
            BackendConnection.GetAvailableUnits(
                "user2",
                units => {
                    List<Unit> unitList = units.Select(unit => new Unit
                    {
                        unitId = unit.id,
                        level = unit.level,
                        character = characters.Find(character => unit.character.ToLower() == character.name.ToLower()),
                        slot = unit.slot,
                        selected = unit.selected
                    }).ToList();
                    SetUpSelectedUnits(unitList, false);
                },
                error => {
                    Debug.LogError("Error when getting the available units: " + error);
                }

            )
        );
    }

    private void SetUpSelectedUnits(List<Unit> units, bool isPlayer)
    {
        UnitPosition[] unitPositions = isPlayer ? playerUnitPositions : opponentUnitPositions;
        foreach(Unit unit in units.Where(unit => unit.selected && unit.slot.Value < unitPositions.Length)) {
            UnitPosition unitPosition;
            unitPosition = unitPositions[unit.slot.Value];
            unitPosition.SetUnit(unit, isPlayer);
            unitPosition.OnUnitRemoved += RemoveUnitFromLineup;
        }
    }

    private void AddUnitToLineup(Unit unit)
    {
        UnitPosition unitPosition = playerUnitPositions.First(unitPosition => !unitPosition.IsOccupied);

        if(unitPosition)
        {
            int slot = Array.FindIndex(playerUnitPositions, up => !up.IsOccupied);

            StartCoroutine(
                BackendConnection.SelectUnit(
                    "user1",
                    unit.unitId,
                    slot,
                    error => {
                        Debug.LogError("Error when selecting unit: " + error);
                    }
                )
            );
            unitPosition.SetUnit(unit, true);
            unitPosition.OnUnitRemoved += RemoveUnitFromLineup;
        }
    }

    private void RemoveUnitFromLineup(Unit unit)
    {
        StartCoroutine(
            BackendConnection.UnselectUnit(
                "user1",
                unit.unitId,
                error => {
                    Debug.LogError("Error when unselecting unit: " + error);
                }
            )
        );
        unitsContainer.SetUnitUIActiveById(unit.unitId);
    }

    public void Populate(Unit unit, GameObject unitItem)
    {
        SpriteState ss = new SpriteState();
        ss.disabledSprite = unit.character.disabledSprite;
        Button unitItemButton = unitItem.GetComponent<Button>();
        unitItemButton.spriteState = ss;
        if(unit.selected) {
            unitItemButton.interactable = false;
        }
    }
}
