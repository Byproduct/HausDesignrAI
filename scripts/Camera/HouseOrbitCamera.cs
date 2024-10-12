using UnityEngine;

public class HouseOrbitCamera : MonoBehaviour
{
    public static HouseOrbitCamera Instance { get; private set; }

    // Example values - set these in editor
    public Vector3 targetPoint = new Vector3(500, 20, 500);
    public float radius = 500.0f;            // distance to point being looked at
    public float angularSpeed = -36.0f;      // degrees per second
    public float startingAngle = 180f;
    public float smoothTime = 0.3f;
    public float rotationSmoothTime = 0.3f;
    public float viewingHeight = 150f;
    public float totalDuration = 10f;
    public float elapsedTime = 0f;

    private float angle = 0.0f;

    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private Quaternion targetRotation;

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
            GameManager.Instance.GetComponent<End>().enabled = true;
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
