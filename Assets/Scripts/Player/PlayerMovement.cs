using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f; // Custom gravity
    public float jumpHeight = 2f;  // Jump height

    private CharacterController _controller;
    private InputManager _input;
    private Vector3 _moveDirection;
    private float _verticalVelocity;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<InputManager>();
    }

    private void Update()
    {
        Move();
        ApplyGravityAndJump();
    }

    private void Move()
    {
        // Get movement input from InputManager
        Vector3 move = new Vector3(_input.move.x, 0, _input.move.y);

        if (move != Vector3.zero)
        {
            // Rotate the player towards movement direction relative to the camera
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Convert movement to the player's direction
        _moveDirection = transform.forward * move.magnitude * moveSpeed;

        // Apply the movement
        _controller.Move(_moveDirection * Time.deltaTime);
    }

    private void ApplyGravityAndJump()
    {
        // Check if the player is grounded
        if (_controller.isGrounded)
        {
            // Reset the vertical velocity if grounded
            if (_verticalVelocity < 0)
                _verticalVelocity = -2f;

            // Check for jump input
            if (_input.jump)
            {
                // Calculate jump velocity using the jump height formula
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            // Apply gravity when not grounded
            _verticalVelocity += gravity * Time.deltaTime;
        }

        // Apply vertical velocity (gravity or jump) to the character
        Vector3 verticalMovement = new Vector3(0, _verticalVelocity, 0);
        _controller.Move(verticalMovement * Time.deltaTime);
    }
}
