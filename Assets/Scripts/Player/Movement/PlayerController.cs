using UnityEngine;

[RequireComponent(typeof(MovementHandler), typeof(AnimatorHandler))]
public class PlayerController : MonoBehaviour
{
    public Transform CameraTransform;
    public float TurnSmoothTime = 0.1f;
    public float MaxLeanAngle = 15f;
    public float LeanSmoothTime = 0.1f;

    private InputHandler _inputHandler;
    private MovementHandler _movementHandler;
    private AnimatorHandler _animatorHandler;
    private LeanHandler _leanHandler;

    private float _turnSmoothVelocity;

    void Start()
    {
        // Initialize handlers
        _inputHandler = new InputHandler();
        _movementHandler = GetComponent<MovementHandler>();
        _animatorHandler = GetComponent<AnimatorHandler>();
        _leanHandler = new LeanHandler(MaxLeanAngle, LeanSmoothTime);

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        _inputHandler.UpdateInput();

        Vector3 direction = new Vector3(_inputHandler.Horizontal, 0f, _inputHandler.Vertical).normalized;
        float movementSpeed = direction.magnitude * _movementHandler.Speed;

        // Update animator
        _animatorHandler.UpdateAnimator(movementSpeed);

        // Calculate lean angle
        float currentLeanAngle = _leanHandler.CalculateLeanAngle(_inputHandler.Horizontal, _inputHandler.Vertical, _inputHandler.MouseX);

        // Handle rotation
        float angle = transform.eulerAngles.y;
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + CameraTransform.eulerAngles.y;
            angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, TurnSmoothTime);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            _movementHandler.Move(moveDir.normalized, _inputHandler.JumpPressed);
        }
        else
        {
            _movementHandler.Move(Vector3.zero, _inputHandler.JumpPressed);
        }

        // Apply rotation with lean angle
        transform.rotation = Quaternion.Euler(0f, angle, currentLeanAngle);
    }
}
