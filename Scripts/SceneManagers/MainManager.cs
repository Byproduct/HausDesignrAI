// Presentation control

// Only the intro is separated into its own Unity scene, which is already complete by the time this script launches.
// All other demo scenes are contained within this single Unity scene.

// Demo scenes change by just enabling/disabling various manager and camera scripts.
// There is no Cinemachine or other timeline/camera stuff.

using System;
using TMPro;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    public static MainManager Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    public int WorldSizeX;
    public int WorldSizeZ;

    // terrain grid on the xz axis. 0 = empty, 1 = wall, 2+ = filled space
    public short[,] terrainGrid = new short[1001, 1002];

    public GameObject Cam;
    public AudioSource JazzPlayer;  
    public TextMeshProUGUI Heading;
    private DateTime startTime;


    void Start()
    {
        startTime = DateTime.Now;
        Heading.text = "";
        WorldSizeX = 1000;
        WorldSizeZ = 1000;
        CameraManager.Instance.SetInitialCameraPosition();

        // If speed is normal, the soundtrack is already playing from the previous scene
        if (Configuration.Speed != Configuration.SpeedType.Normal)
        {
            JazzPlayer.Play();
        }
        Invoke("StartWallDrawing", 0.5f);
    }


    // Scenes in order of appearance

    public void StartWallDrawing()
    {
        Heading.text = "imagining unusual shapes";
        WallManager.Instance.Initiate();
    }

    public void StartPropagators()
    {
        Heading.text = "casting building blocks";
        CameraManager.Instance.BackCamera();
        PropagatorManager.Instance.Initiate();
        EnergyCounter.Instance.Initiate();
    }

    public void StartBuildingBlocks()
    {
        Heading.text = "choosing 100 cutest blocks";
        BuildingBlocksManager.Instance.Initiate();
    }

    /// Blocks flying up on display and "analysis"
    public void StartDisplayBlocks()
    {
        CameraManager.Instance.BlockChoosingCamera();
        DisplayBlocksManager.Instance.Initiate();
    }

    /// House building and display
    public void StartPhysicsBlocks()
    {
        PhysicsBlocksManager.Instance.Initiate();
    }

    /// Display road with all houses
    public void StartDisplayHouses()
    {
        DisplayHousesManager.Instance.Initiate();
    }

    /// Get elapsed demo time for music syncing purposes
    /// To-do: actually implement music sync  
    public TimeSpan GetElapsedTime()
    {
        return DateTime.Now - startTime;
    }
}



