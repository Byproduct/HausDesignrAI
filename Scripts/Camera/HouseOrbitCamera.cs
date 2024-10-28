using UnityEngine;

public class HouseOrbitCamera : MonoBehaviour
{
    public static HouseOrbitCamera Instance { get; private set; }

    // Values set in editor
    public float angularSpeed;     // degrees per second
    public float elapsedTime;
    public float radius;           // distance to point being looked at
    public float rotationSmoothTime;
    public float startingAngle;
    public float smoothTime;
    public Vector3 targetPoint;
    public float totalDuration;
    public float viewingHeight;

    private float angle = 0.0f;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        angle = startingAngle;
        totalDuration = 320f / Mathf.Abs(angularSpeed);
    }

    void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;
        if (elapsedTime > totalDuration)
        {
            CameraManager.Instance.LoadedHousesCamera();
            MainManager.Instance.StartDisplayHouses();
        }

        // Orbit position calculation
        angle += angularSpeed * Time.deltaTime;
        float radianAngle = angle * Mathf.Deg2Rad;
        float x = targetPoint.x + radius * Mathf.Cos(radianAngle);
        float z = targetPoint.z + radius * Mathf.Sin(radianAngle);
        targetPosition = new Vector3(x, viewingHeight, z);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Smooth LookAt rotation
        Vector3 directionToTarget = targetPoint - transform.position;
        Quaternion desiredRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothTime);
    }
}
