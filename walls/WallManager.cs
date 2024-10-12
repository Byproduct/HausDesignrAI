using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;


public class WallManager : MonoBehaviour
{
    public static WallManager Instance { get; private set; }

    private GameManager gm;
    private static short[,] terrainGrid;

    public TextMeshProUGUI Heading;
    public GameObject Floor;

    public WallMaterialStorage wallMaterialStorage;
    public WallDrawingObjectsManager wallDrawingObjectsManager;

    public WallChunkManager wallChunkManager;
    public static int maxWallParts = 30000;

    public GameObject WallPartsParentObject;
    public List<GameObject> WallParts;
    public GameObject WallPartPrefab;
    public static int ActiveChunks { get; private set; }
    private static int wallPartIndex = 0;

    public GameObject WallDrawingObject;
    public GameObject WallDrawingObjectsHierarchy; // used just to for scene hierarchy for generated objects in the editor
    public List<GameObject> WallDrawingObjects;

    public ObjectPool<GameObject> WallPartPool;


    void Awake()
    {
        Instance = this;
    }

    public void Startup()
    {
        gm = GetComponent<GameManager>();
        terrainGrid = gm.terrainGrid;

        WallPartPool = new ObjectPool<GameObject>(CreatePooledObject, OnGetFromPool, OnReleaseToPool, OnDestroyPoolObject, false, 1000, 100000);
        WallDrawingObjects = new List<GameObject>();

        Stopwatch sw = Stopwatch.StartNew();
        UnityEngine.Debug.Log($"Startup {sw.ElapsedMilliseconds} ms - instantiated wall parts.");
        sw.Stop();

        StartCoroutine(CreateOuterWalls());


        Heading.text = "imagining unusual shapes";
        for (int i = 0; i < 40; i++)
        {
            GameObject wallDrawingObject = Instantiate(WallDrawingObject, new Vector3(0 + (i * 25), 8, 0), Quaternion.identity, WallDrawingObjectsHierarchy.transform);
            WallDrawingObjects.Add(wallDrawingObject);
        }
        InvokeRepeating("CheckForCompleteWalls", 2.0f, 0.5f);
        HandleSpeedChange(Configuration.Speed);
        Configuration.OnSpeedChanged += HandleSpeedChange;
    }

    GameObject CreatePooledObject()
    {
        Vector3 initialPosition = new Vector3(0, 8, 0);
        Vector3 initialScale = new Vector3(1, 16, 1);
        GameObject wallPart = Instantiate(WallPartPrefab, initialPosition, Quaternion.identity, WallPartsParentObject.transform);
        wallPart.name = "wall part (object pool)";
        wallPart.transform.localScale = initialScale;
        WallPart wp = wallPart.GetComponent<WallPart>();
        wp.wms = wallMaterialStorage;
        wp.numberOfMaterials = wallMaterialStorage.NumberOfMaterials;
        wp.Reset();
        WallParts.Add(wallPart);
        return wallPart;
    }
    void OnGetFromPool(GameObject obj)
    {
    }

    void OnReleaseToPool(GameObject obj)
    {
        obj.GetComponent<WallPart>().Reset();
        //WallParts.Remove(obj);
        obj.SetActive(false);
        obj.isStatic = false;
    }

    void OnDestroyPoolObject(GameObject obj)
    {
        if (obj != null)
        {
            Destroy(obj);
        }
    }

    public void SpawnWall(Vector3Int position, bool invisibleWall = false)
    {
        if (terrainGrid[position.x, position.z] != 1)
        {
            terrainGrid[position.x, position.z] = 1;
            if (invisibleWall == false)
            {
                GameObject wallPart = WallPartPool.Get();
                wallPart.transform.localPosition = position;
                wallPart.isStatic = true;
                wallPart.SetActive(true);
                wallChunkManager.AddGameObjectToChunk(wallPart);
            }
        }
    }

    // Spawn outer walls in a coroutine to avoid blocking the main thread
    IEnumerator CreateOuterWalls()
    {
        int wallsPerFrame = 30;
        int wallsCreated = 0;

        for (int z = 0; z < gm.WorldSizeZ; z++)
        {
            SpawnWall(position: new Vector3Int(0, 8, z), invisibleWall: false);
            wallsCreated++;
            if (wallsCreated >= wallsPerFrame)
            {
                yield return new WaitForFixedUpdate();
                wallsCreated = 0;
            }
            SpawnWall(position: new Vector3Int(gm.WorldSizeX, 8, z), invisibleWall: false);
            wallsCreated++;
            if (wallsCreated >= wallsPerFrame)
            {
                yield return new WaitForFixedUpdate();
                wallsCreated = 0;
            }
        }

        for (int x = 0; x < gm.WorldSizeX; x++)
        {
            SpawnWall(position: new Vector3Int(x, 8, 0), invisibleWall: false);
            wallsCreated++;
            if (wallsCreated >= wallsPerFrame)
            {
                yield return new WaitForFixedUpdate();
                wallsCreated = 0;
            }
            SpawnWall(position: new Vector3Int(x, 8, gm.WorldSizeZ), invisibleWall: false);
            wallsCreated++;
            if (wallsCreated >= wallsPerFrame)
            {
                yield return new WaitForFixedUpdate();
                wallsCreated = 0;
            }
        }
    }

    /// Periodically check if active wall drawing objects exist. If not, go to the next phase (propagator manager).
    public void CheckForCompleteWalls()
    {
        if (WallDrawingObjects.Count == 0)
        {
            Debug.Log("Walls complete.");
            Heading.text = "casting building blocks";
            Floor.SetActive(true);
            PropagatorManager.Instance.LaunchFirstPropagators();
            CancelInvoke("CheckForCompleteWalls");
            CameraManager.Instance.BackCamera();
            GetComponent<WallMeshCombiner>().WaitAndCombineAllWalls();
            EnergyCounter.Instance.StartCounting();
        }
    }

    /// Periodically check if user has changed global speed
    public void HandleSpeedChange(Configuration.SpeedType newSpeed)
    {
        float s = 79f; // Default speed for "Normal"
        switch (newSpeed)
        {
            case Configuration.SpeedType.Fast:
                s = 200f;
                break;
            case Configuration.SpeedType.Dev:
                s = 300f;
                break;
        }

        foreach (GameObject go in WallDrawingObjects)
        {
            WallDrawingObject wdo = go.GetComponent<WallDrawingObject>();
            if (wdo != null)
            {
                wdo.SetSpeed(s);
            }
        }
    }
}

