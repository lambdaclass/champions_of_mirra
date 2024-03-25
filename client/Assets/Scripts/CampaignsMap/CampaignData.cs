using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Campaign
{
	public string campaignId;
	public int campaignNumber;
    public LevelProgress.Status status = LevelProgress.Status.Locked;
    public List<LevelData> levels;
}
