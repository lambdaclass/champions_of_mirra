using System.Collections;
using UnityEngine;


public class LookAtController : MonoBehaviour
{
    [SerializeField] private Transform lookAtRoot = null;

    private IEnumerator looking_coroutine = null;
    private void Start()
    {
        startLookingCoroutine();
    }

    private void OnEnable()
    {
        startLookingCoroutine();
    }

    private void OnDisable()
    {
        startLookingCoroutine();
    }

    private void stopLookingCoroutine()
    {
        if (looking_coroutine != null)
            StopCoroutine(looking_coroutine);
    }

    private void startLookingCoroutine()
    {
        stopLookingCoroutine();

        looking_coroutine = impl();
        StartCoroutine(looking_coroutine);

        IEnumerator impl()
        {
            while(true)
            {
              yield return null;
              transform.LookAt(lookAtRoot);
            }
        }
    }
}
