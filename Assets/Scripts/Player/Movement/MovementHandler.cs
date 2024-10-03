using UnityEngine;

public class MovementHandler
{
    private CharacterController _controller;
    private Transform _transform;
    private Transform _groundCheck;
    private float _speed;
    private float _jumpHeight;
    private float _gravity;
    private float _groundDistance;
    private LayerMask _groundMask;
    private float _turnSmoothTime;

    private Vector3 _velocity;
    private bool _isGrounded;

    private float _turnSmoothVelocity;

    public MovementHandler(
        CharacterController characterController,
        Transform groundCheck,
        Transform playerTransform,
        float speed = 12f,
        float jumpHeight = 3f,
        float gravity = -9.81f,
        float groundDistance = 0.4f,
        LayerMask groundMask = default,
        float turnSmoothTime = 0.1f
    )
    {
        _controller = characterController;
        _groundCheck = groundCheck;
        _transform = playerTransform;
        _speed = speed;
        _jumpHeight = jumpHeight;
        _gravity = gravity;
        _groundDistance = groundDistance;
        _groundMask = groundMask;
        _turnSmoothTime = turnSmoothTime;
    }

    public float Speed => _speed;

    public void FixedUpdateMovement(Vector3 direction, bool jumpPressed, Transform cameraTransform)
    {
        GroundCheckLogic();

        HandleRotation(direction, cameraTransform);

        HandleMovement(direction);

        HandleJump(jumpPressed);

        ApplyGravity();

        _controller.Move(_velocity * Time.fixedDeltaTime);
    }

    private void GroundCheckLogic()
    {
        _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundDistance, _groundMask);
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }
    }

    private void HandleRotation(Vector3 direction, Transform cameraTransform)
    {
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(_transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
            _transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    private void HandleMovement(Vector3 direction)
    {
        if (direction.magnitude >= 0.1f)
        {
            Vector3 moveDir = Quaternion.Euler(0f, _transform.eulerAngles.y, 0f) * Vector3.forward;
            _controller.Move(moveDir.normalized * _speed * Time.fixedDeltaTime);
        }
    }

    private void HandleJump(bool jumpPressed)
    {
        if (jumpPressed && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }
    }

    private void ApplyGravity()
    {
        _velocity.y += _gravity * Time.fixedDeltaTime * 2f;
    }

    public bool IsGrounded => _isGrounded;
}
