using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 10f;
    public float acceleration = 10f;

    [Header("Jumping Settings")]
    public float jumpHeight = 1.5f;
    public float gravity = -18f;
    public float jumpTimeout = 0.1f;
    public float fallTimeout = 0.15f;

    [Header("Grounded Settings")]
    public bool isGrounded = true;
    public float groundedOffset = 1f;
    public float groundedRadius = 0.3f;
    public LayerMask groundLayers;

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

    /// <summary>
    /// Checks whether the character is grounded.
    /// </summary>
    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Handles player movement.
    /// </summary>
    private void Move()
    {
        float targetSpeed = _input.sprint ? sprintSpeed : moveSpeed;
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        _speed = Mathf.Lerp(_speed, targetSpeed, Time.deltaTime * acceleration);

        Vector2 input = _input.move;

        Vector3 cameraForward = _mainCamera.transform.forward;
        Vector3 cameraRight = _mainCamera.transform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * input.y + cameraRight * input.x).normalized;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 movement = moveDirection * (_speed * Time.deltaTime) + Vector3.up * _verticalVelocity * Time.deltaTime;
        _controller.Move(movement);
    }

    /// <summary>
    /// Handles jump and gravity.
    /// </summary>
    private void JumpAndGravity()
    {
        if (isGrounded)
        {
            // Reset fall timeout
            _fallTimeoutDelta = fallTimeout;

            // Stop downward velocity when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Handle jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _jumpTimeoutDelta = jumpTimeout; // Reset jump timeout
            }

            // Handle jump timeout decrement
            if (_jumpTimeoutDelta > 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // Reset jump timeout
            _jumpTimeoutDelta = jumpTimeout;

            // Apply gravity when in the air
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            // Fall timeout decrement
            if (_fallTimeoutDelta > 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
        }
    }
}
