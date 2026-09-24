using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControle : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float sensitivity = 0.2f;
    [SerializeField] private float distance = 4f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    private float pitch = 0f;
    private float yaw = 0f;


    private void Start()
    {
          


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
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
        transform.position = target.position + offset;
        transform.rotation = rotation;
    }
}
