using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Specialized;

[Serializable]
public class User
{
    public string id { get; set; }

    public string username { get; set; }

    public List<Unit> units { get; set; }

    public List<Item> items { get; set; }

    public int next_unit_id;

    public Dictionary<Currency, int> currencies;

    public int level = 1;

    public int experience = 0;

    public int experienceToNextLevel = 100;

    public DateTime lastAfkRewardClaim = DateTime.Now;
    public DateTime lastAfkRewardAccum = DateTime.Now;

    public Dictionary<Currency, int> accumulatedCurrencyReward = new Dictionary<Currency, int>();

    public int accumulatedExperienceReward = 0;

    public Dictionary<Currency, int> afkMaxCurrencyReward = new Dictionary<Currency, int>();
    public int afkMaxExperienceReward = 0;

	public List<(string superCampaignId, string levelId)> superCampaignsProgress = new List<(string, string)>();
}
