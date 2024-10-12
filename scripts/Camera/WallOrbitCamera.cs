using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WallOrbitCamera : MonoBehaviour
{
    // Example values - set these in editor

    public Vector3 targetPoint = new Vector3(500, 20, 500);
    // set in editor
    public float radius = 600.0f;            // distance to point being looked at
    public float angularSpeed = 20.0f;       // degrees per second
    public float startingAngle = 0f;
    public float smoothTime = 0.3f;
    public float viewingHeight = 170f;

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
