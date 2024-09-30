using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 10f; // Speed of the fireball
    public float lifeTime = 5f; // Time before the fireball is automatically destroyed if no collision
    public GameObject explosionEffect; // Optional explosion effect prefab

    private Rigidbody rb;

    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();

        // Set the velocity of the fireball in the forward direction
        rb.velocity = transform.forward * speed;

        // Automatically destroy the fireball after a certain time
        Destroy(gameObject, lifeTime);
    }

    // Detect collision
    void OnCollisionEnter(Collision collision)
    {
        // Optional: Instantiate an explosion effect at the point of collision
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // Destroy the fireball on impact
        Destroy(gameObject);
    }
}
