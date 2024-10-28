using UnityEngine;

public class BackCamera : MonoBehaviour
{
    // Values set in editor
    public Vector3 targetPosition;
    public Vector3 targetRotation;
    public float smoothTime;

    private Vector3 velocity = Vector3.zero;
    private Quaternion targetQuaternion;

    void Start()
    {
        targetQuaternion = Quaternion.Euler(targetRotation);
    }

    void FixedUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetQuaternion, Time.deltaTime / smoothTime);
    }
}