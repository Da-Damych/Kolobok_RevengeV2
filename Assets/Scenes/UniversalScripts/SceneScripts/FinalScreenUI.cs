using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.FinalScreen
{
    [DisallowMultipleComponent]
    public class FinalScreenUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Панель финального экрана. Если пусто — найдётся автоматически среди детей.")]
        [SerializeField] private GameObject screenRoot;

        [Tooltip("Необязательно: CanvasGroup на панели — для плавного появления.")]
        [SerializeField] private CanvasGroup screenCanvasGroup;

        [Header("Игрок")]
        [Tooltip("Трансформ игрока. Если пусто — возьмётся из FinalScreenTrigger или по тегу Player.")]
        [SerializeField] private Transform player;

        [Tooltip("Отключать компоненты игрока, пока экран открыт " +
                 "(нужно, чтобы камера перестала вращаться при timeScale = 0).")]
        [SerializeField] private bool disablePlayerComponents = true;

        [Tooltip("Компоненты, которые НЕ надо отключать (например, сам скрипт показа экрана, " +
                 "если он висит на игроке).")]
        [SerializeField] private List<MonoBehaviour> componentsToKeepEnabled = new List<MonoBehaviour>();

        [Header("Поведение")]
        [SerializeField] private bool pauseGame = true;
        [SerializeField] private bool controlCursor = true;

        [Header("Анимация появления")]
        [SerializeField] private bool fade = true;
        [Range(0f, 3f)] [SerializeField] private float fadeSpeed = 6f;

        [Header("Отладка")]
        [Tooltip("Клавиша для ручного переключения экрана в Play Mode.")]
        [SerializeField] private KeyCode debugToggleKey = KeyCode.F;
        [SerializeField] private bool debugLogs = true;

        public event Action<bool> OnScreenVisibilityChanged;
        public bool IsVisible { get; private set; }

        private float _timeScaleBeforePause = 1f;
        private bool _wasPausedByUs;
        private float _currentAlpha = 1f;
        private readonly List<MonoBehaviour> _disabledByUs = new List<MonoBehaviour>();
        private bool _validated;

        // ============================ LIFECYCLE ============================

        private void Awake()
        {
            ValidateSetup(force: true);
            Hide(instant: true); // на старте экран всегда скрыт
        }

        private void Start()
        {
            ValidateSetup(force: true);
        }

        private void Update()
        {
            if (debugToggleKey != KeyCode.None && LegacyKeyDown(debugToggleKey))
                Toggle();

            if (!fade || screenCanvasGroup == null || !IsVisible) return;
            if (Mathf.Approximately(_currentAlpha, 1f)) return;

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 1f, fadeSpeed * Time.unscaledDeltaTime);
            ApplyAlpha(_currentAlpha);
        }

        // ============================ ПОКАЗ ============================

        public void Show()
        {
            ValidateSetup();

            if (IsVisible)
            {
                Log($"Show(): экран уже открыт — повторный вызов игнорируется.");
                return;
            }

            IsVisible = true;
            ActivateScreenHierarchy();

            if (pauseGame)
            {
                _timeScaleBeforePause = Time.timeScale;
                _wasPausedByUs = true;
                Time.timeScale = 0f;
            }

            if (controlCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (disablePlayerComponents) SetPlayerComponentsEnabled(false);

            _currentAlpha = (fade && screenCanvasGroup != null) ? 0f : 1f;
            ApplyAlpha(_currentAlpha);

            Log($"Show(): экран открыт. activeInHierarchy={ScreenActiveInHierarchy()}, " +
                $"timeScale={Time.timeScale}, player={(player != null ? player.name : "null")}");

            OnScreenVisibilityChanged?.Invoke(true);
        }

        // ============================ СКРЫТИЕ ============================

        public void Hide() => Hide(false);

        public void Hide(bool instant)
        {
            if (!instant && !IsVisible) return;

            IsVisible = false;

            if (screenRoot != null) screenRoot.SetActive(false);
            if (screenCanvasGroup != null) ApplyAlpha(1f);

            if (disablePlayerComponents) SetPlayerComponentsEnabled(true);

            RestoreTimeScale();
            Log("Hide(): экран закрыт, timeScale=" + Time.timeScale);

            OnScreenVisibilityChanged?.Invoke(false);
        }

        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        // ============================ АКТИВАЦИЯ ПАНЕЛИ ============================

        /// <summary>
        /// Включает панель И всех её неактивных родителей (кроме сцены).
        /// Без этого SetActive(true) на ребёнке не даст видимого результата,
        /// если Canvas выключен.
        /// </summary>
        private void ActivateScreenHierarchy()
        {
            if (screenRoot == null) return;

            Transform t = screenRoot.transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                {
                    Log($"Включаю неактивного родителя панели: '{t.name}'.");
                    t.gameObject.SetActive(true);
                }
                t = t.parent;
            }
        }

        private bool ScreenActiveInHierarchy()
        {
            return screenRoot != null && screenRoot.activeInHierarchy;
        }

        // ============================ ИГРОК ============================

        private void SetPlayerComponentsEnabled(bool enabled)
        {
            if (enabled)
            {
                foreach (var mb in _disabledByUs)
                    if (mb != null) mb.enabled = true;
                _disabledByUs.Clear();
                return;
            }

            if (player == null)
            {
                Log("SetPlayerComponentsEnabled: игрок не найден — камера продолжит вращаться. " +
                    "Назначьте поле Player в инспекторе FinalScreenUI.");
                return;
            }

            _disabledByUs.Clear();
            foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || !mb.enabled) continue;
                if (mb == this) continue;                                  // сам показ экрана
                if (componentsToKeepEnabled.Contains(mb)) continue;        // белый список
                if (mb is FinalScreenUI || mb is FinalScreenTrigger) continue;
                if (mb is UnityEngine.UI.Graphic || mb is Canvas || mb is CanvasGroup) continue;

                mb.enabled = false;
                _disabledByUs.Add(mb);
            }

            Log($"Отключено компонентов игрока: {_disabledByUs.Count}");
        }

        // ============================ СЛУЖЕБНОЕ ============================

        /// <summary>
        /// Обёртка над старым Input Manager: не бросает исключение,
        /// если в проекте Active Input Handling = Input System Package (Only).
        /// </summary>
        private static bool LegacyKeyDown(KeyCode key)
        {
            try { return Input.GetKeyDown(key); }
            catch (System.Exception) { return false; }
        }

        private void RestoreTimeScale()
        {
            if (!_wasPausedByUs) return;
            _wasPausedByUs = false;
            if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = _timeScaleBeforePause;
        }

        private void ApplyAlpha(float value)
        {
            if (screenCanvasGroup == null) return;
            screenCanvasGroup.alpha = value;
            screenCanvasGroup.blocksRaycasts = value > 0.01f;
            screenCanvasGroup.interactable = value > 0.01f;
        }

        private void OnDisable()
        {
            if (_wasPausedByUs) Time.timeScale = _timeScaleBeforePause;
            if (_disabledByUs.Count > 0) SetPlayerComponentsEnabled(true);
        }

        /// <summary>
        /// Автоназначение ссылок + понятные предупреждения, если что-то не настроено.
        /// </summary>
        private void ValidateSetup(bool force = false)
        {
            if (_validated && !force) return;
            _validated = true;

            // 1. Панель
            if (screenRoot == null)
            {
                var cg = GetComponentInChildren<CanvasGroup>(true);
                screenRoot = cg != null ? cg.gameObject : null;

                if (screenRoot == null)
                {
                    var canvas = GetComponentInChildren<Canvas>(true);
                    if (canvas != null && canvas.gameObject != gameObject) screenRoot = canvas.gameObject;
                }

                if (screenRoot == null)
                {
                    Debug.LogWarning("[FinalScreenUI] Screen Root не назначен и не найден среди детей. " +
                                     "Перетащите панель FinalScreen в поле Screen Root.", this);
                }
                else
                {
                    Debug.LogWarning($"[FinalScreenUI] Screen Root не был назначен — найден автоматически: " +
                                     $"'{GetPath(screenRoot.transform)}'. Проверьте, что это нужная панель.", this);
                }
            }

            // 2. CanvasGroup
            if (screenCanvasGroup == null && screenRoot != null)
                screenCanvasGroup = screenRoot.GetComponent<CanvasGroup>();

            // 3. Панель не должна быть этим же объектом (иначе скрипт отключится вместе с ней)
            if (screenRoot == gameObject)
            {
                Debug.LogWarning("[FinalScreenUI] Screen Root = тот же объект, на котором висит этот скрипт. " +
                                 "При Hide() скрипт отключится вместе с панелью. " +
                                 "Перенесите скрипт на Canvas, а в Screen Root укажите панель.", this);
            }

            // 4. Игрок
            if (player == null)
            {
                var trigger = FindFirstObjectByType<FinalScreenTrigger>();
                if (trigger != null && trigger.Player != null) player = trigger.Player;

                if (player == null)
                {
                    var go = GameObject.FindGameObjectWithTag("Player");
                    if (go != null) player = go.transform;
                }

                if (player == null)
                    Debug.LogWarning("[FinalScreenUI] Игрок не найден (тег 'Player' отсутствует). " +
                                     "Назначьте поле Player вручную, иначе камера будет вращаться на паузе.", this);
            }

            // 5. Дубликаты в сцене — частая причина «экран виден сразу»
            var all = FindObjectsByType<FinalScreenUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length > 1)
            {
                var names = new System.Text.StringBuilder();
                foreach (var f in all) names.Append(GetPath(f.transform)).Append(" | ");
                Debug.LogWarning($"[FinalScreenUI] В сцене {all.Length} экземпляра FinalScreenUI: {names}. " +
                                 $"Оставьте один — иначе панели будут конфликтовать.", this);
            }
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }

        private void Log(string msg)
        {
            if (debugLogs) Debug.Log($"[FinalScreenUI] {msg}", this);
        }

        private void OnValidate()
        {
            if (screenCanvasGroup == null && screenRoot != null)
                screenCanvasGroup = screenRoot.GetComponent<CanvasGroup>();
        }
    }
}
