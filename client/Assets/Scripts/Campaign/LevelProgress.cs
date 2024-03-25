using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public static class LevelProgress
{
    public enum Status
    {
        Locked,
        Unlocked,
        Completed
	}

	public static List<Campaign> campaigns;

	public static LevelData selectedLevelData;
	public static LevelData nextLevelData;

	public static LevelData NextLevel(LevelData level)
	{
		Campaign currentCampaign = campaigns.Find(campaign => campaign.campaignId == level.campaignId);
		int levelIndex = currentCampaign.levels.FindIndex(lvl => lvl.levelNumber == level.levelNumber + 1);
		if(levelIndex != -1) {
			return currentCampaign.levels[levelIndex];
		}
		if(campaigns.Any(campaign => campaign.campaignNumber == currentCampaign.campaignNumber + 1)) {
			return campaigns.Find(campaign => campaign.campaignNumber == currentCampaign.campaignNumber + 1).levels.Find(lvl => lvl.levelNumber == 1);
		}
		
		Debug.Log("There are no more campaigns after this one");
		return null;
	}
}
