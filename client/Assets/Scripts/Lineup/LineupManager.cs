using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LineupManager : MonoBehaviour, IUnitPopulator
{
    List<Unit> playerAvailableUnits;

    [SerializeField]
    LineUpUnitsUIContainer unitsContainer;

    [SerializeField]
    UnitPosition[] playerUnitPositions;

    [SerializeField]
    UnitPosition[] opponentUnitPositions;

    void Start()
    {
        StartCoroutine(GetUser());
    }

    private IEnumerator GetUser()
    {
        yield return new WaitUntil(() => GlobalUserData.Instance != null);

        playerAvailableUnits = GlobalUserData.Instance.Units;

        this.unitsContainer.Populate(playerAvailableUnits, this);
        SetUpSelectedUnits(playerAvailableUnits, true);

        unitsContainer.OnUnitSelected.AddListener(AddUnitToLineup);
        
        LevelData levelData = LevelProgress.selectedLevelData;
        SetUpSelectedUnits(levelData.units, false);
    }

    private void SetUpSelectedUnits(List<Unit> units, bool isPlayer)
    {
        UnitPosition[] unitPositions = isPlayer ? playerUnitPositions : opponentUnitPositions;
        foreach(Unit unit in units.Where(unit => unit.selected)) {
            UnitPosition unitPosition = unitPositions[unit.slot.Value - 1];
            unitPosition.SetUnit(unit, isPlayer);
            unitPosition.OnUnitRemoved += RemoveUnitFromLineup;
        }
    }

    private void AddUnitToLineup(Unit unit)
    {
        UnitPosition unitPosition = playerUnitPositions.FirstOrDefault(unitPosition => !unitPosition.IsOccupied);

        if(unitPosition != null)
        {
            int slot = Array.FindIndex(playerUnitPositions, up => !up.IsOccupied) + 1;
            unit.selected = true;
            unit.slot = slot;
            unitPosition.SetUnit(unit, true);
            unitPosition.OnUnitRemoved += RemoveUnitFromLineup;
            SocketConnection.Instance.SelectUnit(unit.id, GlobalUserData.Instance.User.id, slot);
            unitsContainer.SetUnitUIActiveById(unit.id, false);
        }
    }

    private void RemoveUnitFromLineup(Unit unit)
    {
        UnitPosition unitPosition = playerUnitPositions.First(unitPosition => CompareUnitId(unitPosition, unit));
        unitPosition.OnUnitRemoved -= RemoveUnitFromLineup;
        unit.selected = false;
        unit.slot = null;
        SocketConnection.Instance.UnselectUnit(unit.id, GlobalUserData.Instance.User.id);
        unitsContainer.SetUnitUIActiveById(unit.id, true);
    }

    private bool CompareUnitId(UnitPosition unitPosition, Unit unit)
    {
        Unit selectedUnit = unitPosition.GetSelectedUnit();
        if(selectedUnit != null)
        {
            return selectedUnit.id == unit.id;
        }
        return false;
    }

    public void Populate(Unit unit, GameObject unitItem)
    {
        UnitItemUI unitItemUI = unitItem.GetComponent<UnitItemUI>();
        unitItemUI.SetUpUnitItemUI(unit);
        Button unitItemButton = unitItem.GetComponent<Button>();
        if(unit.selected) {
            unitItemUI.SetSelectedChampionMark(true);
            unitItemButton.interactable = false;
        } else if (playerAvailableUnits.Where(unit => unit.selected).Count() >= LineUpUnitsUIContainer.numberOfPositions) {
            unitItemUI.SetLocked(true);
            unitItemButton.interactable = false;
        }
    }
}
