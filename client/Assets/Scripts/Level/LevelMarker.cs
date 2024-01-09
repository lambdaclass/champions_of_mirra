using UnityEngine;

public class LevelMarker : MonoBehaviour
{
    [SerializeField]
    GameObject PlayButton;

    public void SelectLevel() {
        PlayButton.SetActive(true);
    }
}
