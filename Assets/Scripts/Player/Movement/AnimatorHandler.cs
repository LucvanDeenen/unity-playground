using UnityEngine;

public class AnimatorHandler : MonoBehaviour
{
    public Animator Animator;

    public void UpdateAnimator(float movementSpeed)
    {
        Animator.SetFloat("speed", movementSpeed);
    }
}
