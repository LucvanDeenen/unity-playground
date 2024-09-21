using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0f, 5f, -10f);
    public float smoothSpeed = 0.125f;
    public float mouseSensitivity = 100f;
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;

    private void Start()
    {
        // Hide the cursor in play mode
        Cursor.lockState = CursorLockMode.Locked; // Hide and lock cursor
        Cursor.visible = false; // Optionally set to false
    }

    private void LateUpdate()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Update rotation values (clamp Y to prevent excessive up/down rotation)
        currentRotationY += mouseX; // Control player rotation on Y-axis
        currentRotationX -= mouseY;
        currentRotationX = Mathf.Clamp(currentRotationX, -35f, 60f);  // Adjust the clamp as needed

        // Apply rotation to the camera
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);
        Vector3 desiredPosition = player.position + rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Always look at the player
        transform.LookAt(player);

        // Rotate player to face the camera's forward direction
        player.rotation = Quaternion.Euler(0f, currentRotationY, 0f);

        // Optional: Rotate player on Z-axis based on mouse input (not typical for third-person)
        // float playerZRotation = mouseY * mouseSensitivity; 
        // player.Rotate(0f, 0f, playerZRotation);
    }
}
