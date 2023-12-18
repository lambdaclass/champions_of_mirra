using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public void ChangeToScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }
}
