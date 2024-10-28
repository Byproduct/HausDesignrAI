using UnityEngine;

public class LoadedHousesCamera : MonoBehaviour
{
    public static LoadedHousesCamera Instance { get; private set; }

    public float smoothTime;

    private Quaternion targetQuaternion;
    private Vector3 targetPosition = new Vector3(300f, 450f, 950f);
    private Vector3 targetRotation = new Vector3(16f, 140f, 0f);
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        Instance = this;
    }

    void FixedUpdate()
    {
        targetPosition += new Vector3(5f, 0f, 0f); 
        targetQuaternion = Quaternion.Euler(targetRotation);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetQuaternion, Time.deltaTime / smoothTime);
    }
}
