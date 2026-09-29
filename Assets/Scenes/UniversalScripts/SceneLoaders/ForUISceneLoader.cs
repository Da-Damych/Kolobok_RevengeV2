using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private int sceneIndex = 1;

    public void LoadSceneIndex()
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Указан несуществующий индекс сцены ({sceneIndex})");
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }
}
