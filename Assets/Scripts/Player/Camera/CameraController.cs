using UnityEngine;
using Cinemachine;
using UnityEngine.UI;

public class CameraAimController : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera;
    public float aimThreshold = -20f; // Pitch angle threshold to start aiming
    public float cameraOffsetZ = -2f; // Offset to apply when aiming down
    public float transitionSpeed = 2f; // Speed of camera transition
    public Text crosshair; // Reference to the crosshair Text UI element

    private float[] originalRigPositions;
    private bool isAiming = false;

    void Start()
    {
        originalRigPositions = new float[3];
        originalRigPositions[0] = freeLookCamera.m_Orbits[0].m_Height;
        originalRigPositions[1] = freeLookCamera.m_Orbits[1].m_Height;
        originalRigPositions[2] = freeLookCamera.m_Orbits[2].m_Height;
    }

    void Update()
    {
        // Get the camera's pitch angle
        float pitch = freeLookCamera.m_YAxis.Value * 180f - 90f; // Convert from normalized value to degrees

        // Check if the pitch is below the threshold
        if (pitch < aimThreshold)
        {
            if (!isAiming)
            {
                isAiming = true;
                // Increase crosshair opacity to 90%
                SetCrosshairOpacity(0.9f);
            }
            // Smoothly adjust the camera's rig positions to move it closer
            AdjustCameraOffset(cameraOffsetZ);
        }
        else
        {
            if (isAiming)
            {
                isAiming = false;
                // Reset crosshair opacity to 50%
                SetCrosshairOpacity(0.5f);
            }
            // Reset camera offset smoothly
            AdjustCameraOffset(0f);
        }
    }

    void AdjustCameraOffset(float targetOffsetZ)
    {
        // For each rig, adjust the radius to move the camera in the Z-axis
        for (int i = 0; i < 3; i++)
        {
            float currentRadius = freeLookCamera.m_Orbits[i].m_Radius;
            float desiredRadius = Mathf.Lerp(currentRadius, originalRigPositions[i] + targetOffsetZ, Time.deltaTime * transitionSpeed);
            freeLookCamera.m_Orbits[i].m_Radius = desiredRadius;
        }
    }

    void SetCrosshairOpacity(float opacity)
    {
        if (crosshair != null)
        {
            Color color = crosshair.color;
            color.a = opacity;
            crosshair.color = color;
        }
    }
}
