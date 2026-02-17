using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0f;
    public Vector2 deadzoneSize = new Vector2(2f, 3f);
    public Vector3 offset = new Vector3(0, 0, -10);

    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 followPosition;

    void Start()
    {
        if (target != null)
        {
            followPosition = target.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position;

        if (targetPos.x > followPosition.x + deadzoneSize.x)
            followPosition.x = targetPos.x - deadzoneSize.x;
        else if (targetPos.x < followPosition.x - deadzoneSize.x)
            followPosition.x = targetPos.x + deadzoneSize.x;

        if (targetPos.y > followPosition.y + deadzoneSize.y)
            followPosition.y = targetPos.y - deadzoneSize.y;
        else if (targetPos.y < followPosition.y - deadzoneSize.y)
            followPosition.y = targetPos.y + deadzoneSize.y;

        Vector3 finalDestination = followPosition + offset;
        transform.position = Vector3.SmoothDamp(transform.position, finalDestination, ref currentVelocity, smoothTime);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = (followPosition == Vector3.zero && target != null) ? target.position : followPosition;
        Gizmos.DrawWireCube(center, new Vector3(deadzoneSize.x * 2, deadzoneSize.y * 2, 0.1f));
    }
}