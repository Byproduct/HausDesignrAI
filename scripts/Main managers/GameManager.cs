using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private PropagatorManager propagatorManager;
    private BuildingBlocksManager buildingBlocksManager;

    public int WorldSizeX;
    public int WorldSizeZ;

    // terrain grid on the xz axis. 0 = empty, 1 = wall, 2+ = filled space
    public short[,] terrainGrid = new short[1001, 1002];

    public GameObject Cam;
    public AudioSource JazzPlayer;
   
    public TextMeshProUGUI Heading;

    private static bool consoleDebug = false;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        Cam.GetComponent<CameraPositions>().InitialCamera();
        Heading.text = $"";
        propagatorManager = PropagatorManager.Instance;
        WorldSizeX = 1000;
        WorldSizeZ = 1000;
        short[,] terrainGrid = new short[WorldSizeX + 1, WorldSizeZ + 2];
        InvokeRepeating("CheckForCompletePropagators", 2.0f, 0.5f);
        Invoke("StartWallDrawing", 0.5f);
        // Music for normal speed is already playing from the previous scene if applicable
        if (Configuration.Speed != Configuration.SpeedType.Normal)
        {
            JazzPlayer.Play();
        }
    }

    private void StartWallDrawing()
    {
        WallManager.Instance.Startup();
    }



    // Periodically check if all propagators are complete. If so, start the next phase (building blocks manager).
    public void CheckForCompletePropagators()
    {
        if ((propagatorManager.propagating == true) && (propagatorManager.propagatorLauncherEnd == true) && (propagatorManager.PropagatorCooldowns.Count == 0))
        {
            Debug.Log("Propagation complete.");
            CancelInvoke("CheckForCompletePropagators");
            buildingBlocksManager = BuildingBlocksManager.Instance;
            buildingBlocksManager.ActivateBuildingBlocks();
        }
    }

}
