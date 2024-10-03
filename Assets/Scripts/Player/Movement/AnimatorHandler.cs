using UnityEngine;

public class AnimatorHandler
{
    private Animator _animator;

    private readonly int _speedHash = Animator.StringToHash("speed");
    private readonly int _isJumpingHash = Animator.StringToHash("isJumping");

    public AnimatorHandler(Animator animator)
    {
        _animator = animator;
    }

    public void UpdateAnimator(float movementSpeed, bool isJumping)
    {
        _animator.SetFloat(_speedHash, movementSpeed);

        // Update isJumping parameter
        _animator.SetBool(_isJumpingHash, isJumping);
    }
}
