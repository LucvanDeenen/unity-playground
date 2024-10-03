using UnityEngine;

public class LeanHandler
{
    private float _currentLeanAngle = 0f;
    private float _leanSmoothVelocity;
    private float _maxLeanAngle;
    private float _leanSmoothTime;

    public LeanHandler(float maxLeanAngle = 15f, float leanSmoothTime = 0.1f)
    {
        _maxLeanAngle = maxLeanAngle;
        _leanSmoothTime = leanSmoothTime;
    }

    public float CalculateLeanAngle(float horizontal, float vertical, float mouseX)
    {
        float targetLeanAngle = 0f;

        Vector2 inputDirection = new Vector2(horizontal, vertical);
        if (inputDirection.magnitude >= 0.1f)
        {
            // Calculate the angle of movement input
            float inputAngle = Mathf.Atan2(horizontal, vertical) * Mathf.Rad2Deg;

            // Determine lean direction based on input angle
            if ((inputAngle >= 45f && inputAngle <= 135f) || (inputAngle <= -225f && inputAngle >= -315f)) // Right
            {
                targetLeanAngle = -_maxLeanAngle;
            }
            else if ((inputAngle <= -45f && inputAngle >= -135f) || (inputAngle >= 225f && inputAngle <= 315f)) // Left
            {
                targetLeanAngle = _maxLeanAngle;
            }

            // Apply mouse influence
            float mouseInfluence = Mathf.Clamp(mouseX / 5f, -1f, 1f) * _maxLeanAngle;
            targetLeanAngle += mouseInfluence;
        }

        // Smoothly interpolate current lean angle towards target lean angle
        _currentLeanAngle = Mathf.SmoothDamp(_currentLeanAngle, targetLeanAngle, ref _leanSmoothVelocity, _leanSmoothTime);

        return _currentLeanAngle;
    }
}
