using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;             // Base movement speed
    public float sprintSpeed = 8f;           // Sprinting speed
    public float rotationSpeed = 10f;        // Speed of turning
    public float acceleration = 10f;         // Acceleration and deceleration rate

    [Header("Jumping Settings")]
    public float jumpHeight = 1.5f;          // Height of the jump
    public float gravity = -9.81f;           // Gravity force
    public float jumpTimeout = 0.1f;         // Time before next jump is allowed
    public float fallTimeout = 0.15f;        // Time before entering fall state

    [Header("Grounded Settings")]
    public bool isGrounded = true;
    public float groundedOffset = -0.14f;    // Offset for ground detection
    public float groundedRadius = 0.28f;     // Radius of ground detection sphere
    public LayerMask groundLayers;           // Layers considered as ground

    private float _speed;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    private CharacterController _controller;
    private InputManager _input;
    private GameObject _mainCamera;

    private void Awake()
    {
        // Get reference to main camera
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main.gameObject;
        }
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<InputManager>();

        // Reset timeouts
        _jumpTimeoutDelta = jumpTimeout;
        _fallTimeoutDelta = fallTimeout;
    }

    private void Update()
    {
        GroundedCheck();
        JumpAndGravity();
        Move();
    }

    private void GroundedCheck()
    {
        // Check if the character is grounded using a sphere at the character's feet
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void Move()
    {
        // Determine target speed based on input and sprinting
        float targetSpeed = _input.sprint ? sprintSpeed : moveSpeed;

        // If no input, slow down
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // Smoothly interpolate to target speed
        _speed = Mathf.Lerp(_speed, targetSpeed, Time.deltaTime * acceleration);

        // Get movement input
        Vector2 input = _input.move;

        // Get camera's forward and right vectors
        Vector3 cameraForward = _mainCamera.transform.forward;
        Vector3 cameraRight = _mainCamera.transform.right;

        // Zero out the y-component to keep movement on the horizontal plane
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate move direction relative to the camera
        Vector3 moveDirection = (cameraForward * input.y + cameraRight * input.x).normalized;

        if (moveDirection != Vector3.zero)
        {
            // Rotate the player to face the movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Apply movement and vertical velocity (gravity or jump)
        Vector3 movement = moveDirection * (_speed * Time.deltaTime) + Vector3.up * _verticalVelocity * Time.deltaTime;
        _controller.Move(movement);
    }

    private void JumpAndGravity()
    {
        if (isGrounded)
        {
            // Reset fall timeout
            _fallTimeoutDelta = fallTimeout;

            // Stop vertical velocity if grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // Calculate the velocity needed to achieve the jump height
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                // Reset jump timeout
                _jumpTimeoutDelta = jumpTimeout;
            }

            // Jump timeout
            if (_jumpTimeoutDelta > 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // Reset jump timeout
            _jumpTimeoutDelta = jumpTimeout;

            // Fall timeout
            if (_fallTimeoutDelta > 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }

            // Disable jump input while in air
            _input.jump = false;
        }

        // Apply gravity
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }
    }
}
