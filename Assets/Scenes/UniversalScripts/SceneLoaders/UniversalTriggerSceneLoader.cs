using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class UniversalTriggerSceneLoader : MonoBehaviour
{
    [SerializeField] private int sceneIndex = 1;

    private bool hasLoaded;

    private void OnTriggerEnter(Collider other)
    {
        if (hasLoaded)
            return;

        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Указан несуществующий индекс сцены ({sceneIndex})");
            return;
        }

        hasLoaded = true;
        SceneManager.LoadScene(sceneIndex);
    }
}