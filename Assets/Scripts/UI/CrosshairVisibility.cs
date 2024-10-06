using UnityEngine;

public class CrosshairVisibility : Visibility
{
    [SerializeField] private PlayerController playerControllerScript;
    
    void Update()
    {
        targetValue = playerControllerScript.IsAiming;
    }
}