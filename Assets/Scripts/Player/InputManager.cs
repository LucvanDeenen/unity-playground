using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;      // Movement input
    public Vector2 look;      // Camera look input
    public bool jump;         // Jump input
    public bool sprint;       // Sprint input

    [Header("Mouse Settings")]
    public bool cursorLocked = true;

    private void Update()
    {
        MoveInput();
        LookInput();
        JumpInput();
        SprintInput();
    }

    private void MoveInput()
    {
        move.x = Input.GetAxisRaw("Horizontal");
        move.y = Input.GetAxisRaw("Vertical");
    }

    private void LookInput()
    {
        look.x = Input.GetAxis("Mouse X");
        look.y = Input.GetAxis("Mouse Y");
    }

    private void JumpInput()
    {
        jump = Input.GetButtonDown("Jump");
    }

    private void SprintInput()
    {
        sprint = Input.GetKey(KeyCode.LeftShift);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !newState;
    }
}
