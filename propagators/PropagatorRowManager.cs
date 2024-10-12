using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Class to keep track of individual rows inside shapes
// Once a row is filled completely, cooldown is started. After cooldown, objects in row are merged into one object.
// Cooldowns can start multiple times, as propagators can run into another inside the same object
public class PropagatorRowStatus
{
    public bool leftIsComplete;
    public bool rightIsComplete;
    public bool cooldownStarted;
    public short groupId;
    public int rowNum;
    private PropagatorRowManager prm;

    public PropagatorRowStatus(short groupId, int rowNum)
    {
        leftIsComplete = false;
        rightIsComplete = false;
        cooldownStarted = false;
        this.groupId = groupId;
        this.rowNum = rowNum;
        prm = PropagatorRowManager.Instance;
    }

    public void CompletedLeft()
    {
        leftIsComplete = true;
        checkComplete();
    }

    public void CompletedRight()
    {
        rightIsComplete = true;
        checkComplete();
    }

    private void checkComplete()
    {
        if (leftIsComplete && rightIsComplete && cooldownStarted == false)
        {
            cooldownStarted = true;
            prm.AddCooldown(groupId, rowNum); // Add or refresh cooldown
        }
    }
}

// Class to manage individual propagator rows that are combined into single objects
public class PropagatorRowManager : MonoBehaviour
{
    public static PropagatorRowManager Instance { get; private set; }

    public PropagatorManager propagatorManager;
    public PropagatorColorManager propagatorColorManager;

    // (groupId, row) - rows of individual propagator objects within single shapes. Kept track of for mesh combining.
    public Dictionary<(short, int), List<GameObject>> PropagatorObjectsInRow = new();

    // (groupId, row) - tracks which rows are completed, on cooldown, and ready to merge 
    public Dictionary<(short, int), PropagatorRowStatus> PropagatorRowStatuses = new();

    // SortedList of cooldowns, kept track of with Time.Time
    public SortedDictionary<float, (short, int)> PropagatorRowCooldowns = new();

    // (groupId) - list of already combined rows - if the list is large (very tall overall object), combine some of those meshes too
    public Dictionary<short, HashSet<GameObject>> CombinedRows = new();

    // Temporary list for removals in UpdateCooldowns
    private List<(short, int)> toRemove = new();

    private float meshCombineInterval = 0.5f;          // for the intermediate mesh updating above
    private int meshCombineThreshold = 25;             // how many rows required before their meshes are combined
    private float cooldownUpdateInterval = 0.05f;      // update rate of all cooldowns (not required every frame, but too sparse can cause several mesh combining operations in one frame)
    public GameObject CombinedPropagatorParentObject;  // Object only for editor scene hierarchy

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        propagatorManager = PropagatorManager.Instance;
        propagatorColorManager = PropagatorColorManager.Instance;
        PropagatorObjectsInRow = new Dictionary<(short, int), List<GameObject>>();  // (groupId, rowNum)
        PropagatorRowStatuses = new Dictionary<(short, int), PropagatorRowStatus>();
        PropagatorRowCooldowns = new SortedDictionary<float, (short, int)>();
        CombinedRows = new Dictionary<short, HashSet<GameObject>>();
        toRemove = new List<(short, int)>();
        InvokeRepeating("UpdateCooldowns", 1.0f, cooldownUpdateInterval);
        InvokeRepeating("CombineRowMeshes", 1.0f, meshCombineInterval);
    }

    public void AddToRow(GameObject obj, short groupId, int rowNum)
    {
        var key = (groupId, rowNum);

        if (PropagatorObjectsInRow.TryGetValue(key, out List<GameObject> objList))
        {
            objList.Add(obj);
        }
        else
        {
            PropagatorObjectsInRow[key] = new List<GameObject> { obj };
        }
    }

    // Remove all from groupId in case of a collision
    public void RemoveAll(short groupId)
    {
        foreach ((short, int) key in PropagatorObjectsInRow.Keys)
        {
            if (key.Item1 == groupId)
            {
                PropagatorObjectsInRow[key].Clear();
            }
        }
        foreach ((short, int) key in PropagatorRowStatuses.Keys.ToList())
        {
            if (key.Item1 == groupId)
            {
                PropagatorRowStatuses.Remove(key);
            }
        }
        foreach (float key in PropagatorRowCooldowns.Keys.ToList())
        {
            if (PropagatorRowCooldowns[key].Item1 == groupId)
            {
                PropagatorRowCooldowns.Remove(key);
            }
        }
    }
    // to-do: cooldowns bugged - track by id and rownum to debug
    public void AddCooldown(short groupId, int rowNum)
    {
        float key = Time.time + 1.15f;
        //// Check and remove existing entry with the same groupId and rowNum
        //// There shouldn't be duplicates to remove, but this can be enabled for debugging purposes.
        //var existingKey = PropagatorRowCooldowns.FirstOrDefault(kvp => kvp.Value.Item1 == groupId && kvp.Value.Item2 == rowNum).Key;
        //if (!float.IsNaN(existingKey)) // Check if a key was found
        //{
        //    PropagatorRowCooldowns.Remove(existingKey);
        //}

        // Ensure the new key is unique
        while (PropagatorRowCooldowns.ContainsKey(key))
        {
            key += 0.001f;
        }
        PropagatorRowCooldowns.Add(key, (groupId, rowNum));
    }


    public List<GameObject> GetObjectsInRow(short groupId, int rowNum)
    {
        var key = (groupId, rowNum);
        if (PropagatorObjectsInRow.TryGetValue(key, out var objects))
        {
            return objects;
        }
        return new List<GameObject>(); // Return an empty list if no objects are found
    }

    private void UpdateCooldowns()
    {
        while (PropagatorRowCooldowns.Count > 0)
        {
            var firstKey = PropagatorRowCooldowns.Keys.First();
            if (Time.time > firstKey)
            {
                var firstValue = PropagatorRowCooldowns[firstKey];
                CombineRowObjects(firstValue);
                PropagatorRowStatuses.Remove(firstValue);
                PropagatorRowCooldowns.Remove(firstKey);
            }
            else
            {
                break;
            }
        }
    }


    private void CombineRowObjects((short, int) pair)
    {
        short groupId = pair.Item1;
        int rowNum = pair.Item2;

        List<GameObject> rowItems = GetObjectsInRow(groupId, rowNum);
        if (rowItems.Count > 0)
        {
            Dictionary<short, HashSet<GameObject>> po = propagatorManager.PropagatorObjects;
            Renderer renderer = new Renderer();
            float min = float.MaxValue;
            float max = float.MinValue;
            float xPosition = 0.0f;

            // Get the smallest and largest x-values of the objects in the row
            foreach (GameObject obj in rowItems)
            {
                xPosition = obj.transform.position.x;
                if (xPosition < min)
                {
                    min = xPosition;
                }
                if (xPosition > max)
                {
                    max = xPosition;
                }
            }
            int minX = Mathf.RoundToInt(min);
            int maxX = Mathf.RoundToInt(max);
            int rowLength = maxX - minX + 1;
            //Debug.Log($"Combining {minX}, {maxX}, length {rowLength}");

            GameObject combinedRowObject = propagatorManager.SingleSquarePropagatorObjectPool.Get();
            if (combinedRowObject != null)
            {
                combinedRowObject.name = "Combined row";
                GameObject firstItem = rowItems[0];   // a random item in the row, to get position, scale and material from
                combinedRowObject.transform.position = new Vector3(minX + ((float)rowLength / 2 - 0.5f), firstItem.transform.position.y, rowNum);
                combinedRowObject.transform.localScale = new Vector3(rowLength, firstItem.transform.localScale.y, 1);

                //Set color in a silly but faster way
                renderer = combinedRowObject.GetComponent<Renderer>();
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color", propagatorColorManager.GetFinalColor(groupId));
                renderer.SetPropertyBlock(propBlock);

                combinedRowObject.isStatic = true;
                combinedRowObject.SetActive(true);
                if (po.TryGetValue(groupId, out HashSet<GameObject> propaObjList))
                {
                    propaObjList.Add(combinedRowObject);
                }
                if (CombinedRows.TryGetValue(groupId, out propaObjList))
                {
                    propaObjList.Add(combinedRowObject);
                }
                else
                {
                    CombinedRows[groupId] = new HashSet<GameObject>();
                    CombinedRows[groupId].Add(combinedRowObject);
                }
                foreach (GameObject obj in rowItems)
                {
                    propagatorManager.RecyclePropagatorObject(obj); // deactivate and return to pool
                    po[groupId].Remove(obj);
                }
            }
        }
    }

    // for commented version see WallMeshCombiner.cs
    private void CombineRowMeshes()
    {
        foreach (var combinedRow in CombinedRows)
        {
            if (combinedRow.Value.Count > meshCombineThreshold)
            {
                //                Debug.Log($"Group {groupId.Key} has {objects.Count} merged rows - combining meshes.");

                // Hashset converted into list and sorted. I assume the combine meshes will perform better when adjacent parts have adjacent indices / vertices / etc, but haven't tested this.
                List<GameObject> sortedObjects = new List<GameObject>(combinedRow.Value);
                sortedObjects.Sort((gameObject1, gameObject2) => gameObject1.transform.position.z.CompareTo(gameObject2.transform.position.z));
                Vector3 worldPosition = sortedObjects[0].transform.position;
                GameObject combinedObject = propagatorManager.SingleSquarePropagatorObjectPool.Get();
                var mr = combinedObject.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.sharedMaterial = sortedObjects[0].GetComponent<MeshRenderer>().sharedMaterial;
                combinedObject.transform.SetParent(CombinedPropagatorParentObject.transform);
                combinedObject.transform.position = worldPosition;
                combinedObject.transform.localScale = Vector3.one;

                //Set color in a silly but faster way
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                Renderer r = combinedObject.GetComponent<Renderer>();
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color", propagatorColorManager.GetFinalColor(combinedRow.Key));
                r.SetPropertyBlock(propBlock);

                CombineInstance[] combine = new CombineInstance[sortedObjects.Count];
                int totalVertexCount = 0;
                int i = 0;
                foreach (GameObject obj in sortedObjects)
                {
                    if (obj.GetComponent<MeshFilter>() != null && obj.GetComponent<MeshRenderer>() != null)
                    {
                        combine[i].mesh = obj.GetComponent<MeshFilter>().mesh;
                        totalVertexCount += combine[i].mesh.vertexCount;
                        combine[i].transform = Matrix4x4.Translate(-worldPosition) * obj.transform.localToWorldMatrix;
                        i++;
                    }
                }
                combinedObject.name = $"Combined chunk of rows - {sortedObjects.Count} objects, {totalVertexCount} verts";

                Mesh combinedMesh = new Mesh();
                combinedMesh.indexFormat = totalVertexCount > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                combinedMesh.CombineMeshes(combine, true, true);
                combinedObject.GetComponent<MeshFilter>().mesh = combinedMesh;
                combinedObject.isStatic = true;
                combinedObject.SetActive(true);
                if (propagatorManager.PropagatorObjects.TryGetValue(combinedRow.Key, out HashSet<GameObject> propaObjList))
                {
                    propaObjList.Add(combinedObject);
                }
                else
                {
                    propagatorManager.PropagatorObjects[combinedRow.Key] = new HashSet<GameObject> { combinedObject };
                }
                foreach (GameObject obj in sortedObjects.ToList())
                {
                    propagatorManager.RecyclePropagatorObject(obj);  // deactivate and return to pool
                    CombinedRows[combinedRow.Key].Remove(obj);
                    propagatorManager.PropagatorObjects[combinedRow.Key].Remove(obj);
                }
                sortedObjects.Clear();
            }
        }
    }
}
