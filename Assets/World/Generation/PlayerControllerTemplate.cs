using UnityEngine;

public class PlayerControllerTemplate : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        transform.Translate(new Vector3(moveX, 0, moveZ));
    }
}
