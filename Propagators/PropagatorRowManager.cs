// Class to keep track of individual rows of propagators inside blocks.
// Once a row is filled completely, cooldown is started to account for color fade
// After cooldown, the objects in the row are merged into one simple cuboid

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// PropagatorRowStatus tracks which rows are completed, on cooldown, or ready to merge 
public class PropagatorRowStatus
{
    public bool LeftSideComplete;
    public bool RightSizeComplete;
    public bool CooldownStarted;
    public short GroupId;
    public int RowNum;

    public PropagatorRowStatus(short groupId, int rowNum)
    {
        LeftSideComplete = false;
        RightSizeComplete = false;
        CooldownStarted = false;
        GroupId = groupId;
        RowNum = rowNum;
    }

    public void CompletedLeft()
    {
        LeftSideComplete = true;
        checkComplete();
    }

    public void CompletedRight()
    {
        RightSizeComplete = true;
        checkComplete();
    }

    private void checkComplete()
    {
        if (LeftSideComplete && RightSizeComplete && CooldownStarted == false)
        {
            CooldownStarted = true;
            PropagatorRowManager.Instance.AddCooldown(GroupId, RowNum);
        }
    }
}

public class PropagatorRowManager : MonoBehaviour
{
    public static PropagatorRowManager Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    private const float cooldownUpdateInterval = 0.05f;      // Update rate of all cooldowns (not needed every frame, but too sparse checking can cause several mesh combining operations in one frame)
    private const float meshCombineInterval = 0.5f;          // How often to combine multiple completed rows into chunks
    private const int meshCombineThreshold = 25;             // How many complete rows required for the above-mentioned combining

    // (groupId, row) - Lists of propagator objects within single rows. Kept track of for mesh combining.
    public Dictionary<(short, int), List<GameObject>> PropagatorObjectsInRow = new();

    // (groupId, row) - PropagatorRowStatus tracks which rows are completed, on cooldown or ready to merge.
    public Dictionary<(short, int), PropagatorRowStatus> PropagatorRowStatuses = new();

    // Sorted dictionary of cooldown (groupId, rowNumber).
    public SortedDictionary<float, (short, int)> PropagatorRowCooldowns = new();

    // Dictionary to keep track of already combined rows - if there are many such rows, combine their meshes too.
    public Dictionary<short, HashSet<GameObject>> CombinedRows = new();

    public GameObject CombinedPropagatorParentObject;  // Object only for editor scene hierarchy


    private void Start()
    {
        PropagatorObjectsInRow = new Dictionary<(short, int), List<GameObject>>();     // (groupId, rowNum)
        PropagatorRowStatuses = new Dictionary<(short, int), PropagatorRowStatus>();
        PropagatorRowCooldowns = new SortedDictionary<float, (short, int)>();
        CombinedRows = new Dictionary<short, HashSet<GameObject>>();
        InvokeRepeating("UpdateCooldowns", 0.1f, cooldownUpdateInterval);
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

    /// Cooldowns for propagator rows are actually cooldown end times (current time + 1 second) as keys in a sorted dictionary.
    /// That way only the first cooldown needs to be checked against current time, as opposed to maintaining many simultaneously running cooldowns.
    public void AddCooldown(short groupId, int rowNum)
    {
        float key = Time.time + 1.0f;
        while (PropagatorRowCooldowns.ContainsKey(key))
        {
            key += 0.001f;
        }
        PropagatorRowCooldowns.Add(key, (groupId, rowNum));
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

    /// Return list of objects in a row, or create a new list if none are found.
    public List<GameObject> GetObjectsInRow(short groupId, int rowNum)
    {
        var key = (groupId, rowNum);
        if (PropagatorObjectsInRow.TryGetValue(key, out var objects))
        {
            return objects;
        }
        return new List<GameObject>();
    }

    private void CombineRowObjects((short, int) pair)
    {
        short groupId = pair.Item1;
        int rowNum = pair.Item2;

        List<GameObject> rowItems = GetObjectsInRow(groupId, rowNum);
        if (rowItems.Count > 0)
        {
            Dictionary<short, HashSet<GameObject>> PropagatorGameObjects = PropagatorManager.Instance.PropagatorGameObjects;
            Renderer renderer = new Renderer();

            // Get the smallest and largest x-values of the objects in the row
            int minX = Mathf.RoundToInt(rowItems.Min(obj => obj.transform.position.x));
            int maxX = Mathf.RoundToInt(rowItems.Max(obj => obj.transform.position.x));
            int rowLength = maxX - minX + 1;

            GameObject combinedRowObject = PropagatorManager.Instance.PropagatorGameObjectPool.Get();
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
                propBlock.SetColor("_Color", PropagatorColorManager.Instance.GetFinalColor(groupId));
                renderer.SetPropertyBlock(propBlock);

                combinedRowObject.isStatic = true;
                combinedRowObject.SetActive(true);
                if (PropagatorGameObjects.TryGetValue(groupId, out HashSet<GameObject> propagatorHashSet))
                {
                    propagatorHashSet.Add(combinedRowObject);
                }
                if (CombinedRows.TryGetValue(groupId, out propagatorHashSet))
                {
                    propagatorHashSet.Add(combinedRowObject);
                }
                else
                {
                    CombinedRows[groupId] = new HashSet<GameObject>{combinedRowObject};
                }
                foreach (GameObject obj in rowItems)
                {
                    PropagatorManager.Instance.RecyclePropagatorGameObject(obj);    // deactivate and return to pool
                    PropagatorGameObjects[groupId].Remove(obj);
                }
            }
        }
    }

    /// Combine the meshes of a chunk of multiple complete rows. 
    private void CombineRowMeshes()
    {
        foreach (var combinedRow in CombinedRows)
        {
            if (combinedRow.Value.Count > meshCombineThreshold)
            {
                // Hashset converted into list and sorted based on z-coordinate
                List<GameObject> sortedObjects = new List<GameObject>(combinedRow.Value);
                sortedObjects.Sort((gameObject1, gameObject2) => gameObject1.transform.position.z.CompareTo(gameObject2.transform.position.z));

                Vector3 worldPosition = sortedObjects[0].transform.position;
                GameObject combinedObject = PropagatorManager.Instance.PropagatorGameObjectPool.Get();
                var mr = combinedObject.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.sharedMaterial = sortedObjects[0].GetComponent<MeshRenderer>().sharedMaterial;
                combinedObject.transform.SetParent(CombinedPropagatorParentObject.transform);
                combinedObject.transform.position = worldPosition;
                combinedObject.transform.localScale = Vector3.one;

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

                // Use Switch to index format if total verts exceed 65535
                combinedMesh.indexFormat = totalVertexCount > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

                combinedMesh.CombineMeshes(combine, true, true);
                combinedObject.GetComponent<MeshFilter>().mesh = combinedMesh;

                //Set color in a silly but faster way
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                Renderer r = combinedObject.GetComponent<Renderer>();
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color", PropagatorColorManager.Instance.GetFinalColor(combinedRow.Key));
                r.SetPropertyBlock(propBlock);

                combinedObject.isStatic = true;
                combinedObject.SetActive(true);
                if (PropagatorManager.Instance.PropagatorGameObjects.TryGetValue(combinedRow.Key, out HashSet<GameObject> PropagatorObjects))
                {
                    PropagatorObjects.Add(combinedObject);
                }
                else
                {
                    PropagatorManager.Instance.PropagatorGameObjects[combinedRow.Key] = new HashSet<GameObject> { combinedObject };
                }

                // Deactivate and return individual objects to pool
                foreach (GameObject obj in sortedObjects.ToList())
                {
                    PropagatorManager.Instance.RecyclePropagatorGameObject(obj);
                    CombinedRows[combinedRow.Key].Remove(obj);
                    PropagatorManager.Instance.PropagatorGameObjects[combinedRow.Key].Remove(obj);
                }
                sortedObjects.Clear();
            }
        }
    }
}