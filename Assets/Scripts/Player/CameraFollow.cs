using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;                   // Reference to the player transform
    public Vector3 offset = new Vector3(0f, 2f, -5f); // Offset from the player
    public float rotationSpeed = 5f;           // Speed of camera rotation
    public float mouseSensitivity = 10f;       // Sensitivity of mouse input
    public float minPitch = -35f;              // Minimum vertical angle
    public float maxPitch = 60f;               // Maximum vertical angle

    private float _pitch = 0.0f;
    private float _yaw = 0.0f;

    private InputManager _input;

    private void Start()
    {
        _input = player.GetComponent<InputManager>();
    }

    private void LateUpdate()
    {
        if (_input == null) return;

        // Get mouse input
        float mouseX = _input.look.x * mouseSensitivity;
        float mouseY = _input.look.y * mouseSensitivity;

        // Update rotation values
        _yaw += mouseX * Time.deltaTime;
        _pitch -= mouseY * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        // Create rotation based on yaw and pitch
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Calculate the desired position
        Vector3 desiredPosition = player.position + rotation * offset;

        // Set camera position
        transform.position = desiredPosition;

        // Look at the player
        transform.LookAt(player.position + Vector3.up * 1.5f); // Adjust vertical offset as needed
    }
}
