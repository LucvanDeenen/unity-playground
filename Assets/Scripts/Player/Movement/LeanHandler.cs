using UnityEngine;

public class LeanHandler
{
    private float _currentLeanAngle = 0f;
    private float _leanSmoothVelocity;
    private float _maxLeanAngle;
    private float _leanSmoothTime;

    public LeanHandler(float maxLeanAngle, float leanSmoothTime)
    {
        _maxLeanAngle = maxLeanAngle;
        _leanSmoothTime = leanSmoothTime;
    }

    public float CalculateLeanAngle(float horizontal, float vertical, float mouseX)
    {
        float targetLeanAngle = 0f;

        if (new Vector2(horizontal, vertical).magnitude >= 0.1f)
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

            // Mouse-based leaning
            float mouseInfluence = Mathf.Lerp(0f, _maxLeanAngle, Mathf.Abs(mouseX) / 5f);
            targetLeanAngle += mouseX > 0f ? -mouseInfluence : mouseInfluence;
        }

        // Smoothly interpolate current lean angle towards target lean angle
        _currentLeanAngle = Mathf.SmoothDamp(_currentLeanAngle, targetLeanAngle, ref _leanSmoothVelocity, _leanSmoothTime);

        return _currentLeanAngle;
    }
}
