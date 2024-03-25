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
	LevelManager levelManager;

    void Start()
    {
		victorySplash.SetActive(false);
		defeatSplash.SetActive(false);
        List<Unit> userUnits = GlobalUserData.Instance.Units;
        List<Unit> opponentUnits = LevelProgress.selectedLevelData.units;

        SetUpUnits(userUnits, opponentUnits);
        Battle();
    }

    public void Battle()
    {
        SocketConnection.Instance.Battle(GlobalUserData.Instance.User.id, LevelProgress.selectedLevelData.id, (result) => {
            HandleBattleResult(result);
        });
    }

    private void HandleBattleResult(bool result)
    {
        if(result) {
			// Should this be here? refactor after demo?
			try {
				SocketConnection.Instance.GetUserAndContinue();
			} catch (Exception ex) {
				Debug.LogError(ex.Message);
			}
            GlobalUserData user = GlobalUserData.Instance;
            user.AddCurrency(LevelProgress.selectedLevelData.rewards);
            user.AddExperience(LevelProgress.selectedLevelData.experienceReward);
            user.AccumulateAFKRewards();
            user.User.afkMaxCurrencyReward = LevelProgress.selectedLevelData.afkCurrencyRate;
            user.User.afkMaxExperienceReward = LevelProgress.selectedLevelData.afkExperienceRate;
            victorySplash.GetComponentInChildren<RewardsUIContainer>().Populate(CreateRewardsList());
            victorySplash.SetActive(true);
            victorySplash.GetComponent<AudioSource>().Play();

            GameObject victoryText = victorySplash.transform.Find("CenterContainer").transform.Find("Sign").transform.Find("Text").gameObject;

            // Always shows the same texts when a win is achieved, but they should change based on if it was the last level of the campaign and if there are other campaigns after that
            victoryText.GetComponent<Text>().text = "Victory!";

			SetUpNextButton();
			
			// This should be handled differently
			CampaignManager.selectedCampaignData.levels.Find(level => level.id == LevelProgress.selectedLevelData.id).status = LevelProgress.Status.Completed;
			if(CampaignManager.selectedCampaignData.levels.Any(level => level.id == LevelProgress.nextLevelData.id)) {
				CampaignManager.selectedCampaignData.levels.Find(level => level.id == LevelProgress.nextLevelData.id).status = LevelProgress.Status.Unlocked;
			}
        } else {
            defeatSplash.SetActive(true);
            defeatSplash.GetComponent<AudioSource>().Play();
        }
    }

	private void SetUpNextButton()
	{
		GameObject nextButton = victorySplash.transform.Find("Next").gameObject;
		if(LevelProgress.selectedLevelData.campaignId != LevelProgress.nextLevelData.campaignId) {
			nextButton.GetComponentInChildren<Text>().text = "NEXT CAMPAIGN";
		} else {
			nextButton.GetComponentInChildren<Text>().text = "NEXT LEVEL";
			nextButton.GetComponent<Button>().onClick.AddListener(() => {
				LevelProgress.selectedLevelData = LevelProgress.nextLevelData;
				LevelProgress.nextLevelData = LevelProgress.NextLevel(LevelProgress.nextLevelData);
				gameObject.GetComponent<LevelManager>().ChangeToScene("Lineup");
			});
		}
	}

    private void SetUpUnits(List<Unit> userUnits, List<Unit> opponentUnits)
    {
        SetUpUserUnits(userUnits, true);
        SetUpUserUnits(opponentUnits, false);
    }

    private void SetUpUserUnits(List<Unit> units, bool isPlayer)
    {
        UnitPosition[] unitPositions = isPlayer ? playerUnitPositions : opponentUnitPositions;
		// The -1 are since the indexes of the slots in the database go from 1 to 6, and the indexes of the unit position game objects range from 0 to 5
        foreach(Unit unit in units.Where(unit => unit.selected)) {
            UnitPosition unitPosition = unitPositions[unit.slot.Value - 1];
            unitPosition.SetUnit(unit, isPlayer);    
        }
    }

    private List<UIReward> CreateRewardsList() {
        List<UIReward> rewards = new List<UIReward>();

        if (LevelProgress.selectedLevelData.experienceReward != 0) { rewards.Add(new ExperienceUIReward(LevelProgress.selectedLevelData.experienceReward)); }

        foreach (var currencyReward in LevelProgress.selectedLevelData.rewards) {
            rewards.Add(new CurrencyUIReward(currencyReward.Key, currencyReward.Value));
        }

        return rewards;
    }
}
