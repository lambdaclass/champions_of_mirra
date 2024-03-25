using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SummonManager : MonoBehaviour
{
	[SerializeField]
	BoxUI boxPrefab;

	[SerializeField]
	GameObject boxesContainer;

	[SerializeField]
	GameObject confirmPopup;

	[SerializeField]
	Button confirmPopupButton;

	[SerializeField]
	GameObject newCharacterContainer;

	[SerializeField]
	TMP_Text newUnitName;

	[SerializeField]
	Image newUnitImage;

	[SerializeField]
    GameObject insufficientCurrencyPopup;

	// remove this
	[SerializeField]
	Sprite[] boxesSprites;

	void Start() {
		SocketConnection.Instance.GetBoxes(
			(boxes) => {
				int imageIndex = 0;
				foreach(Box box in boxes) {
					BoxUI boxUI = Instantiate(boxPrefab, boxesContainer.transform);
					Action<string, string> onClick = (userId, boxId) => ShowConfirmPopup(userId, box);
					
					// fix how boxes images are handled
					boxUI.SetBox(box, boxesSprites[imageIndex], onClick);
					imageIndex = imageIndex < boxesSprites.Length ? imageIndex + 1 : 0;
				}
			},
			(reason) => {
				Debug.LogError(reason);
			}
		);
	}

	void ShowConfirmPopup(string userId, Box box) {
		confirmPopupButton.onClick.RemoveAllListeners();
		UnityAction onClickAction = () => Summon(userId, box);
		confirmPopupButton.onClick.AddListener(onClickAction);
		confirmPopup.gameObject.SetActive(true);
	}

	private void Summon(string userId, Box box)
	{
		foreach(KeyValuePair<Currency, int> cost in box.costs) {
			if(cost.Value > GlobalUserData.Instance.GetCurrency(cost.Key)) {
				// Need to specify which currency
            	insufficientCurrencyPopup.SetActive(true);
				return;
			}
		}

		SocketConnection.Instance.Summon(userId, box.id,
			(user, unit) => {
				foreach(KeyValuePair<Currency, int> userCurrency in user.currencies) {
					GlobalUserData.Instance.SetCurrencyAmount(userCurrency.Key, userCurrency.Value);
				}
				GlobalUserData.Instance.User.units.Add(unit);
				newUnitName.text = unit.character.name;
				newUnitImage.sprite = unit.character.inGameSprite;
				newCharacterContainer.SetActive(true);
			},
			reason => {
				switch(reason) {
					case "cant_afford":
						// Need to specify which currency
						insufficientCurrencyPopup.SetActive(true);
						break;
					default:
						Debug.LogError(reason);
						break;
				}
			}	
		);
	}
}
