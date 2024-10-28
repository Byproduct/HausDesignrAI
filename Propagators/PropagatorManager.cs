// Each separate building block has a unique numerical id starting from 2 upwards.
// "Group id" refers to propagators belonging to that specific block.
// For example, when block #2 is being propagated, there are multiple active propagator objects that belong to propagator group id #2.
// Numerical entries in dictionaries, lists etc. refer to this group id.

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class PropagatorManager : MonoBehaviour
{
    public static PropagatorManager Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    // Unity objects
    public TextMeshProUGUI Heading;
    public GameObject CombinedPropagatorParentObject;
    public GameObject PropagatorSingleSquareObjectPoolParent;

    // Values determined by speed setting
    public int PropagatorSteps;             // Overall propagation speed (number of minimum propagator steps to run per frame) - affects fps and duration of the scene
    public float PropagatorFadeInTime;      // Colour fading time affects visuals and fps
    public int ColorsPerPropagator;         // Number of colors to cycle through for each propagator - affects visuals and fps - initial value must be set in editor for color genration at the beginning

    // Values fetched from MainManager to avoid repeated calls to it
    private int worldSizeX;
    private int worldSizeZ;
    private static short[,] terrainGrid;

    // Propagator management
    public bool AllPropagatorsComplete;                      // True if the entire map has been propagated
    public bool Propagating;
    public short PropagatorGroupId = 2;                     // Terrain grid: 0 = empty space, 1 = wall, 2+ = propagator
    private const int initialPropagators = 5;               // A small jumpstart to the scene, starting more than 1 propagator immediately              
    private const float propagatorCheckFrequency = 0.1f;    // Rate at which to check for completely filled shapes (should check often but not every frame)
    private List<Vector2Int> startingPositions;
    private int startingPropagatorId = 0;
    private bool propagatedThisCycle;                       // Number of successful propagations this cycle - if 0, launch a new propagator

    // Sets of propagators belonging to the same block (groupId).
    public Dictionary<short, HashSet<Propagator>> Propagators = new();

    // Cooldown periods starting from a completely filled shape.
    // Cooldown is for the visual colour fade to complete, after which mesh optimization occurs.
    public Dictionary<short, float> PropagatorCooldowns = new();

    // GroupIds that have active propagators in them
    // Used to check if a shape has filled completely: active list with 0 propagators remaining = completely filled shape.
    public List<short> ActivePropagatorGroups = new();

    // Visible propagator GameObjects
    public Dictionary<short, HashSet<GameObject>> PropagatorGameObjects = new();

    // Separate object pools for Propagators and their corresponding visible GameObjects
    public ObjectPool<Propagator> PropagatorPool;
    public ObjectPool<GameObject> PropagatorGameObjectPool;

    // Single prefab that is cloned in the object pool
    public GameObject PropagatorObjectPrefab;

    // Heights of blocks vary, and it must be remembered when spawning new propagators and optimizing meshes.
    public Dictionary<short, int> BlockHeights = new();

    private Shader standardShader;

    public class Propagator
    {
        public short GroupId;            // indicates which block this propagator belongs to
        public int X;
        public int Z;

        public Propagator() { }         // empty constructor (init called separately)

        /// When a propagator is initialized, mark the terrain grid accordingly,
        /// add it to the dictionary of propagators, and spawn corresponding GameObject
        public void Initialize(int x, int z, short groupId)
        {
            this.X = x;
            this.Z = z;
            this.GroupId = groupId;
            terrainGrid[x, z] = groupId;
            PropagatorManager pm = PropagatorManager.Instance;
            if (pm.Propagators.TryGetValue(groupId, out var propagatorsHashSet))
            {
                propagatorsHashSet.Add(this);
            }
            else
            {
                pm.Propagators.Add(groupId, new HashSet<Propagator> { this });
            }
            pm.SpawnPropagatorGameObject(x, z, groupId);
        }
    }

    void Start()
    {
        Propagating = false;
        terrainGrid = MainManager.Instance.terrainGrid;
        worldSizeX = MainManager.Instance.WorldSizeX;
        worldSizeZ = MainManager.Instance.WorldSizeZ;
        standardShader = Shader.Find("Standard");

        PropagatorPool = new ObjectPool<Propagator>(
    () => new Propagator(),   // Empty constructor for new objects
    null,                     // OnGetFromPool = null
    null,                     // OnReleaseToPool = null
    null,                     // OnDestroyPooledObject = null
    false,                    // No collection check
    100,                      // Default size
    100000                    // Max size
);

        PropagatorGameObjectPool = new ObjectPool<GameObject>(
    CreatePooledGameObject,   // Create function is defined separately
    null,                     // OnGetFromPool = null
    null,                     // OnReleaseToPool = null
    DestroyPooledGameObject,  // Destroy function is defined separately
    false,                    // No collection check
    10000,                    // Default size
    100000                    // Max size
);
    }

    /// Initiate propagation phase
    public void Initiate()
    {
        startingPositions = PropagatorLaunchPositionsCalculator.Instance.Run();
        Propagating = true;
        PropagatorColorManager.Instance.ActivateColorFades();
        for (int i = 0; i < initialPropagators; i++)
        {
            LaunchNextPropagator();
        }
        InvokeRepeating("CheckForCompletePropagators", 1f, propagatorCheckFrequency);
        InvokeRepeating("CheckForAllPropagatorsComplete", 2.0f, 0.5f);
    }


    private void FixedUpdate()
    {
        RunPropagators();
        CheckForSpeedChange();
    }


    // -- Methods of object pools

    GameObject CreatePooledGameObject()
    {
        GameObject propagatorObject = Instantiate(PropagatorObjectPrefab, Vector3.zero, Quaternion.identity, PropagatorSingleSquareObjectPoolParent.transform);
        propagatorObject.name = "Single square propagator";
        return propagatorObject;
    }

    void DestroyPooledGameObject(GameObject obj)
    {
        if (obj != null)
        {
            Destroy(obj);
        }
    }

    public void RecyclePropagatorGameObject(GameObject obj)
    {
        obj.SetActive(false);
        obj.isStatic = false;
        if (obj.name == "Single square propagator")
        {
            PropagatorGameObjectPool.Release(obj);
        }
        else if (obj.name == "Combined row")
        {
            obj.name = "Single square propagator";
            obj.transform.SetParent(PropagatorSingleSquareObjectPoolParent.transform);
            obj.transform.position = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            PropagatorGameObjectPool.Release(obj);
        }
    }

    // --

    public void SpawnPropagatorGameObject(int x, int z, short groupId)
    {
        GameObject propagatorObject = PropagatorGameObjectPool.Get();
        if (propagatorObject != null)
        {
            int height = BlockHeights[groupId];
            propagatorObject.transform.localPosition = new Vector3Int(x, (int)(height / 2), z);
            propagatorObject.transform.localScale = new Vector3Int(1, height, 1);
            propagatorObject.isStatic = true;
            propagatorObject.SetActive(true);

            // Add the propagator object to the group in the dictionary, or create the group if it doesn't yet exist
            if (PropagatorGameObjects.TryGetValue(groupId, out HashSet<GameObject> propagatorGameObjectSet))
            {
                propagatorGameObjectSet.Add(propagatorObject);
            }
            else
            {
                HashSet<GameObject> newSet = new HashSet<GameObject> { propagatorObject };
                PropagatorGameObjects.Add(groupId, newSet);
            }
            PropagatorRowManager.Instance.AddToRow(propagatorObject, groupId, z);
            PropagatorColorManager.Instance.AddNew(propagatorObject.GetComponent<Renderer>(), groupId);
        }
    }

    public void RunPropagators()
    {
        if (Propagating)
        {
            for (int steps = 0; steps < PropagatorSteps; steps++)
            {
                propagatedThisCycle = false;
                if (Propagators.Count == 0)
                {
                    LaunchNextPropagator();
                }

                // List of propagators to add - i.e. list of calls to send to TryCreatePropagator method
                // Parameters x-coordinate, z-coordinate, groupId, direction (0 = fwd, 1 = left 2 = right)
                List<(int, int, short, int)> toAdd = new List<(int, int, short, int)>();

                // List of propagators to remove - as soon as they are done propagating
                HashSet<(short, Propagator)> propagatorsToRemove = new();

                foreach (var propaList in Propagators.Values)
                {
                    if (propaList.Count > 0)
                    {
                        foreach (Propagator p in propaList)
                        {
                            steps++;
                            if (steps > PropagatorSteps)
                            {
                                break;
                            }
                            int x = p.X;
                            int z = p.Z;
                            short groupId = p.GroupId;

                            toAdd.Add((x - 1, z, groupId, 1));
                            toAdd.Add((x + 1, z, groupId, 2));
                            toAdd.Add((x, z + 1, groupId, 0));
                            propagatorsToRemove.Add((groupId, p));
                        }
                    }

                    // As soon as a propagator is done propagating, remove it from the dictionary of propagators and release the pooled object
                    foreach (var v in propagatorsToRemove)
                    {
                        Propagators[v.Item1].Remove(v.Item2);
                        PropagatorPool.Release(v.Item2);
                    }
                    propagatorsToRemove.Clear();

                    foreach (var v in toAdd)
                    {
                        TryCreatePropagator(v.Item1, v.Item2, v.Item3, v.Item4);
                    }
                    toAdd.Clear();
                }
                if (!propagatedThisCycle)
                {
                    LaunchNextPropagator();
                }
            }
        }
    }

    /// When no shapes are currently being filled, launch a new propagator at the next starting position
    public void LaunchNextPropagator()
    {
        // If about to launch a propagator with a higher id than the number of total starting positions,
        // abort and mark propagation phase as complete
        if (startingPropagatorId >= startingPositions.Count)
        {
            AllPropagatorsComplete = true;
            return;
        }
        if (Propagating && (startingPropagatorId < startingPositions.Count))
        {
            int x = startingPositions[startingPropagatorId].x;
            int z = startingPositions[startingPropagatorId].y;  // the second value of Vector2 is called "y" but here refers to the z-coordinate.
            ActivePropagatorGroups.Add(PropagatorGroupId);
            int height = Random.Range(20, 100);
            BlockHeights.Add(PropagatorGroupId, height);
            Propagator p = PropagatorPool.Get();
            p.Initialize(x, z, PropagatorGroupId);
            PropagatorGroupId++;
            startingPropagatorId++;
        }
    }

    private void TryCreatePropagator(int newX, int newZ, short groupId, int direction)
    {
        // if the square is empty, go forth and multiply
        if (terrainGrid[newX, newZ] == 0)
        {
            Propagator newPropagator = PropagatorPool.Get();
            newPropagator.Initialize(newX, newZ, groupId);
            propagatedThisCycle = true;
        }
        // If not, unable to create propagator. In that case check if the row is complete on either side.
        else
        {
            // If the occupied square is a wall, and the direction is to the side, mark that side as complete.
            if (terrainGrid[newX, newZ] == 1)
            {
                if ((direction == 1) || (direction == 2))
                {
                    // If the row status exists, mark that side as complete. If not, create a new status.
                    if (PropagatorRowManager.Instance.PropagatorRowStatuses.TryGetValue((groupId, newZ), out PropagatorRowStatus prs))
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
                        PropagatorRowManager.Instance.PropagatorRowStatuses.Add((groupId, newZ), newPrs);
                    }
                }
            }
        }
    }

    /// Check if a shape has filled completely. This is the case if the group exists as "active" but has 0 propagators remaining.
    /// For a filled shape, activate a cooldown (to wait for colour fade to complete), after which combine the meshes of the group.
    /// Runs periodically, not every frame
    public void CheckForCompletePropagators()
    {
        Util.WriteVerboseLog($"Pools: Propagator{PropagatorPool.CountAll} PropagatorGameObject{PropagatorGameObjectPool.CountAll}");

        List<short> toCombine = new List<short>();
        foreach (short groupId in ActivePropagatorGroups)
        {
            if ((Propagators.ContainsKey(groupId)) && (Propagators[groupId].Count == 0))
            {
                if (PropagatorCooldowns.ContainsKey(groupId) == false)
                {
                    Util.WriteVerboseLog($"Shape {groupId} filled completely - starting cooldown");
                    PropagatorCooldowns.Add(groupId, 0);
                }
            }
        }
        foreach (var entry in PropagatorCooldowns.ToList())
        {
            PropagatorCooldowns[entry.Key] += propagatorCheckFrequency;
            if (PropagatorCooldowns[entry.Key] > PropagatorFadeInTime + 0.5f)
            {
                Util.WriteVerboseLog($"Shape {entry.Key} cooldown complete - combining meshes.");
                toCombine.Add(entry.Key);
            }
        }
        foreach (var key in toCombine)
        {
            MeshSimplifier.Instance.SpawnNewObject(key);
            PostCombineCleanup(key);
        }
    }

    /// After combining propagator objects into single mesh, clean up all associated gameobjects and data
    public void PostCombineCleanup(short groupId)
    {
        PropagatorCooldowns.Remove(groupId);
        if (PropagatorGameObjects.TryGetValue(groupId, out HashSet<GameObject> objects))
        {
            foreach (GameObject obj in objects)
            {
                RecyclePropagatorGameObject(obj); // deactivate and return to pool
            }
            objects.Clear();
            PropagatorGameObjects.Remove(groupId);
        }
        if (Propagators.TryGetValue(groupId, out var propaList))
        {
            propaList.Clear();
            Propagators.Remove(groupId);
        }
        if (ActivePropagatorGroups.Contains(groupId))
        {
            ActivePropagatorGroups.Remove(groupId);
        }
        if (PropagatorRowManager.Instance.CombinedRows.TryGetValue(groupId, out var propaObjList))
        {
            propaObjList.Clear();
            PropagatorRowManager.Instance.CombinedRows.Remove(groupId);
        }
        List<(short, int)> keysToRemove = new List<(short, int)>();
        foreach (var key in PropagatorRowManager.Instance.PropagatorObjectsInRow.Keys)
        {
            if (key.Item1 == groupId)
            {
                keysToRemove.Add(key);
            }
        }
        foreach (var key in keysToRemove)
        {
            PropagatorRowManager.Instance.PropagatorObjectsInRow.Remove(key);
        }
    }

    /// Periodically check if all propagators are complete. If so, start the next phase in the demo (building blocks manager).
    public void CheckForAllPropagatorsComplete()
    {
        if ((Propagating == true) && (AllPropagatorsComplete == true) && (PropagatorCooldowns.Count == 0))
        {
            Util.WriteLog("Propagation complete.");
            CancelInvoke("CheckForCompletePropagators");
            CancelInvoke("CheckForAllPropagatorsComplete");
            MainManager.Instance.StartBuildingBlocks();
        }
    }

    /// Check if user has changed program speed
    public void CheckForSpeedChange()
    {
        PropagatorSteps = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 300,
            Configuration.SpeedType.Fast => 480,
            Configuration.SpeedType.Dev => 1000,
            _ => 300,
        };
        PropagatorFadeInTime = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 1f,
            Configuration.SpeedType.Fast => 0.25f,
            Configuration.SpeedType.Dev => 0.1f,
            _ => 1f,
        };
        ColorsPerPropagator = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 15,
            Configuration.SpeedType.Fast => 5,
            Configuration.SpeedType.Dev => 2,
            _ => 15,
        };
    }
}
