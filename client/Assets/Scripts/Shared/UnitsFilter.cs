using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UnitsFilter : MonoBehaviour
{
    [SerializeField]
    UnitsUIContainer unitsUIContainer;

    [SerializeField]
    InputField inputField;

    public void FilterUnits()
    {
        string filter = inputField.text.ToLower();
        List<Unit> units = GlobalUserData.Instance.Units;
        unitsUIContainer.Populate(units.Where(unit => unit.character.name.ToLower().Contains(filter)).ToList());
    }

}
