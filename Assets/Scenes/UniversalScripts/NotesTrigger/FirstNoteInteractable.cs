using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class NoteTrigger1 : MonoBehaviour
{
    [SerializeField] private MovementPlayer playerMovement;
    [SerializeField] private CameraControle cameraControle;
    [SerializeField] private GameObject noteUI;
    [SerializeField] private GameObject hintUI;
    [SerializeField] private string noteText;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            hintUI.SetActive(true);
        }
    }



    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            hintUI.SetActive(false);
            noteUI.SetActive(false);
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!playerInside) return;
        noteUI.SetActive(true);
        playerMovement.enabled = false;
    }

    private void OpenNote()
    {
        noteOpen = true;
        noteUI.SetActive(true);
        hintUI.SetActive(false);

        playerMovement.enabled = false;
        cameraControle.enabled = false;

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void CloseNote()
    {
        noteOpen = false;
        noteUI.SetActive(false);

        playerMovement.enabled = true;
        cameraControle.enabled = true;

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        if (playerInside) hintUI.SetActive(true);
    }
}
