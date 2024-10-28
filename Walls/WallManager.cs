//This script manages walls, their object pool, as well as the objects that draw walls as they move.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public class WallManager : MonoBehaviour
{
    public static WallManager Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    private static short[,] terrainGrid;
    public static int ActiveChunks { get; private set; }

    public GameObject Floor;

    public GameObject WallPartPrefab;
    public List<GameObject> WallParts;
    public GameObject WallPartsParent;
    public ObjectPool<GameObject> WallPartPool;

    public GameObject WallDrawingObject;
    public GameObject WallDrawingObjectsParent;
    public List<GameObject> WallDrawingObjects;

    private static int wallPartIndex = 0;


    public void Initiate()
    {
        terrainGrid = MainManager.Instance.terrainGrid;
        WallPartPool = new ObjectPool<GameObject>(CreatePooledObject, OnGetFromPool, OnReleaseToPool, OnDestroyPoolObject, false, 1000, 100000);
        WallDrawingObjects = new List<GameObject>();

        StartCoroutine(CreateOuterWalls());

        for (int i = 0; i < 40; i++)
        {
            GameObject wallDrawingObject = Instantiate(WallDrawingObject, new Vector3(0 + (i * 25), 8, 0), Quaternion.identity, WallDrawingObjectsParent.transform);
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
        GameObject wallPart = Instantiate(WallPartPrefab, initialPosition, Quaternion.identity, WallPartsParent.transform);
        wallPart.name = "wall part (object pool)";
        wallPart.transform.localScale = initialScale;
        WallColorFade wcf = wallPart.GetComponent<WallColorFade>();
        wcf.Reset();
        WallParts.Add(wallPart);
        return wallPart;
    }
    void OnGetFromPool(GameObject obj)
    {
    }

    void OnReleaseToPool(GameObject obj)
    {
        obj.GetComponent<WallColorFade>().Reset();
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
                WallChunkManager.Instance.AddGameObjectToChunk(wallPart);
            }
        }
    }

    // Spawn outer walls in a coroutine to avoid blocking the main thread
    IEnumerator CreateOuterWalls()
    {
        int wallsPerFrame = 30;
        int wallsCreated = 0;

        int maxX = MainManager.Instance.WorldSizeX;
        int maxZ = MainManager.Instance.WorldSizeZ;
        List<Vector3Int> outerWallPositions = new List<Vector3Int>();

        // Collect positions of all outer walls 
        for (int z = 0; z < maxZ; z++)
        {
            outerWallPositions.Add(new Vector3Int(0, 8, z));     // west (x = 0)
            outerWallPositions.Add(new Vector3Int(maxX, 8, z));  // east (x = max)
        }

        for (int x = 0; x < maxX; x++)
        {
            outerWallPositions.Add(new Vector3Int(x, 8, 0));     // south (z = 0)
            outerWallPositions.Add(new Vector3Int(x, 8, maxZ));  // north (z = max)
        }

        // Spawn walls gradually
        yield return new WaitForFixedUpdate();
        foreach (var position in outerWallPositions)
        {
            SpawnWall(position, invisibleWall: false);
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
            Util.WriteLog("Walls complete.");
            Floor.SetActive(true);
            CancelInvoke("CheckForCompleteWalls");
            GetComponent<WallMeshCombiner>().WaitAndCombineAllWalls();
            MainManager.Instance.StartPropagators();
        }
    }

    /// Periodically check if user has changed global speed
    public void HandleSpeedChange(Configuration.SpeedType newSpeed)
    {
        float s = 79f;    // Default speed for "Normal"
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

