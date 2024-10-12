using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float sensitivity;
    public Camera cam;

    float rotX = 30f;
    float rotY = 0f;

    public int CameraPointX;
    public int CameraPointZ;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Invoke("InitCameraPosition", 0.1f);
    }

    void InitCameraPosition()
    {
        cam.transform.position = new Vector3(500, 60, -20);
//        cam.transform.eulerAngles = new Vector3(30, 0, 0);
    }
    void Update()
    {
        rotX -= Input.GetAxis("Mouse Y") * sensitivity;
        rotY += Input.GetAxis("Mouse X") * sensitivity;

        //rotX = Mathf.Clamp(rotX, minX, maxX);

        cam.transform.localEulerAngles = new Vector3(rotX, rotY, 0);
    }

    void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.visible && Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}