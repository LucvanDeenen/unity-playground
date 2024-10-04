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
    public Vector3 aimingOffset = new Vector3(-2f, 1f, -2f);
    public float transitionSpeed = 5f;
    
    [Header("Setup")]
    public PlayerController _playerController;
    public CinemachineFreeLook freeLookCamera;

    private CinemachineCameraOffset _cameraOffsetExtension;
    private Vector3 _originalOffset;
    private CameraSide _cameraSide;

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
        Vector3 targetOffset = _playerController.IsAiming ? aimingOffset : _originalOffset;

        // Smoothly interpolate to the target offset
        _cameraOffsetExtension.m_Offset = Vector3.Lerp(
            _cameraOffsetExtension.m_Offset,
            targetOffset,
            Time.deltaTime * transitionSpeed
        );
    }

    public void ToggleView(CameraSide cameraSide)
    {
        if (cameraSide == _cameraSide) 
            return;
        
        _cameraSide = _cameraSide == CameraSide.Left ? CameraSide.Right : CameraSide.Left;
        aimingOffset = new Vector3(aimingOffset.x * -1 , aimingOffset.y, aimingOffset.z);
    }
}
