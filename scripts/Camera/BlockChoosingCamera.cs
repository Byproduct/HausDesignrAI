using UnityEngine;

public class BlockChoosingCamera : MonoBehaviour
{
    public Vector3 targetPosition = new Vector3(500f, 400f, 1120f);
    public Vector3 targetRotation = new Vector3(0f, 180f, 0f);

    public float smoothTime = 3f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Quaternion targetQuaternion;
    private float interpolationProgress = 0f;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        targetQuaternion = Quaternion.Euler(targetRotation);
    }

    void Update()
    {
        interpolationProgress += Time.deltaTime / smoothTime;
        interpolationProgress = Mathf.Clamp01(interpolationProgress);

        transform.position = Vector3.Lerp(initialPosition, targetPosition, interpolationProgress);

        float rotationProgress = Mathf.SmoothStep(0f, 1f, interpolationProgress);

        transform.rotation = Quaternion.Slerp(initialRotation, targetQuaternion, rotationProgress);
    }
}
