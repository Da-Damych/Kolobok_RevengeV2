using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControle : MonoBehaviour
{
    private Transform cameraTransform;
    private float sensitivity = 0.1f;
    private float pitch = 0f;


    private void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }
           


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensitivity;

        transform.Rotate(0, mouseDelta.x, 0, Space.World);

        pitch -= mouseDelta.y;
        pitch = Mathf.Clamp(pitch, -90f, 90);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    private void OnLook(InputValue value)
    {
        Vector2 look = value.Get<Vector2>();
    }
}
