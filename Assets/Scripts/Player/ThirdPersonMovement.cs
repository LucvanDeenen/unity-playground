using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public CharacterController controller;
    public Animator animator;
    public Transform cam;
    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    public float leanSmoothTime = 0.1f; // Time to smoothly lean
    public float maxLeanAngle = 15f; // Max lean angle for leaning left/right

    private float turnSmoothVelocity;
    private float currentLeanAngle = 0f;
    private float leanSmoothVelocity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Get player input for movement and mouse
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        float mouseX = Input.GetAxis("Mouse X"); // Mouse movement on X-axis

        // Create movement direction vector
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Calculate movement speed and update animator
        float movementSpeed = direction.magnitude * speed;
        animator.SetFloat("speed", movementSpeed);

        // Determine target lean angle based on input and mouse movement
        float targetLeanAngle = 0f;

        if (direction.magnitude >= 0.1f)
        {
            // Calculate lean based on input (WASD) - lean left or right based on direction
            if (vertical > 0f && horizontal > 0f) // Forward-right (W+D)
            {
                targetLeanAngle = Mathf.Lerp(0f, -maxLeanAngle, Mathf.Abs(horizontal));
            }
            else if (vertical > 0f && horizontal < 0f) // Forward-left (W+A)
            {
                targetLeanAngle = Mathf.Lerp(0f, maxLeanAngle, Mathf.Abs(horizontal));
            }
            else if (vertical < 0f && horizontal < 0f) // Backward-left (S+A)
            {
                targetLeanAngle = Mathf.Lerp(0f, maxLeanAngle, Mathf.Abs(horizontal));
            }
            else if (vertical < 0f && horizontal > 0f) // Backward-right (S+D)
            {
                targetLeanAngle = Mathf.Lerp(0f, -maxLeanAngle, Mathf.Abs(horizontal));
            }

            // Modify lean based on the mouse movement (camera direction)
            float mouseInfluence = Mathf.Lerp(0f, maxLeanAngle, Mathf.Abs(mouseX) / 5f);
            if (mouseX > 0f)
            {
                // Mouse moving right, lean right
                targetLeanAngle -= mouseInfluence;
            }
            else if (mouseX < 0f)
            {
                // Mouse moving left, lean left
                targetLeanAngle += mouseInfluence;
            }
        }

        // Smoothly interpolate current lean angle towards target lean angle
        currentLeanAngle = Mathf.SmoothDamp(currentLeanAngle, targetLeanAngle, ref leanSmoothVelocity, leanSmoothTime);

        // Handle movement and rotation
        float angle = transform.eulerAngles.y; // Default to current y rotation
        if (direction.magnitude >= 0.1f)
        {
            // Calculate target rotation based on camera and input
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

            // Smoothly rotate towards the target angle
            angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            // Calculate move direction and move character
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }

        // Apply rotation with lean angle (rotation around z-axis for lean)
        transform.rotation = Quaternion.Euler(0f, angle, currentLeanAngle);
    }
}
