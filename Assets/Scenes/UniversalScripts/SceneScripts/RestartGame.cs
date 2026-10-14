using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.FinalScreen
{
    /// <summary>Какую сцену перезапускать по кнопке "Заново".</summary>
    public enum RestartSceneMode
    {
        /// <summary>Перезагрузить текущую сцену (Active Scene).</summary>
        CurrentScene,
        /// <summary>Перезагрузить сцену с указанным именем.</summary>
        SceneByName,
        /// <summary>Перезагрузить сцену с указанным индексом в Build Settings.</summary>
        SceneByIndex
    }

    /// <summary>
    /// Скрипт №3 — повтор игры (перезапуск сцены).
    /// Вешается на кнопку "Заново". Сам подписывается на onClick в Awake.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class RestartGameButton : MonoBehaviour
    {
        [Tooltip("Button на этом же объекте. Если пусто — найдётся автоматически.")]
        [SerializeField] private Button button;

        [Header("Что перезапускать")]
        [SerializeField] private RestartSceneMode restartMode = RestartSceneMode.CurrentScene;
        [Tooltip("Имя сцены (должна быть добавлена в File → Build Settings).")]
        [SerializeField] private string sceneName;
        [Tooltip("Индекс сцены в Build Settings (0 = первая сцена).")]
        [SerializeField] private int sceneIndex = 0;

        [Header("Дополнительно")]
        [Tooltip("Скрыть финальный экран перед перезагрузкой.")]
        [SerializeField] private FinalScreenUI finalScreenUI;

        [Tooltip("Восстановить Time.timeScale = 1 перед загрузкой (обязательно, если экран ставил игру на паузу).")]
        [SerializeField] private bool restoreTimeScale = true;

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
            if (restoreTimeScale) Time.timeScale = 1f;

            Restart();
        }

        /// <summary>Перезапуск сцены — можно вызывать и из другого кода (например, по клавише R).</summary>
        public void Restart()
        {
            switch (restartMode)
            {
                case RestartSceneMode.SceneByName:
                    if (string.IsNullOrEmpty(sceneName))
                    {
                        Debug.LogError("[RestartGameButton] Не указано имя сцены — перезагружаю текущую.");
                        ReloadCurrentScene();
                        return;
                    }
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                    break;

                case RestartSceneMode.SceneByIndex:
                    SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
                    break;

                default:
                    ReloadCurrentScene();
                    break;
            }
        }

        private void ReloadCurrentScene()
        {
            var current = SceneManager.GetActiveScene();
            if (!current.IsValid())
            {
                Debug.LogError("[RestartGameButton] Активная сцена не найдена.");
                return;
            }
            SceneManager.LoadScene(current.buildIndex, LoadSceneMode.Single);
        }

        private void OnValidate()
        {
            if (restartMode == RestartSceneMode.SceneByIndex && sceneIndex < 0) sceneIndex = 0;
        }
    }
}
