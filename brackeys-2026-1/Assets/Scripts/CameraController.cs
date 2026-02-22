using UnityEngine;

public class CameraFollow : MonoBehaviour
{   
    public Rigidbody2D targetRb;
    public Transform target;

    [Header("Balloon Settings")]
    public float stiffness = 8f;     // How strong the pull toward player is
    public float damping = 5f;       // How fast it settles (lower = floatier)
    public Vector3 offset = new(0, 0, -10);

    [Header("Look Ahead")]
    public float lookAheadDistance = 2f;
    public float lookAheadSmooth = 5f;
    public float lookAheadFallingOffset = -1f;
    public float maxDistanceY = 2.5f;

    private Camera cam;

    private Vector3 velocity;
    private Vector3 currentLookAhead;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        float moveDir = Mathf.Sign(targetRb.linearVelocity.x);

        Vector3 targetLookAhead = Vector3.zero;

        if (Mathf.Abs(targetRb.linearVelocity.x) > 0.1f)
        {
            targetLookAhead = lookAheadDistance * moveDir * Vector3.right;
        }

        currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, lookAheadSmooth * Time.deltaTime);

        desiredPosition += currentLookAhead;
        if (targetRb.linearVelocity.y < -1f)
            desiredPosition.y += lookAheadFallingOffset;

        Vector3 force = (desiredPosition - transform.position) * stiffness;
        force.y *= 1.8f;
        velocity += force * Time.deltaTime;
        velocity *= Mathf.Exp(-damping * Time.deltaTime);

        transform.position += velocity * Time.deltaTime;

        /* Clamp vertical position
        float clampedY = Mathf.Clamp(transform.position.y, target.position.y - maxDistanceY, target.position.y + maxDistanceY);
        transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
        */

    }
}
