using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScriptFinalScene : MonoBehaviour
{
    public GameObject canvas;

    public bool pauseGame = true;
    public bool showCursor = true;
    public int restartSceneIndex = 0;
    public string playerTag = "Player";

    private void Start()
    {
        if (canvas != null)
            canvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            return;

        ShowPotracheno();
    }

    private void ShowPotracheno()
    {
        if (canvas == null) return;

        canvas.SetActive(true);

        if (pauseGame)
            Time.timeScale = 0f;

        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(restartSceneIndex);
    }

    private void Quit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
