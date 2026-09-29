using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class OnlyPlayerTriggerSceneLoader : MonoBehaviour
{
    [SerializeField] private int sceneIndex = 1;

    private bool hasLoaded;

    private void OnTriggerEnter(Collider other)
    {
        if (hasLoaded)
            return;

        if (!other.CompareTag("Player"))
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