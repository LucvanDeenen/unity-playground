using UnityEngine;

public class InputManager : MonoBehaviour
{
    public Vector2 move;  // Stores movement input (WASD)
    public Vector2 look;  // Stores mouse movement input
    public bool jump;     // Stores jump input
    public bool sprint;   // Stores sprint input

    private void Update()
    {
        // Capture movement input
        move.x = Input.GetAxis("Horizontal");  // A/D or Left/Right
        move.y = Input.GetAxis("Vertical");    // W/S or Up/Down

        // Capture look input
        look.x = Input.GetAxis("Mouse X");
        look.y = Input.GetAxis("Mouse Y");

        // Capture jump input
        jump = Input.GetButtonDown("Jump");    // Spacebar

        // Capture sprint input
        sprint = Input.GetKey(KeyCode.LeftShift);
    }
}
