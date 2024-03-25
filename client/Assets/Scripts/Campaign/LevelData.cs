using System.Collections;
using System.Collections.Generic;

public class LevelData
{
    // This fields shouldn't be modifiable externally (they shouldn't be public?)
    public string id;
    public int levelNumber;
    public string campaignId;
    public List<Unit> units;

    public Dictionary<Currency, int> rewards = new Dictionary<Currency, int>();

    // AFK Rewards rate granted
    // These are how many a player makes in the maximum timespan available (12h now)
    public Dictionary<Currency, int> afkCurrencyRate = new Dictionary<Currency, int>();

    public int afkExperienceRate;

    public int experienceReward;

    public LevelProgress.Status status;
}
