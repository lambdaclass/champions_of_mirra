using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStates : MonoBehaviour
{
    [SerializeField]
    List<StateInfo> states;

    [SerializeField]
    GameObject statesContainer;

    [SerializeField]
    GameObject StateItem;

    public void DisplayStateIcon(ulong id, string stateName, bool isActive)
    {
        if (id == SocketConnectionManager.Instance.playerId)
        {
            GameObject item = null;
            if (isActive)
            {
                StateInfo state = GetStateById(stateName);
                item = Instantiate(StateItem, statesContainer.transform);
                item.GetComponent<Image>().sprite = state.image;
                item.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }
            StartCoroutine(RemoveIconState(!isActive, item));
        }
    }

    private StateInfo GetStateById(string stateName)
    {
        return states.Find(el => el.name == stateName);
    }

    //TODO : remove state
    IEnumerator RemoveIconState(bool duration, GameObject item)
    {
        yield return new WaitUntil(() => duration == false);
        Destroy(item);
        print("termino el poison" + duration);
    }
}
