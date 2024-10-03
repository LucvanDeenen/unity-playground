using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementHandler : MonoBehaviour
{
    public float Speed = 12f;
    public float JumpHeight = 3f;
    public float Gravity = -9.81f;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    public Transform GroundCheck;
    public float GroundDistance = 0.4f;
    public LayerMask GroundMask;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    public void Move(Vector3 moveDirection, bool jumpPressed)
    {
        GroundCheckLogic();

        _controller.Move(moveDirection * Speed * Time.deltaTime);

        if (jumpPressed && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }

        ApplyGravity();
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void GroundCheckLogic()
    {
        _isGrounded = Physics.CheckSphere(GroundCheck.position, GroundDistance, GroundMask);
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }
    }

    private void ApplyGravity()
    {
        _velocity.y += Gravity * Time.deltaTime * 2f;
    }

    public bool IsGrounded()
    {
        return _isGrounded;
    }
}
