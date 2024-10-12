using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    public GameObject Cam;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (Configuration.Speed == Configuration.SpeedType.Normal)
        {
            Invoke("WallOrbitCamera", 5f);
        }
        else
        {
            Invoke("WallOrbitCamera", 2f);
        }
    }

    public void WallOrbitCamera()
    {
        if (Configuration.Speed == Configuration.SpeedType.Normal)
        {
            Cam.GetComponent<WallOrbitCamera>().enabled = true;
        }
        else  // If speed is fast or dev, skip straight to back camera
        {
            BackCamera();
        }
    }

    public void BackCamera()
    {
        Cam.GetComponent<WallOrbitCamera>().enabled = false;
        Cam.GetComponent<BackCamera>().enabled = true;
    }

    public void BlockChoosingCamera()
    {
        Destroy(Cam.GetComponent<WallOrbitCamera>());
        Destroy(Cam.GetComponent<BackCamera>());
        Cam.GetComponent<BlockChoosingCamera>().enabled = true;
    }

    public void HouseOrbitCamera(Vector3 targetPoint)
    {
        Destroy(Cam.GetComponent<BlockChoosingCamera>());
        Cam.GetComponent<HouseOrbitCamera>().enabled = true;
        Cam.GetComponent<HouseOrbitCamera>().targetPoint = targetPoint;
    }

    public void LoadedHousesCamera()
    {
        Destroy(Cam.GetComponent<HouseOrbitCamera>());
        Cam.GetComponent<LoadedHousesCamera>().enabled = true;
    }
}
