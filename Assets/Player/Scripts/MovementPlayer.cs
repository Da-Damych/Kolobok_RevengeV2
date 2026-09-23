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
        Vector3 direction = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

        float speed = isSprinting ? runSpeed : walkSpeed;

        controller.Move(direction * speed * Time.deltaTime);

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
