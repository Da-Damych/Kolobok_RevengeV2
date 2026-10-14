using UnityEngine;

namespace Game.FinalScreen
{
    /// <summary>
    /// Триггер финального экрана. Вешается на объект с Collider (Is Trigger).
    /// Когда в зону входит игрок — вызывает FinalScreenUI.Show().
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class FinalScreenTrigger : MonoBehaviour
    {
        [SerializeField] private FinalScreenUI finalScreenUI;

        [Header("Кого считаем игроком")]
        [SerializeField] private DetectPlayerMode detectMode = DetectPlayerMode.ByTag;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private LayerMask playerLayer = ~0;
        [SerializeField] private Transform playerReference;

        [Header("Поведение")]
        [SerializeField] private bool onceOnly = true;
        [SerializeField] private bool hideOnExit = false;
        [SerializeField] private AudioSource triggerSound;

        [Header("Отладка")]
        [SerializeField] private bool debugLogs = true;

        private bool _used;

        /// <summary>Трансформ игрока, который последним вошёл в зону (для FinalScreenUI).</summary>
        public Transform Player { get; private set; }

        public enum DetectPlayerMode { ByTag, ByLayer, ByReference }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            if (finalScreenUI == null) finalScreenUI = FindFirstObjectByType<FinalScreenUI>();
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning("[FinalScreenTrigger] У Collider не включён Is Trigger — " +
                                 "вход в зону не определится как триггер (игрок упрётся в стену). Включаю сам.", this);
                col.isTrigger = true;
            }

            if (finalScreenUI == null)
            {
                finalScreenUI = FindFirstObjectByType<FinalScreenUI>();
                if (finalScreenUI != null)
                    Debug.LogWarning($"[FinalScreenTrigger] Final Screen UI не назначен — найден автоматически: " +
                                     $"'{finalScreenUI.name}'. Лучше перетащить ссылку вручную.", this);
                else
                    Debug.LogError("[FinalScreenTrigger] Final Screen UI не назначен и не найден в сцене. " +
                                   "Экран показан не будет.", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_used && onceOnly) return;
            if (!IsPlayer(other.transform))
            {
                Log($"OnTriggerEnter: '{other.name}' — не игрок, пропускаю.");
                return;
            }

            _used = true;
            Player = other.transform.root;
            Log($"OnTriggerEnter: игрок '{other.name}' вошёл в зону → показываю экран.");

            if (triggerSound != null) triggerSound.Play();

            if (finalScreenUI != null) finalScreenUI.Show();
            else Debug.LogError("[FinalScreenTrigger] finalScreenUI == null — экран не показан.", this);

            if (onceOnly)
            {
                var col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!hideOnExit) return;
            if (!IsPlayer(other.transform)) return;
            Log($"OnTriggerExit: игрок '{other.name}' покинул зону → скрываю экран.");
            if (finalScreenUI != null) finalScreenUI.Hide();
        }

        private bool IsPlayer(Transform t)
        {
            switch (detectMode)
            {
                case DetectPlayerMode.ByReference:
                    return playerReference != null &&
                           (t == playerReference || t.IsChildOf(playerReference) || playerReference.IsChildOf(t));

                case DetectPlayerMode.ByLayer:
                    return (playerLayer.value & (1 << t.gameObject.layer)) != 0;

                default:
                    return t.CompareTag(playerTag) || IsChildWithTag(t, playerTag);
            }
        }

        private static bool IsChildWithTag(Transform t, string tag)
        {
            foreach (Transform child in t)
                if (child.CompareTag(tag)) return true;
            return false;
        }

        private void Log(string msg)
        {
            if (debugLogs) Debug.Log($"[FinalScreenTrigger] {msg}", this);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(playerTag)) playerTag = "Player";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _used ? Color.gray : new Color(0f, 1f, 0.4f, 0.6f);
            var col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(box.center), transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(Vector3.zero, box.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 0.75f);
            }
        }
    }
}
