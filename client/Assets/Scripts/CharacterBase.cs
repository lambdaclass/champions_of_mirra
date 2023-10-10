using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBase : MonoBehaviour
{
    [SerializeField]
    public GameObject PlayerName;

    [SerializeField]
    public GameObject Hitbox;

    [SerializeField]
    public GameObject FeedbackContainer;

    [SerializeField]
    public GameObject AimDirection;

    [SerializeField]
    public GameObject SkillRange;

    [SerializeField]
    public GameObject spawnFeedback;

    [SerializeField]
    public GameObject HealthBar;

    public void activateSpawnFeedback()
    {
        spawnFeedback.SetActive(true);
    }

    void Awake()
    {
        activateSpawnFeedback();
    }
}
