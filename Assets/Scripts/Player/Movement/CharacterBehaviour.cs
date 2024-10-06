using UnityEngine;

public class CharacterBehaviour : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] protected float speed = 8f;
    [SerializeField] protected float jumpHeight = 3f;
    [SerializeField] protected float gravity = -18f;

    [Header("Setup")]
    [SerializeField] protected Transform cameraTransform;
    [SerializeField] protected Transform meshTransform;
    [SerializeField] protected Transform defaultCharacterTransform;
    [SerializeField] protected Transform groundCheck;

    [Header("Animations")]
    [SerializeField] protected Animator animator;

    [Header("Masks")]
    [SerializeField] protected LayerMask groundMask;

    protected MovementHandler _movementHandler;
    protected AnimatorHandler _animatorHandler;
    protected LeanHandler _leanHandler;

    void Start()
    {
        _leanHandler = new LeanHandler();
        _movementHandler = new MovementHandler(
            groundCheck,
            meshTransform,
            speed: speed,
            jumpHeight: jumpHeight,
            gravity: gravity,
            groundMask: groundMask
        );
        _animatorHandler = new AnimatorHandler(animator);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
