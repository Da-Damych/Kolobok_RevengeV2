using UnityEngine;
using UnityEngine.InputSystem;

public class FerstTriggerNote : MonoBehaviour
{
    [SerializeField] private GameObject noteUI;
    [SerializeField] private GameObject hintUI;
    [SerializeField] private MovementPlayer playerMovement;
    [SerializeField] private CameraControle CameraControle;

    private Movement controls;
    private bool playerInside = false;
    private bool noteOpen = false;

    private void Awake()
    {
        controls = new Movement();
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
        controls.Gameplay.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        controls.Gameplay.Interact.performed -= OnInteract;
        controls.Gameplay.Disable();
    }

    private void Start()
    {
        noteUI.SetActive(false);
        hintUI.SetActive(false);
    }

    private void OnTriggerenter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        hintUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        hintUI.SetActive(false);

        if (noteOpen) CloseNote();
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!playerInside) return;

        if (noteOpen) CloseNote();
        else OpenNote();
    }

    private void OpenNote()
    {
        noteOpen = true;
        noteUI.SetActive(true);
        hintUI.SetActive(false);

        playerMovement.enabled = false;
        CameraControle.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseNote()
    {
        noteOpen = false;
        noteUI.SetActive(false);

        playerMovement.enabled = true;
        CameraControle.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerInside) hintUI.SetActive(true);
    }
}
