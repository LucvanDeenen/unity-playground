using UnityEngine;
using Unity.Cinemachine;

public enum CameraSide
{
    Left,
    Right
}

public class CameraRigHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 aimingOffset = new Vector3(2f, 1f, 2f);
    [SerializeField] private CameraSide _cameraSide;

    [Header("Setup")]
    [SerializeField] private PlayerController _playerController;
    [SerializeField][System.Obsolete] private CinemachineFreeLook freeLookCamera;

    private CinemachineCameraOffset _cameraOffsetExtension;
    private Vector3 _originalOffset;

    // Variables to handle inventory camera behavior
    private float _originalXAxisMaxSpeed;
    private float _originalYAxisMaxSpeed;
    private float _initialYAxisValue;
    private float _initialXAxisValue;
    private float _targetYAxisValue;
    private float _targetXAxisValue;
    private bool _isTransitioning = false;
    private float _transitionTime = 0f;
    private float _transitionDuration = 0.5f; // Adjust transition duration as needed

    void Start()
    {
        // Get references
        if (freeLookCamera == null)
            freeLookCamera = GetComponent<CinemachineFreeLook>();

        if (freeLookCamera != null)
        {
            // Get or add the CinemachineCameraOffset extension
            _cameraOffsetExtension = freeLookCamera.GetComponent<CinemachineCameraOffset>();
            if (_cameraOffsetExtension == null)
            {
                _cameraOffsetExtension = freeLookCamera.gameObject.AddComponent<CinemachineCameraOffset>();
            }

            // Store the original offset
            _originalOffset = _cameraOffsetExtension.Offset;

            // Store original camera movement speeds
            _originalXAxisMaxSpeed = freeLookCamera.m_XAxis.m_MaxSpeed;
            _originalYAxisMaxSpeed = freeLookCamera.m_YAxis.m_MaxSpeed;
        }
        else
        {
            Debug.LogError("CinemachineFreeLook camera is not assigned or found.");
        }
    }

    void Update()
    {
        if (_cameraOffsetExtension == null || _playerController == null)
            return;

        // Determine the target offset based on aiming state and inventory state
        Vector3 targetOffset = _playerController.IsAiming && !_playerController.InventoryOpen ? aimingOffset : _originalOffset;
        float transitionSpeed = 5f;

        // Smoothly interpolate to the target offset
        _cameraOffsetExtension.Offset = Vector3.Lerp(
            _cameraOffsetExtension.Offset,
            targetOffset,
            Time.deltaTime * transitionSpeed
        );

        // Handle camera transition when inventory is toggled
        if (_isTransitioning)
        {
            _transitionTime += Time.deltaTime;
            float t = _transitionTime / _transitionDuration;
            if (t >= 1f)
            {
                t = 1f;
                _isTransitioning = false;
            }

            // Interpolate m_YAxis.Value and m_XAxis.Value
            freeLookCamera.m_YAxis.Value = Mathf.Lerp(_initialYAxisValue, _targetYAxisValue, t);
            freeLookCamera.m_XAxis.Value = Mathf.LerpAngle(_initialXAxisValue, _targetXAxisValue, t);
        }
    }

    public void ToggleInventory()
    {
        // Update player controller's inventory state
        // _playerController.SetInventoryOpen(!_playerController.InventoryOpen);
        // _playerController.SetAiming(false);

        if (_playerController.InventoryOpen)
        {
            // Disable camera movement
            freeLookCamera.m_XAxis.m_MaxSpeed = 0f;
            freeLookCamera.m_YAxis.m_MaxSpeed = 0f;

            // Store initial camera axis values
            _initialYAxisValue = freeLookCamera.m_YAxis.Value;
            _initialXAxisValue = freeLookCamera.m_XAxis.Value;

            // Set target values for bottom rig and facing the player
            _targetYAxisValue = 0f; // Move to bottom rig

            // Calculate the angle to face the player
            _targetXAxisValue = (_initialXAxisValue + 0.5f) % 1f; // Rotate 180 degrees around

            // Start transition
            _isTransitioning = true;
            _transitionTime = 0f;
        }
        else
        {
            // Enable camera movement
            freeLookCamera.m_XAxis.m_MaxSpeed = _originalXAxisMaxSpeed;
            freeLookCamera.m_YAxis.m_MaxSpeed = _originalYAxisMaxSpeed;

            // Store initial camera axis values
            _initialYAxisValue = freeLookCamera.m_YAxis.Value;
            _initialXAxisValue = freeLookCamera.m_XAxis.Value;

            // Set target values to default positions
            _targetYAxisValue = 0.5f; // Default middle rig value (adjust as needed)
            _targetXAxisValue = _initialXAxisValue; // Keep current angle

            // Start transition
            _isTransitioning = true;
            _transitionTime = 0f;
        }
    }

    public void ToggleView(CameraSide cameraSide)
    {
        if (cameraSide == _cameraSide)
            return;

        _cameraSide = _cameraSide == CameraSide.Left ? CameraSide.Right : CameraSide.Left;
        aimingOffset = new Vector3(aimingOffset.x * -1, aimingOffset.y, aimingOffset.z);
    }
}
