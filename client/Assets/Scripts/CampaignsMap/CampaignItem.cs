using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CampaignItem : MonoBehaviour
{
    [SerializeField]
    GameObject lockObject;

    [SerializeField]
    GameObject completedCrossObject;

    [SerializeField]
    GameObject campaignToShowPrefab;

    private Campaign campaignData;

    public LevelManager sceneManager;

    // Load campaign scene if its unlocked. Scene needs to have the same name as our campaign object.
    public void SelectCampaign()
    {
        if (campaignData.status == LevelProgress.Status.Unlocked) {
            CampaignManager.selectedCampaignData = campaignData;
            CampaignManager.campaignPrefab = campaignToShowPrefab;
            sceneManager.ChangeToSceneWithDelay("Campaign");
        }
    }

    public void SetCampaignData(Campaign campaignData)
    {
        this.campaignData = campaignData;
        switch(this.campaignData.status) 
        {
            case LevelProgress.Status.Locked:
                break;
            case LevelProgress.Status.Unlocked:
				lockObject.SetActive(false);
                break;
            case LevelProgress.Status.Completed:
                completedCrossObject.SetActive(true);
				lockObject.SetActive(false);
                break;
        }
        gameObject.GetComponent<Button>().onClick.AddListener(() => SelectCampaign());
    }
}
