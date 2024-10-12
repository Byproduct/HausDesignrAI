using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPositions : MonoBehaviour
{
    public void InitialCamera()
    {
        Vector3 initialPosition = new Vector3(500, 120, -85);
        Vector3 initialRotation = new Vector3(20, 0, 0);
        Quaternion initialRot = Quaternion.Euler(initialRotation);

        transform.position = initialPosition;
        transform.rotation = initialRot;
    }
}
