using UnityEngine;

public class LeanHandler
{
    private float _currentLeanAngle = 0f;
    private float _leanSmoothVelocity;
    private float _maxLeanAngle;
    private float _leanSmoothTime;

    public float CurrentLeanAngle => _currentLeanAngle;

    public LeanHandler(float maxLeanAngle = 15f, float leanSmoothTime = 0.1f)
    {
        _maxLeanAngle = maxLeanAngle;
        _leanSmoothTime = leanSmoothTime;
    }

    public void CalculateLeanAngle(float horizontal, float vertical, float mouseX, bool isAiming)
    {
        float targetLeanAngle = 0f;

        if (isAiming)
        {
            targetLeanAngle = 0f;
        }
        else
        {
            // Calculate target lean angle based on input
            Vector2 inputDirection = new Vector2(horizontal, vertical);
            if (inputDirection.magnitude >= 0.1f)
            {
                // Direction-based leaning
                if (vertical > 0f && horizontal > 0f) // Forward-right
                    targetLeanAngle = Mathf.Lerp(0f, -_maxLeanAngle, Mathf.Abs(horizontal));
                else if (vertical > 0f && horizontal < 0f) // Forward-left
                    targetLeanAngle = Mathf.Lerp(0f, _maxLeanAngle, Mathf.Abs(horizontal));
                else if (vertical < 0f && horizontal < 0f) // Backward-left
                    targetLeanAngle = Mathf.Lerp(0f, _maxLeanAngle, Mathf.Abs(horizontal));
                else if (vertical < 0f && horizontal > 0f) // Backward-right
                    targetLeanAngle = Mathf.Lerp(0f, -_maxLeanAngle, Mathf.Abs(horizontal));
            }
        }

        // Smoothly interpolate current lean angle towards target lean angle
        _currentLeanAngle = Mathf.SmoothDampAngle(_currentLeanAngle, targetLeanAngle, ref _leanSmoothVelocity, _leanSmoothTime);
    }
}
