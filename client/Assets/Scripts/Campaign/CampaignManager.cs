using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampaignManager : MonoBehaviour
{
    public static GameObject campaignPrefab;
    
    public static Campaign selectedCampaignData;
    
    [SerializeField]
    GameObject screenLocker;

    void Start() {
        LevelProgress.selectedLevelData = null;
		LevelProgress.nextLevelData = null;
		GameObject campaignGameObject = Instantiate(campaignPrefab, transform);
        campaignGameObject.transform.SetSiblingIndex(0);
        var campaignLevelManager = campaignGameObject.GetComponentInChildren<CampaignLevelsManager>();
        campaignLevelManager.AssignLevelsData(selectedCampaignData.levels);
    }
}
