using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    [SerializeField]
    GameObject victorySplash;

    [SerializeField]
    GameObject defeatSplash;

    [SerializeField]
    UnitPosition[] playerUnitPositions;

    [SerializeField]
    UnitPosition[] opponentUnitPositions;

    [SerializeField]
    List<Character> characters;

    readonly string playerDeviceId = "user1";

    void Start()
    {
        GetAndSetUpUserAvailableUnits(playerDeviceId, true);

        GetAndSetUpUserAvailableUnits("user2", false);

        StartCoroutine(
            BackendConnection.GetOpponents
            (
                playerDeviceId, opponents => {
                    string opponentId = opponents[0].id;
                    SimulateBattle(playerDeviceId, opponentId);                    
                },
                error => {
                    Debug.LogError("Error when getting the opponents: " + error);
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
        }
    }

    private void GetAndSetUpUserAvailableUnits(string playerDeviceId, bool isPlayer)
    {
        StartCoroutine(
            BackendConnection.GetAvailableUnits(
                playerDeviceId,
                units => {
                    List<Unit> unitList = units.Select(unit => new Unit
                    {
                        unitId = unit.id,
                        level = unit.level,
                        character = characters.Find(character => unit.character.ToLower() == character.name.ToLower()),
                        slot = unit.slot,
                        selected = unit.selected
                    }).ToList();
                    SetUpSelectedUnits(unitList, isPlayer);
                },
                error => {
                    Debug.LogError("Error when getting the available units: " + error);
                }
            )
        );
    }

    private void SimulateBattle(string playerDeviceId, string opponentId)
    {
        StartCoroutine(
            BackendConnection.GetBattleResult(playerDeviceId, opponentId,
                winnerId => {
                    if(winnerId == opponentId) {
                        defeatSplash.SetActive(true);
                    } else {
                        victorySplash.SetActive(true);
                    }
                },
                error => {
                    Debug.LogError("Error when getting the battle result: " + error);
                }
            )
        );
    }
}
