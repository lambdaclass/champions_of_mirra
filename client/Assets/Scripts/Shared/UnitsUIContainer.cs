using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UnitsUIContainer : MonoBehaviour
{
    [SerializeField]
    GameObject unitItemUIPrefab;

    [SerializeField]
    GameObject unitsContainer;

    [NonSerialized]
    public UnityEvent<Unit> OnUnitSelected = new UnityEvent<Unit>();

    private Dictionary<string, GameObject> unitUIItemDictionary = new Dictionary<string, GameObject>();

    public void Populate(List<Unit> units, IUnitPopulator unitPopulator = null)
    {
        unitsContainer.SetActive(false);
        units.ForEach(unit =>
        {
            GameObject unitUIItem = Instantiate(unitItemUIPrefab, unitsContainer.transform);
            unitUIItem.GetComponent<Image>().sprite = unit.character.iconSprite;
            Button unitItemButton = unitUIItem.GetComponent<Button>();
            unitItemButton.onClick.AddListener(() => SelectUnit(unit, unitItemButton));
            if (unitPopulator != null)
            {
                unitPopulator.Populate(unit, unitUIItem);
            }
            unitUIItemDictionary.Add(unit.unitId, unitUIItem);
        });
        unitsContainer.SetActive(true);
    }

    public void SelectUnit(Unit unit, Button unitItemButton)
    {
        OnUnitSelected.Invoke(unit);
        unitItemButton.interactable = false;
    }

    public void SetUnitUIActiveById(string unitId)
    {
        unitUIItemDictionary[unitId].GetComponent<Button>().interactable = true;
    }
}
