using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Animations")]
    [SerializeField] private Animator animator;
    
    [Header("Movement")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;

    private IInputHandler _inputHandler;
    private MovementHandler _movementHandler;
    private AnimatorHandler _animatorHandler;
    private LeanHandler _leanHandler;

    private float _currentLeanAngle = 0f;

    void Start()
    {
        _inputHandler = new InputHandler();
        _leanHandler = new LeanHandler();

        _movementHandler = new MovementHandler(
            characterController,
            groundCheck,
            transform,
            groundMask: groundMask
        );

        _animatorHandler = new AnimatorHandler(animator);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        _inputHandler.UpdateInput();

        Vector3 direction = new Vector3(_inputHandler.Horizontal, 0f, _inputHandler.Vertical).normalized;
        float movementSpeed = direction.magnitude * _movementHandler.Speed;

        // Update animator with movement speed and jumping state
        _animatorHandler.UpdateAnimator(movementSpeed, !_movementHandler.IsGrounded);

        // Calculate lean angle
        _currentLeanAngle = _leanHandler.CalculateLeanAngle(_inputHandler.Horizontal, _inputHandler.Vertical, _inputHandler.MouseX);

        // Apply lean rotation
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, _currentLeanAngle);
    }

    void FixedUpdate()
    {
        Vector3 direction = new Vector3(_inputHandler.Horizontal, 0f, _inputHandler.Vertical).normalized;

        // Handle movement and rotation in FixedUpdate
        _movementHandler.FixedUpdateMovement(direction, _inputHandler.JumpPressed, cameraTransform);
    }
}
