using UnityEngine;

// ВАЖНО: неймспейс НЕ должен называться Light — иначе тип UnityEngine.Light
// перекрывается именем неймспейса (ошибка CS0118).
namespace Game.Lighting
{
    /// <summary>
    /// Факел: тусклый мерцающий точечный свет + затемнение сцены (туман/ambient) + звук.
    /// </summary>
    [DisallowMultipleComponent]
    public class TorchLight : MonoBehaviour
    {
        [Header("Свет")]
        [Tooltip("Свой Light. Если пусто — создастся автоматически как дочерний объект.")]
        [SerializeField] private Light torchLight;

        [SerializeField] private Color lightColor = new Color(1f, 0.62f, 0.28f);

        [Tooltip("Базовая яркость. Факел должен светить ТУСКЛО: 0.6–1.5 для URP/HDRP, 1–3 для Built-in.")]
        [Range(0f, 8f)] [SerializeField] private float baseIntensity = 1.1f;

        [Tooltip("Радиус освещения в метрах. 4–7 = видно только рядом с игроком.")]
        [Range(0.5f, 30f)] [SerializeField] private float radius = 5.5f;

        [Tooltip("Смещение источника света относительно этого объекта (чтобы свет был у конца факела, а не в кисти).")]
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.35f, 0.25f);

        [Header("Мерцание")]
        [Tooltip("Насколько сильно плавает яркость (0 — ровный свет, 1 — сильно дёргается).")]
        [Range(0f, 1f)] [SerializeField] private float flickerAmount = 0.35f;

        [Tooltip("Скорость мерцания.")]
        [Range(0.1f, 8f)] [SerializeField] private float flickerSpeed = 2.2f;

        [Tooltip("Порог «порыва ветра»: когда шум ниже значения, факел резко тускнеет.")]
        [Range(0f, 1f)] [SerializeField] private float gustThreshold = 0.22f;

        [Tooltip("Во сколько раз гаснет свет во время порыва.")]
        [Range(0.05f, 1f)] [SerializeField] private float gustDim = 0.45f;

        [Tooltip("Лёгкое дрожание позиции огня.")]
        [Range(0f, 0.1f)] [SerializeField] private float positionJitter = 0.012f;

        [Header("Общая темнота сцены")]
        [Tooltip("Включить тёмный туман — он скрывает всё, что дальше radius.")]
        [SerializeField] private bool useFog = true;

        [SerializeField] private Color fogColor = new Color(0.012f, 0.014f, 0.02f);

        [Tooltip("Плотность тумана (режим ExponentialSquared). 0.05–0.12 = почти полная тьма вдалеке.")]
        [Range(0f, 0.5f)] [SerializeField] private float fogDensity = 0.08f;

        [Tooltip("Обнулить Ambient (небо не подсвечивает сцену) и отключить Directional Light.")]
        [SerializeField] private bool killAmbientAndSun = true;

        [Header("Звук (необязательно)")]
        [Tooltip("AudioSource с зацикленным потрескиванием огня.")]
        [SerializeField] private AudioSource crackleLoop;

        [Header("Состояние")]
        [SerializeField] private bool startLit = true;

        /// <summary>true, если факел сейчас горит.</summary>
        public bool IsLit { get; private set; }

        /// <summary>Текущая яркость (удобно для других скриптов — например, враги видят игрока по свету).</summary>
        public float CurrentIntensity { get; private set; }

        private float _noiseOffset;
        private Vector3 _baseLocalPos;
        private AmbientSettings _savedAmbient;
        private Light _savedSun;
        private bool _sceneSettingsApplied;

        private struct AmbientSettings
        {
            public Color color;
            public float intensity;
            public FogMode mode;
            public bool fog;
            public Color fogColor;
            public float fogDensity;
        }

        // ============================ LIFECYCLE ============================

        private void Awake()
        {
            _noiseOffset = Random.value * 1000f;

            if (torchLight == null) torchLight = GetComponentInChildren<Light>(true);

            if (torchLight == null)
            {
                var go = new GameObject("TorchFire");
                go.transform.SetParent(transform, false);
                torchLight = go.AddComponent<Light>();
                Debug.Log("[TorchLight] Свет не был назначен — создан дочерний объект 'TorchFire'.", this);
            }

            // Сначала запоминаем базовую позицию, ПОТОМ применяем localOffset.
            // (Раньше ConfigureLight вызывался раньше присвоения _baseLocalPos,
            //  из-за чего offset накапливался/терялся.)
            _baseLocalPos = torchLight.transform.localPosition;

            ConfigureLight();

            SetLit(startLit, instant: true);
        }

        private void OnEnable()
        {
            // Затемняем сцену только когда факел реально горит.
            if (IsLit && (useFog || killAmbientAndSun)) ApplySceneDarkness();
        }

        private void OnDisable()
        {
            RestoreSceneSettings();
        }

        private void OnDestroy()
        {
            RestoreSceneSettings();
        }

        private void Update()
        {
            if (!IsLit || torchLight == null) return;

            // --- мерцание на основе шума Перлина (два слоя = живой огонь) ---
            float t = Time.time * flickerSpeed + _noiseOffset;
            float n1 = Mathf.PerlinNoise(t, 0f);                 // медленная волна
            float n2 = Mathf.PerlinNoise(0f, t * 2.7f);          // быстрая составляющая
            float noise = n1 * 0.65f + n2 * 0.35f;               // 0..1

            float multiplier = 1f;

            // «порыв ветра» — редкое резкое падение яркости
            if (noise < gustThreshold)
            {
                float k = Mathf.InverseLerp(gustThreshold, 0f, noise);   // 0..1, чем тише — тем сильнее гаснет
                multiplier = Mathf.Lerp(1f, gustDim, k);
            }

            float flicker = Mathf.Lerp(1f, 0.55f + noise, flickerAmount);
            CurrentIntensity = baseIntensity * flicker * multiplier;
            torchLight.intensity = CurrentIntensity;

            // --- дрожание позиции ---
            if (positionJitter > 0f)
            {
                torchLight.transform.localPosition = _baseLocalPos + localOffset + new Vector3(
                    (Mathf.PerlinNoise(t * 1.3f, 11f) - 0.5f) * positionJitter,
                    (Mathf.PerlinNoise(t * 1.7f, 23f) - 0.5f) * positionJitter,
                    (Mathf.PerlinNoise(t * 1.1f, 37f) - 0.5f) * positionJitter);
            }
        }

        // ============================ НАСТРОЙКА СВЕТА ============================

        private void ConfigureLight()
        {
            torchLight.type = LightType.Point;
            torchLight.color = lightColor;
            torchLight.range = radius;
            torchLight.intensity = baseIntensity;
            torchLight.shadows = LightShadows.Soft;
            torchLight.shadowStrength = 0.85f;
            torchLight.renderMode = LightRenderMode.ForcePixel;   // ровный свет на близкой геометрии
            torchLight.cullingMask = ~0;
            torchLight.transform.localPosition = _baseLocalPos + localOffset;

            // URP: Additional Lights должны быть включены в URP Asset (Per Pixel),
            // иначе этот свет не отрендерится. У Additional Light мягкие тени дороже —
            // при просадках FPS поставьте Hard или None.
            // HDRP: intensity задаётся в люксах — 1.1 будет почти невидимо, ставьте ~5000–20000
            // или включите ev100/physical units по своему проекту.
        }

        // ============================ ТЕМНОТА СЦЕНЫ ============================

        private void ApplySceneDarkness()
        {
            if (_sceneSettingsApplied) return;

            _savedAmbient = new AmbientSettings
            {
                color = RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat
                          ? RenderSettings.ambientLight : Color.gray,
                intensity = RenderSettings.ambientIntensity,
                mode = RenderSettings.fogMode,
                fog = RenderSettings.fog,
                fogColor = RenderSettings.fogColor,
                fogDensity = RenderSettings.fogDensity
            };

            if (killAmbientAndSun)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black;
                RenderSettings.ambientIntensity = 0f;

                // гасим направленное «солнце», иначе оно подсветит сцену
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.type == LightType.Directional && l.enabled)
                    {
                        if (_savedSun == null) _savedSun = l;
                        l.enabled = false;
                    }
                }
            }

            if (useFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
            }

            _sceneSettingsApplied = true;
        }

        private void RestoreSceneSettings()
        {
            if (!_sceneSettingsApplied) return;

            RenderSettings.ambientLight = _savedAmbient.color;
            RenderSettings.ambientIntensity = _savedAmbient.intensity;
            RenderSettings.fog = _savedAmbient.fog;
            RenderSettings.fogMode = _savedAmbient.mode;
            RenderSettings.fogColor = _savedAmbient.fogColor;
            RenderSettings.fogDensity = _savedAmbient.fogDensity;

            if (_savedSun != null) _savedSun.enabled = true;

            _sceneSettingsApplied = false;
        }

        // ============================ ПУБЛИЧНОЕ API ============================

        /// <summary>Зажечь или потушить факел.</summary>
        public void SetLit(bool lit, bool instant = false)
        {
            IsLit = lit;

            if (torchLight != null)
            {
                torchLight.enabled = lit;
                torchLight.intensity = lit ? baseIntensity : 0f;
            }

            if (crackleLoop != null)
            {
                if (lit && !crackleLoop.isPlaying) crackleLoop.Play();
                else if (!lit && crackleLoop.isPlaying) crackleLoop.Stop();
            }

            if (lit)
            {
                if (!instant && isActiveAndEnabled && (useFog || killAmbientAndSun)) ApplySceneDarkness();
            }
            else
            {
                CurrentIntensity = 0f;
                RestoreSceneSettings();   // потушили факел — возвращаем свет сцены
            }
        }

        public void Toggle() => SetLit(!IsLit);

        /// <summary>Плавно изменить радиус (например, факел «разгорается» после подбора предмета).</summary>
        public void SetRadius(float newRadius, float seconds = 1f)
        {
            radius = Mathf.Max(0.1f, newRadius);
            if (torchLight == null) return;

            if (seconds <= 0f) torchLight.range = radius;
            else StartCoroutine(AnimateRange(torchLight.range, radius, seconds));
        }

        private System.Collections.IEnumerator AnimateRange(float from, float to, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                if (torchLight == null) yield break;
                torchLight.range = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            if (torchLight != null) torchLight.range = to;
        }

        private void OnValidate()
        {
            if (torchLight == null) return;

            torchLight.color = lightColor;
            torchLight.range = radius;
            torchLight.intensity = baseIntensity;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
            Vector3 center = torchLight != null
                ? torchLight.transform.position
                : transform.TransformPoint(localOffset);
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}
