using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackCamera : MonoBehaviour
{
    public Vector3 targetPosition = new Vector3(500f, 400f, 1120f);
    public Vector3 targetRotation = new Vector3(45f, 180f, 0f);

    public float smoothTime = 0.3f;
    private Vector3 velocity = Vector3.zero;
    private Quaternion targetQuaternion;

    void Start()
    {
        targetQuaternion = Quaternion.Euler(targetRotation);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetQuaternion, Time.deltaTime / smoothTime);
    }
}