using UnityEngine;

public class PlayerController : CharacterBehaviour
{
    [Header("Camera")]
    public CameraRigHandler rigHandler;

    [Header("References")]
    public bool _isAiming { get; set; }
    public bool IsAiming => _isAiming;
    public bool _inventoryOpen { get; set; }
    public bool InventoryOpen => _inventoryOpen;

    private float _horizontal;
    private float _vertical;
    private float _mouseX;
    private bool _jumpPressed;
    private bool _isRunning;

    void Update()
    {
        _horizontal = Input.GetAxisRaw("Horizontal");
        _vertical = Input.GetAxisRaw("Vertical");
        _mouseX = Input.GetAxis("Mouse X");
        _isAiming = Input.GetMouseButton(1);
        
        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;

        if (Input.GetKeyDown(KeyCode.Q)) 
            rigHandler.ToggleView(CameraSide.Left);

        if (Input.GetKeyDown(KeyCode.E))
            rigHandler.ToggleView(CameraSide.Right);

        if (Input.GetKeyDown(KeyCode.I))
            rigHandler.ToggleInventory();

        if (Input.GetKey(KeyCode.LeftShift) && !_isAiming)
            _isRunning = true;

        if (Input.GetKeyUp(KeyCode.LeftShift) && !_isAiming)
            _isRunning = false;

        Vector3 direction = new Vector3(_horizontal, 0f, _vertical).normalized;
        float movementSpeed = direction.magnitude * speed;
        if (_isAiming)
            movementSpeed *= 0.5f;

        if (_isRunning)
            movementSpeed *= 1.5f;

        _animatorHandler.UpdateAnimator(movementSpeed, !_movementHandler.IsGrounded);
        _leanHandler.CalculateLeanAngle(_horizontal, _vertical, _mouseX, _isAiming);
        meshTransform.rotation = Quaternion.Euler(0f, meshTransform.eulerAngles.y, _leanHandler.CurrentLeanAngle);
    }

    void FixedUpdate()
    {
        Vector3 direction = new Vector3(_horizontal, 0f, _vertical).normalized;
        float currentSpeed = speed;
        if (_isAiming)
            currentSpeed *= 0.5f;

        if (_isRunning)
            currentSpeed *= 1.5f;

        _movementHandler.FixedUpdateMovement(direction, _jumpPressed, cameraTransform, _isAiming, currentSpeed);
        _jumpPressed = false;
    }
}
