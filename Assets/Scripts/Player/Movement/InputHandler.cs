using UnityEngine;

public interface IInputHandler
{
    float Horizontal { get; }
    float Vertical { get; }
    float MouseX { get; }
    bool JumpPressed { get; }

    void UpdateInput();
}

public class InputHandler : IInputHandler
{
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public float MouseX { get; private set; }
    public bool JumpPressed { get; private set; }

    public void UpdateInput()
    {
        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");
        MouseX = Input.GetAxis("Mouse X");
        JumpPressed = Input.GetButtonDown("Jump");
    }
}
