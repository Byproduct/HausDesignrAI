//Todo: Coroutines to precalc propagators and materials

using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using static Configuration;

public class PropagatorManager : MonoBehaviour
{
    public static PropagatorManager Instance { get; private set; }

    public bool propagating = false;

    public List<Vector2Int> startingPositions;
    private int startingIndex = 0;

    private PropagatorColorManager propagatorColorManager;
    private PropagatorRowManager propagatorRowManager;
    private MeshOptimizer meshOptimizer;

    private static short[,] terrainGrid;                         //terrain grid, xz coordinates
    private static int maxPropagatorObjects = 10;             // should actually be about 40000 and generate more if required
    private static int propagatorObjectIndex = 0;
    public GameManager gm;
    private int maxPropagators = 10;
    public short propagatorGroupId = 2;               // terrain grid: 0 = empty space, 1 = wall, 2+ = object or propagator   (therefore propgator groups 0 and 1 are not used)
    private float propagatorCheckFrequency = 0.05f;   // How often to check for completely filled propagator shapes
    public int propagatorSteps;                       // set in editor - default 130 - number of minimum propagator steps to run per frame
    private int propagatedThisCycle = 0;              // Number of successful propagations this cycle - if 0, launch a new propagator
    public float propagatorFadeInTime;                // set in editor - colour fading time - default 0.8
    public int colorsPerPropagator;                   // set in editor - number of colors to cycle through for each propagator - default 15
    public bool propagatorLauncherEnd = false;       // true if the entire map has been propagated
    public int DestroyedPropagators = 0;

    // Objects in Unity Editor, only for scene hierarchy
    public GameObject CombinedPropagatorParentObject;
    public GameObject PropagatorSingleSquareObjectPoolParent;

    // Sets of propagators belonging to the same area (groupId). To-do: why is this sorted? Sorting probably not needed anymore.
    public SortedDictionary<short, HashSet<Propagator>> Propagators = new();

    // Cooldown period starting from a completely filled shape (-> color fade-in -> mesh combining)
    public Dictionary<short, float> PropagatorCooldowns = new();

    // groupIds that have active propagators in them - used to check when a shape has filled completely. Active list with 0 propagators = completely filled.
    // short = groupId, int = starting row
    public Dictionary<short, int> ActivePropagatorGroups = new();

    // Visible propagator GameObjects, and an object pool for rapid creation and removal
    public Dictionary<short, HashSet<GameObject>> PropagatorObjects = new();
    public List<GameObject> PropagatorObjectPool = new();
    public ObjectPool<GameObject> SingleSquarePropagatorObjectPool;  // separate pool for single square propagator objects - no need for mesh assignments when activating/deactivating.

    // Dictionary of propagator heights for creating identical building blocks later
    public Dictionary<short, int> PropagatorHeights = new();

    // Single prefab that is cloned in the object pool
    public GameObject PropagatorObjectPrefab;


    Stopwatch propagatorFpsStopwatch = new Stopwatch();
    Stopwatch propagatorStopwatch = new Stopwatch();
    Stopwatch debugStopwatch = new Stopwatch();

    private Shader standardShader;

    //temporary lists for adding and removing objects in a batch
    private List<short> toCombine = new List<short>();
    private HashSet<(short, Propagator)> propagatorsToRemove = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        propagatorColorManager = PropagatorColorManager.Instance;
        propagatorRowManager = PropagatorRowManager.Instance;
        meshOptimizer = MeshOptimizer.Instance;

        terrainGrid = gm.terrainGrid;

        standardShader = Shader.Find("Standard");
        propagating = false;
        DestroyedPropagators = 0;

        SingleSquarePropagatorObjectPool = new ObjectPool<GameObject>(CreatePooledObject, OnGetFromPool, OnReleaseToPool, OnDestroyPoolObject, false, 10000, 100000);
        //InvokeRepeating("RestartSweep", 60f, 30f);
    }


    public void Update()
    {
        if (!propagating) return;
        propagatorFpsStopwatch.Stop();
        propagatorFpsStopwatch.Restart();
    }

    private void FixedUpdate()
    {
        RunPropagators();
        propagatorSteps = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 300,
            Configuration.SpeedType.Fast => 500,
            Configuration.SpeedType.Dev => 1000,
            _ => 130,
        };
        propagatorFadeInTime = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 1f,
            Configuration.SpeedType.Fast => 0.25f,
            Configuration.SpeedType.Dev => 0.1f,
            _ => 1f,
        };
        colorsPerPropagator = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 15,
            Configuration.SpeedType.Fast => 5,
            Configuration.SpeedType.Dev => 2,
            _ => 40,
        };
    }

    public class Propagator
    {
        private PropagatorManager propagatorManager;
        public short groupId;              // determines which group (which 3d shape) this propagator belongs to
        public int x;
        public int z;
        public int height;               // y-scale
        public Material material;        // main colour
        public Material[] materials;     // fade-in colours
        public float elapsedTime;

        // invisible propagator. For the visible gameobject there is a separate simple PropagatorObject script.
        public Propagator(PropagatorManager pm, int x, int z, int height, short groupId)
        {
            this.propagatorManager = pm;
            this.x = x;
            this.z = z;
            this.height = height;
            this.groupId = groupId;
            terrainGrid[x, z] = groupId;
            if (pm.Propagators.TryGetValue(groupId, out var propagatorsHashSet))
            {
                propagatorsHashSet.Add(this);
            }
            else
            {
                HashSet<Propagator> newhs = new HashSet<Propagator>();
                newhs.Add(this);
                pm.Propagators.Add(groupId, newhs);
            }
            pm.SpawnPropagatorObject(x, z, height, groupId);
        }
    }
    GameObject CreatePooledObject()
    {
        GameObject propagatorObject = Instantiate(PropagatorObjectPrefab, Vector3.zero, Quaternion.identity, PropagatorSingleSquareObjectPoolParent.transform);
        propagatorObject.name = "Single square propagator";
        return propagatorObject;
    }
    void OnGetFromPool(GameObject obj)
    {
    }

    void OnReleaseToPool(GameObject obj)
    {
    }

    void OnDestroyPoolObject(GameObject obj)
    {
        if (obj != null)
        {
            Destroy(obj);
        }
    }

    public void RecyclePropagatorObject(GameObject obj)
    {
        obj.SetActive(false);
        obj.isStatic = false;
        if (obj.name == "Single square propagator")
        {
            SingleSquarePropagatorObjectPool.Release(obj);
        }
        else if (obj.name == "Combined row")
        {
            obj.name = "Single square propagator";
            obj.transform.SetParent(PropagatorSingleSquareObjectPoolParent.transform);
            obj.transform.position = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            SingleSquarePropagatorObjectPool.Release(obj);
        }
    }

    // Get a new inactive propagator object from pool
    public GameObject GetInactivePropagatorObject()
    {
        int attempts = 0;
        while (true)
        {
            propagatorObjectIndex++;
            if (propagatorObjectIndex >= maxPropagatorObjects)
            {
                propagatorObjectIndex = 0;
            }
            if (PropagatorObjectPool[propagatorObjectIndex].activeSelf == false)
            {
                return PropagatorObjectPool[propagatorObjectIndex];
            }
            else
            {
                attempts++;
                if (attempts > maxPropagatorObjects + 1)
                {
                    UnityEngine.Debug.Log("Error: unable to find new propagator objects to use - quitting program.");
                    Application.Quit(1);
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#endif
                    return null;
                }
            }
        }
    }
    public void SpawnPropagatorObject(int x, int z, int height, short groupId)
    {
        if (x > 0 && x < gm.WorldSizeX)
        {
            if (z > 0 && z < gm.WorldSizeZ)
            {
                if (terrainGrid[x, z] != 0)
                {
                    GameObject propagator = SingleSquarePropagatorObjectPool.Get();
                    if (propagator != null)
                    {
                        propagator.transform.localPosition = new Vector3Int(x, (int)(height / 2), z);
                        propagator.transform.localScale = new Vector3Int(1, height, 1);
                        propagator.isStatic = true;
                        propagator.SetActive(true);
                        terrainGrid[x, z] = groupId;
                        // Add the propagator object to the list in the dictionary
                        if (PropagatorObjects.TryGetValue(groupId, out HashSet<GameObject> propaList))
                        {
                            propaList.Add(propagator);
                        }
                        else
                        {
                            HashSet<GameObject> newList = new HashSet<GameObject>();
                            newList.Add(propagator);
                            PropagatorObjects.Add(groupId, newList);
                        }
                        propagatorRowManager.AddToRow(propagator, groupId, z);
                        propagatorColorManager.AddNew(propagator.GetComponent<Renderer>(), groupId);
                    }
                    else
                    {
                        // Handle the case where no inactive propagator object is available
                    }
                }
            }
        }
    }

    public void LaunchFirstPropagators()
    {
        startingPositions = GetComponent<CalculatePropagatorLaunchPositions>().Run();
        propagating = true;
        propagatorColorManager.StartColorFades();
        for (int i = 0; i < maxPropagators; i++)
        {
            LaunchNextPropagator();
        }
        InvokeRepeating("CheckForCompletePropagators", 1f, propagatorCheckFrequency);
    }

    public void LaunchNextPropagator()
    {
        if (startingIndex >= startingPositions.Count)
        {
            propagatorLauncherEnd = true;
            return;
        }
        if (propagating && (startingIndex < startingPositions.Count))
        {
            int x = startingPositions[startingIndex].x;
            int z = startingPositions[startingIndex].y;                     // The grid is actually x,z . Z is called y here because of the Vector2Int structure.
            terrainGrid[x, z] = propagatorGroupId;
            ActivePropagatorGroups.Add(propagatorGroupId, z);
            int height = UnityEngine.Random.Range(20, 100);
            PropagatorHeights.Add(propagatorGroupId, height);
            Propagator p = new Propagator(this, x, z, height, propagatorGroupId);

            propagatorGroupId++;
            startingIndex++;
        }
    }

    public void RunPropagators()
    {
        if (propagating)
        {
            for (int steps = 0; steps < propagatorSteps; steps++)
            {
                propagatedThisCycle = 0;
                if (Propagators.Count == 0)
                {
                    LaunchNextPropagator();
                }

                // List of calls to send to TryCreatePropagator function after iteration. x, z, height, mat, groupId, direction (0 = fwd, 1 = left 2 = right)
                List<(int, int, int, short, int)> toAdd = new List<(int, int, int, short, int)>();

                // List of propagators to remove after iteration (groupId, Propagator)
                propagatorsToRemove.Clear();

                foreach (var propaList in Propagators.Values)
                {
                    if (propaList.Count > 0)
                    {
                        foreach (Propagator p in propaList)
                        {
                            steps++;
                            if (steps > propagatorSteps)
                            {
                                break;
                            }
                            int x = p.x;
                            int z = p.z;
                            int height = p.height;
                            short groupId = p.groupId;
                            Material mat = p.material;

                            toAdd.Add((x - 1, z, height, groupId, 1));
                            toAdd.Add((x + 1, z, height, groupId, 2));
                            toAdd.Add((x, z + 1, height, groupId, 0));
                            propagatorsToRemove.Add((groupId, p));
                        }
                    }

                    foreach (var v in propagatorsToRemove)
                    {
                        Propagators[v.Item1].Remove(v.Item2);
                    }
                    propagatorsToRemove.Clear();
                    foreach (var v in toAdd)
                    {
                        TryCreatePropagator(v.Item1, v.Item2, v.Item3, v.Item4, v.Item5);
                    }
                    toAdd.Clear();
                }
                //if (steps < propagatorSteps)
                //{
                //    LaunchNextPropagator();
                //}
                if (propagatedThisCycle == 0)
                {
                    LaunchNextPropagator();
                }
            }
        }
    }

    // To-fix: colliding propagators can make the row in question not register as complete and merge due to propagation stopping at the collision point and sometimes no new propagators arriving from elsewhere. Fix to remove unnecessary verts from complete objects 
    private void TryCreatePropagator(int newX, int newZ, int height, short groupId, int direction)
    {
        // square is empty, so go forth and multiply
        if (terrainGrid[newX, newZ] == 0)
        {
            Propagator newPropagator = new Propagator(this, newX, newZ, height, groupId);
            terrainGrid[newX, newZ] = groupId;
            propagatedThisCycle++;
        }
        // else unable to create propagator
        else
        {
            if ((direction == 1) || (direction == 2))
            {
                // If the occupied square is a wall, mark that side as complete.
                if (terrainGrid[newX, newZ] == 1)
                {
                    // If the row status exists, mark that side as complete. If not, create a new status.
                    if (propagatorRowManager.PropagatorRowStatuses.TryGetValue((groupId, newZ), out PropagatorRowStatus prs))
                    {
                        if (direction == 1)
                        {
                            prs.CompletedLeft();
                        }
                        if (direction == 2)
                        {
                            prs.CompletedRight();
                        }
                    }
                    else
                    {
                        PropagatorRowStatus newPrs = new PropagatorRowStatus(groupId, newZ);
                        if (direction == 1)
                        {
                            newPrs.CompletedLeft();
                        }
                        if (direction == 2)
                        {
                            newPrs.CompletedRight();
                        }
                        propagatorRowManager.PropagatorRowStatuses.Add((groupId, newZ), newPrs);
                    }
                }
            }
        }
    }

    // Runs periodically (e.g. every 200ms), not every frame
    // Check if a shape has filled completely. This is the case if the group exists as "active" but has 0 propagators remaining.
    // For a filled shape, activate a cooldown (to wait for colour fade to complete), after which combine the meshes of the group.
    public void CheckForCompletePropagators()
    {
        toCombine.Clear();  // Keys to combine and remove after iteration

        foreach (short groupId in ActivePropagatorGroups.Keys)
        {
            if ((Propagators.ContainsKey(groupId)) && (Propagators[groupId].Count == 0))
            {
                if (PropagatorCooldowns.ContainsKey(groupId) == false)
                {
                    //UnityEngine.Debug.Log($"Shape {groupId} filled completely - starting cooldown");
                    PropagatorCooldowns.Add(groupId, 0);
                }
            }
        }
        foreach (var entry in PropagatorCooldowns.ToList())
        {
            PropagatorCooldowns[entry.Key] += propagatorCheckFrequency;
            if (PropagatorCooldowns[entry.Key] > propagatorFadeInTime + 0.5f)
            {
                //UnityEngine.Debug.Log($"Shape {entry.Key} cooldown complete - combining meshes.");
                toCombine.Add(entry.Key);
            }
        }
        foreach (var key in toCombine)
        {
            meshOptimizer.SpawnNewObject(key);
            PostCombineCleanup(key);
        }
    }

    /// After combining propagator objects into single mesh, clean up all associated gameobjects and data
    public void PostCombineCleanup(short groupId)
    {
        PropagatorCooldowns.Remove(groupId);
        if (PropagatorObjects.TryGetValue(groupId, out HashSet<GameObject> objects))
        {
            foreach (GameObject obj in objects)
            {
                RecyclePropagatorObject(obj); // deactivate and return to pool
            }
            objects.Clear();
            PropagatorObjects.Remove(groupId);
        }
        if (Propagators.TryGetValue(groupId, out var propaList))
        {
            propaList.Clear();
            Propagators.Remove(groupId);
        }
        if (ActivePropagatorGroups.Keys.Contains(groupId))
        {
            ActivePropagatorGroups.Remove(groupId);
        }
        if (propagatorRowManager.CombinedRows.TryGetValue(groupId, out var propaObjList))
        {
            propaObjList.Clear();
            propagatorRowManager.CombinedRows.Remove(groupId);
        }
        List<(short, int)> keysToRemove = new List<(short, int)>();
        foreach (var key in propagatorRowManager.PropagatorObjectsInRow.Keys)
        {
            if (key.Item1 == groupId)
            {
                keysToRemove.Add(key);
            }
        }
        foreach (var key in keysToRemove)
        {
            propagatorRowManager.PropagatorObjectsInRow.Remove(key);
        }
    }
}
