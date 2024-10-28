// Camera in this demo is handled mainly by activating and destroying scripts attached to the camera object. 
// They are all located in the Scripts/Camera folder.

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
        // At the start of the demo, after a short delay, start "wall orbiting camera" (normal mode) or skip to back camera (fast or dev mode) 
        if (Configuration.Speed == Configuration.SpeedType.Normal)
        {
            Invoke("WallOrbitCamera", 5f);
        }
        else
        {
            Invoke("BackCamera", 2f);
        }
    }

    public void SetInitialCameraPosition()
    {
        Vector3 initialPosition = new Vector3(500, 120, -85);
        Vector3 initialRotation = new Vector3(20, 0, 0);
        Quaternion initialRot = Quaternion.Euler(initialRotation);

        transform.position = initialPosition;
        transform.rotation = initialRot;
    }

    public void WallOrbitCamera()
    {
        Cam.GetComponent<WallOrbitCamera>().enabled = true;
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