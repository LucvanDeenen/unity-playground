using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform meshTransform;
    [SerializeField] private Transform defaultCharacterTransform;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private CharacterController characterController;

    [Header("Animations")]
    [SerializeField] private Animator animator;

    [Header("Masks")]
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

        if (meshTransform == null)
        {
            meshTransform = transform.Find("Mesh");
        }

        if (meshTransform != null)
        {
            if (characterController == null)
                characterController = meshTransform.GetComponent<CharacterController>();
            if (groundCheck == null)
                groundCheck = meshTransform.Find("GroundCheck");
        }

        if (defaultCharacterTransform == null && meshTransform != null)
        {
            defaultCharacterTransform = meshTransform.Find("DefaultCharacter");
        }

        if (defaultCharacterTransform != null)
        {
            if (animator == null)
                animator = defaultCharacterTransform.GetComponent<Animator>();
        }

        _movementHandler = new MovementHandler(
            characterController,
            groundCheck,
            meshTransform,
            speed: 12f,
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
        if (meshTransform != null)
        {
            meshTransform.rotation = Quaternion.Euler(0f, meshTransform.eulerAngles.y, _currentLeanAngle);
        }
    }

    void FixedUpdate()
    {
        Vector3 direction = new Vector3(_inputHandler.Horizontal, 0f, _inputHandler.Vertical).normalized;

        // Handle movement and rotation in FixedUpdate
        _movementHandler.FixedUpdateMovement(direction, _inputHandler.JumpPressed, cameraTransform);
    }
}
