using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UIModelManager : MonoBehaviour
{
    [SerializeField]
    GameObject playerModelContainer;

    private float timer = 0f;
    const float ANIMATION_INTERVAL = 5f;
    // float animationClipDuration;

    public void SetModel(GameObject playerModel)
    {
        GameObject modelClone = Instantiate(
            playerModel,
            playerModelContainer.transform
        );
        // animationClipDuration = AnimationClipTime(modelClone.GetComponentInChildren<Animator>());
        // StartCoroutine(AnimateCharacter(modelClone));
    }

    public void RemoveCurrentModel()
    {
        if (playerModelContainer.transform.childCount > 0)
        {
            Destroy(playerModelContainer.transform.GetChild(0).gameObject);
        }
    }

    float AnimationClipTime(Animator modelClone)
    {
        List<AnimationClip> clips = modelClone.runtimeAnimatorController.animationClips.ToList();
        foreach(AnimationClip clip in clips) {
            print(clip.name);
        }
        return clips.Single(clip => clip.name == "GR6_Victory").length;
    }

    IEnumerator AnimateCharacter(GameObject modelClone)
    {
        while (true)
        {
            timer += Time.deltaTime;

            if(timer >= ANIMATION_INTERVAL)
            {
                modelClone.GetComponentInChildren<Animator>().SetBool("BreakIdle", true);    
            }
            yield return new WaitForSeconds(1f);
            modelClone.GetComponentInChildren<Animator>().SetBool("BreakIdle", true);
            print("BreakIdle -> true");
            yield return new WaitForSeconds(.5f);
            modelClone.GetComponentInChildren<Animator>().SetBool("BreakIdle", false);
            print("BreakIdle -> false");
            yield return new WaitForSeconds(ANIMATION_INTERVAL);
        }
    }
}
