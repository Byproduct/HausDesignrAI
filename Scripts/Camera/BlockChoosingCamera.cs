using UnityEngine;

public class BlockChoosingCamera : MonoBehaviour
{
    // Values set in editor
    public Vector3 targetPosition;
    public Vector3 targetRotation;
    public float smoothTime;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float interpolationProgress = 0f;
    private Quaternion targetQuaternion;

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
