using UnityEngine;

public class PlayerController : CharacterBehaviour
{
    private float _horizontal;
    private float _vertical;
    private float _mouseX;
    private bool _jumpPressed;
    private bool _isAiming;

    void Update()
    {
        // Handle input
        _horizontal = Input.GetAxisRaw("Horizontal");
        _vertical = Input.GetAxisRaw("Vertical");
        _mouseX = Input.GetAxis("Mouse X");
        _isAiming = Input.GetMouseButton(1);
        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;

        // Calculate movement speed for animator
        Vector3 direction = new Vector3(_horizontal, 0f, _vertical).normalized;
        float movementSpeed = direction.magnitude * speed;

        // Halve movement speed when aiming
        if (_isAiming)
            movementSpeed *= 0.5f;

        // Update animator with movement speed and jumping state
        _animatorHandler.UpdateAnimator(movementSpeed, !_movementHandler.IsGrounded);

        // Calculate & apply lean angle
        _leanHandler.CalculateLeanAngle(_horizontal, _vertical, _mouseX, _isAiming);
        meshTransform.rotation = Quaternion.Euler(0f, meshTransform.eulerAngles.y, _leanHandler.CurrentLeanAngle);
    }

    void FixedUpdate()
    {
        Vector3 direction = new Vector3(_horizontal, 0f, _vertical).normalized;

        // Adjust movement speed when aiming
        float currentSpeed = speed;
        if (_isAiming)
        {
            currentSpeed *= 0.5f; // Halve the movement speed when aiming
        }

        // Handle movement and rotation in FixedUpdate
        _movementHandler.FixedUpdateMovement(direction, _jumpPressed, cameraTransform, _isAiming, currentSpeed);

        // Reset jumpPressed after handling to prevent continuous jumping
        _jumpPressed = false;
    }
}
