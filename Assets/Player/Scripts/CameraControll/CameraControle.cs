using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CameraControle : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float sensitivity = 0.2f;
    [SerializeField] private float distance = 4f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;
    
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float cameraRadius = 0.3f;
    [SerializeField] private float wallPadding = 0.1f;
    private float currectDistance;


    private float pitch = 0f;
    private float yaw = 0f;
    private Vector2 moveInput;

    private void Start()
    {
        if (target.transform == null)
        {
            target = Camera.main.transform;
        }

        currectDistance = distance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensitivity;

        yaw += mouseDelta.x;
        pitch -= mouseDelta.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 direction = rotation * Vector3.back;

        RaycastHit hit;
        bool blocked = Physics.SphereCast(
            target.position,
            cameraRadius,
            direction,
            out hit,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        float finalDistance = blocked ? hit.distance - wallPadding : distance;
        finalDistance = Mathf.Max(finalDistance, 0.5f);
        float smooth = (finalDistance < currectDistance) ? 20f : 5f;
        currectDistance = Mathf.Lerp(currectDistance, finalDistance, smooth * Time.deltaTime);

        transform.position = target.position + direction * currectDistance;
        transform.rotation = rotation;
    }
}
