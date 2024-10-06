using UnityEngine;
using Cinemachine;

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
    [SerializeField] private CinemachineFreeLook freeLookCamera;

    private CinemachineCameraOffset _cameraOffsetExtension;
    private Vector3 _originalOffset;

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
            _originalOffset = _cameraOffsetExtension.m_Offset;
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

        // Determine the target offset based on aiming state
        Vector3 targetOffset = _playerController.IsAiming && !_playerController._inventoryOpen ? aimingOffset : _originalOffset;
        float transitionSpeed = 5f;

        // Smoothly interpolate to the target offset
        _cameraOffsetExtension.m_Offset = Vector3.Lerp(
            _cameraOffsetExtension.m_Offset,
            targetOffset,
            Time.deltaTime * transitionSpeed
        );
    }

    public void ToggleInventory()
    {
        _playerController._isAiming = false;
        _playerController._inventoryOpen = !_playerController._inventoryOpen;
        // Handle implementation to rotate to face the player
    }

    public void ToggleView(CameraSide cameraSide)
    {
        if (cameraSide == _cameraSide) 
            return;
        
        _cameraSide = _cameraSide == CameraSide.Left ? CameraSide.Right : CameraSide.Left;
        aimingOffset = new Vector3(aimingOffset.x * -1 , aimingOffset.y, aimingOffset.z);
    }
}
