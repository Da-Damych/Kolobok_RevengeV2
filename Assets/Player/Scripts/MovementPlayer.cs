using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementPlayer : MonoBehaviour
{
    private Movement controls;
    private CharacterController controller;
    private Vector2 moveInput;
    private bool isSprinting;
    private float verticalVelocity;

    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSpeed = 10f;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    private void Awake()
    {
        controls = new Movement();
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
        controls.Gameplay.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        controls.Gameplay.Jump.performed -= OnJump;
        controls.Gameplay.Disable();
    }
    
    private void Update()
    {
        moveInput = controls.Gameplay.Move.ReadValue<Vector2>();
        isSprinting = controls.Gameplay.Sprint.IsPressed();
    }

    private void LateUpdate()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;

        Vector3 direction = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            float speed = isSprinting ? runSpeed : walkSpeed;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);

            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity += -2f;
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if ( controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
