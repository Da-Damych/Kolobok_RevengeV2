using UnityEngine;
using UnityEngine.UI;

namespace Game.FinalScreen
{

    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class QuitGame : MonoBehaviour
    {
        [Tooltip("Button на этом же объекте. Если пусто — найдётся автоматически.")]
        [SerializeField] private Button button;

        [Tooltip("Скрывать финальный экран перед выходом (актуально для WebGL/браузерных сборок).")]
        [SerializeField] private FinalScreenUI finalScreenUI;

        [Tooltip("Секунд задержки перед реальным выходом — чтобы успел проиграться звук/анимация.")]
        [Range(0f, 3f)] [SerializeField] private float delay = 0f;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (finalScreenUI != null) finalScreenUI.Hide(instant: true);
            Time.timeScale = 1f; // важно для корректного OnApplicationQuit / WebGL

            if (delay > 0f) Invoke(nameof(QuitApplication), delay);
            else QuitApplication();
        }
        
        private void QuitApplication()
        {
            #if UNITY_EDITOR
                // В редакторе Application.Quit() не работает — эмулируем остановку плей-мода.
                Debug.Log("[QuitGameButton] Выход из игры (в редакторе — остановка Play Mode).");
                UnityEditor.EditorApplication.isPlaying = false;
            #elif UNITY_WEBGL
                Debug.Log("[QuitGameButton] WebGL: выход не поддерживается, открываем javascript-хук.");
                // Если на странице есть функция — дёргаем её, например window.unityQuitGame().
                // Application.ExternalCall("unityQuitGame");
            #else
                Debug.Log("[QuitGameButton] Application.Quit()");
                Application.Quit();
            #endif
        }
    }
}