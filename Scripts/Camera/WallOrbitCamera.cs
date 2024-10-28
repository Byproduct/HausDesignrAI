using UnityEngine;

public class WallOrbitCamera : MonoBehaviour
{
    // Values set in editor
    public float angularSpeed;      // degrees per second
    public float radius;            // distance to point being looked at
    public float smoothTime;
    public float startingAngle;
    public float viewingHeight;
    public Vector3 targetPoint;

    private float angle = 0.0f;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        angle = startingAngle;
    }
    void FixedUpdate()
    {
        angle += angularSpeed * Time.deltaTime;    // + for clockwise, - for anticlockwise
        float radianAngle = angle * Mathf.Deg2Rad;
        float x = targetPoint.x + radius * Mathf.Cos(radianAngle);
        float z = targetPoint.z + radius * Mathf.Sin(radianAngle);
        targetPosition = new Vector3(x, viewingHeight, z);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        transform.LookAt(targetPoint);
    }
}
