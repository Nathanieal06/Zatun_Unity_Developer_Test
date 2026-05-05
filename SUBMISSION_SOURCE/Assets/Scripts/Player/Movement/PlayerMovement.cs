using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float rotationSpeed = 10f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;
    private Vector3 velocity; // Stores our vertical falling speed

    [Header("Animation")]
    [field: SerializeField] public Animator PlayerAnimator { get; private set; }

    private PlayerControls controls;
    private CharacterController controller;
    private Transform mainCamera;
    private WeaponController weaponController;

    private void Awake()
    {
        controls = new PlayerControls();
        controller = GetComponent<CharacterController>();
        weaponController = GetComponent<WeaponController>();
        mainCamera = Camera.main.transform;

        if (PlayerAnimator == null)
        {
            PlayerAnimator = GetComponentInChildren<Animator>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        if (!controller.enabled) return;

        // 1. Grounded Check & Base Gravity
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Snaps the player firmly to the ground
        }

        // 2. Read Inputs
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
        bool isSprinting = controls.Player.Sprint.IsPressed();
        bool isJumpPressed = controls.Player.Jump.triggered; // Reads the spacebar

        // 3. Horizontal Movement Calculation
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 moveDirection = (mainCamera.forward * inputDirection.z) + (mainCamera.right * inputDirection.x);
        moveDirection.y = 0f;
        moveDirection.Normalize();

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        bool isAiming = InputManager.IsAimHeld;

        if (isAiming)
        {
            // Rotate player to face the camera's forward direction while aiming
            Vector3 cameraForward = mainCamera.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        if (moveDirection.magnitude >= 0.1f)
        {
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);
            
            if (!isAiming)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // 4. Jump Execution (Physics Formula: v = sqrt(h * -2 * g))
        if (isJumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (PlayerAnimator != null)
            {
                PlayerAnimator.SetTrigger("Jump");
            }
        }

        // 5. Apply Gravity over time
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime); // Applies the vertical movement

        // 6. Update Animator
        if (PlayerAnimator != null)
        {
            float targetAnimSpeed = moveDirection.magnitude * currentSpeed;
            PlayerAnimator.SetFloat("Speed", targetAnimSpeed, 0.1f, Time.deltaTime);
            PlayerAnimator.SetBool("IsGrounded", isGrounded);
        }
    }
}