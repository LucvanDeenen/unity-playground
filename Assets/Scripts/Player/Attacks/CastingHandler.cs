using UnityEngine;

public class CastingHandler : MonoBehaviour
{
    public GameObject fireballPrefab; // Assign the Fireball prefab
    public Transform firePoint; // Assign the FirePoint transform
    public float fireRate = 1f; // Time between shots
    public Animator animator; // Reference to the Animator

    private float nextFireTime = 0f;

    void Update()
    {
        // Check if it's time to fire
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            ShootFireball();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    void ShootFireball()
    {
        // Instantiate the fireball at the fire point
        GameObject fireball = Instantiate(fireballPrefab, firePoint.position, firePoint.rotation);

        // Get the Rigidbody component
        Rigidbody rb = fireball.GetComponent<Rigidbody>();

        // Set the velocity of the fireball
        rb.velocity = firePoint.forward * Time.deltaTime;

        // Trigger the attack animation
        animator.SetTrigger("AttackTrigger");
    }
}
