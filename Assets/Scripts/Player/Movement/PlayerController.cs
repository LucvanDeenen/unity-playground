using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float gravity = -18f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform meshTransform;
    [SerializeField] private Transform defaultCharacterTransform;
    [SerializeField] private Transform groundCheck;

    [Header("Animations")]
    [SerializeField] private Animator animator;

    [Header("Masks")]
    [SerializeField] private LayerMask groundMask;

    private MovementHandler _movementHandler;
    private AnimatorHandler _animatorHandler;
    private LeanHandler _leanHandler;

    private float _horizontal;
    private float _vertical;
    private float _mouseX;
    private bool _jumpPressed;


    void Start()
    {
        _leanHandler = new LeanHandler();
        _movementHandler = new MovementHandler(
            groundCheck,
            meshTransform,
            speed: speed,
            jumpHeight: jumpHeight,
            gravity: gravity,
            groundMask: groundMask
        );
        _animatorHandler = new AnimatorHandler(animator);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Handle input
        _horizontal = Input.GetAxisRaw("Horizontal");
        _vertical = Input.GetAxisRaw("Vertical");
        _mouseX = Input.GetAxis("Mouse X");

        if (Input.GetButtonDown("Jump"))
        {
            _jumpPressed = true;
        }

        // Calculate movement speed for animator
        Vector3 direction = new Vector3(_horizontal, 0f, _vertical).normalized;
        float movementSpeed = direction.magnitude * speed;

        // Update animator with movement speed and jumping state
        _animatorHandler.UpdateAnimator(movementSpeed, !_movementHandler.IsGrounded);

        // Calculate & apply lean angle
        _leanHandler.CalculateLeanAngle(_horizontal, _vertical, _mouseX);
        meshTransform.rotation = Quaternion.Euler(0f, meshTransform.eulerAngles.y, _leanHandler.CurrentLeanAngle);
    }

    void FixedUpdate()
    {
        Vector3 direction = new Vector3(_horizontal, 0f, _vertical).normalized;

        // Handle movement and rotation in FixedUpdate
        _movementHandler.FixedUpdateMovement(direction, _jumpPressed, cameraTransform);

        // Reset jumpPressed after handling to prevent continuous jumping
        _jumpPressed = false;
    }
}
