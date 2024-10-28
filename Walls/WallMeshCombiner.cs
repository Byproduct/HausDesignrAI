using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallMeshCombiner : MonoBehaviour
{
    public static WallMeshCombiner Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    public GameObject WallParentObject;
    public Material WallMaterial;

    /// Combine meshes that are inside the same chunk
    public void CombineMeshes(List<GameObject> objects, Vector3 worldPosition)
    {
        //Before combining the meshes, combine adjacent blocks into single larger cuboids.
        objects = CombineAdjacentBlocks(objects);

        if (objects.Count > 0)   // fast-forwarding can cause an empty list
        {
            // Create a new GameObject to hold the combined mesh
            int x = (int)(worldPosition.x);
            int z = (int)(worldPosition.z);
            GameObject combinedObject = new GameObject($"Combomesh - {objects.Count} objects - chunk {x}, 0, {z}");
            combinedObject.name = $"Combomesh - {objects.Count} objects - chunk {x}, 0, {z};";
            combinedObject.transform.SetParent(WallParentObject.transform);
            combinedObject.transform.position = worldPosition;
            AddComponentsToGameObject(combinedObject);

            CombineInstance[] combine = new CombineInstance[objects.Count];
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].GetComponent<MeshFilter>() != null && objects[i].GetComponent<MeshRenderer>() != null)
                {
                    combine[i].mesh = objects[i].GetComponent<MeshFilter>().mesh;
                    // Each object's position corresponds to the combined object's position ("worldPosition")
                    combine[i].transform = Matrix4x4.Translate(-worldPosition) * objects[i].transform.localToWorldMatrix;
                }
            }
            combinedObject.GetComponent<MeshFilter>().mesh = new Mesh();
            combinedObject.GetComponent<MeshFilter>().mesh.CombineMeshes(combine, true, true);

            // Deactivate the original objects
            // It'd be better to release these to the pool, but didn't have the time to investigate the resulting bug, so just deactivating instead for now.
            foreach (GameObject obj in objects)
            {
                //WallManager.Instance.WallPartPool.Release(obj);
                obj.SetActive(false);
                obj.isStatic = false;
            }
            objects.Clear();
        }
    }

    /// Check which objects are adjacent along the z-axis. These rows of individual objects can be replaced with one cuboid object.
    private List<GameObject> CombineAdjacentBlocks(List<GameObject> objects)
    {
        // 100/200 not exact number of objects, just initial values to reduce list resizing.
        Dictionary<Vector3Int, int> positionToGroup = new Dictionary<Vector3Int, int>(200);
        Dictionary<int, List<int>> groupToIndices = new Dictionary<int, List<int>>(100);

        int groupCounter = 0;

        for (int i = 0; i < objects.Count; i++)
        {
            Vector3Int pos = new Vector3Int((int)objects[i].transform.position.x, 0, (int)objects[i].transform.position.z);
            Vector3Int previousPos = new Vector3Int(pos.x, pos.y, pos.z - 1);

            //If an adjacent object is found, add the current item to the same group
            if (positionToGroup.TryGetValue(previousPos, out int group))
            {
                positionToGroup[pos] = group;
                groupToIndices[group].Add(i);
            }
            //If an adjacent object is not found, increment the group number by one and create a new group with it
            else
            {
                positionToGroup[pos] = groupCounter;
                groupToIndices[groupCounter] = new List<int> { i };
                groupCounter++;
            }
        }

        // Processing and creating new objects
        foreach (var kvp in groupToIndices)
        {
            List<int> indices = kvp.Value;
            if (indices.Count < 2) continue;  // Do nothing with lists of size 1 (they don't have any adjacent blocks)

            // Get a new object from pool
            var wallPart = WallManager.Instance.WallPartPool.Get();
            if (wallPart != null)
            {
                // Align the wall part. X-coordinate is the same for all objects in the group, y-coordinate is irrelevant, and z-coordinate is halfway into the object (object size divided by 2 minus half unit)
                Vector3 firstPos = objects[indices[0]].transform.position;
                wallPart.transform.position = new Vector3(firstPos.x, firstPos.y, firstPos.z + (float)indices.Count / 2 - 0.5f);
                wallPart.transform.localScale = new Vector3(1, 16, indices.Count);
                wallPart.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                wallPart.isStatic = true;
                wallPart.SetActive(true);
                objects.Add(wallPart);
            }
            for (int i = 0; i < indices.Count; i++)
            {
                var obj = objects[indices[i]];
                WallManager.Instance.WallPartPool.Release(obj);  // pool will handle resetting the object and making it inactive
            }
        }
        // Rebuild the objects list to only include active objects
        objects = objects.Where(obj => obj.activeSelf).ToList();
        return objects;
    }

    // When all walls are drawn, wait for 3 seconds for color changes to finish, then combine all walls into one single object.
    public void WaitAndCombineAllWalls()
    {
        StartCoroutine(WaitAndExecute());
    }

    private IEnumerator WaitAndExecute()
    {
        yield return new WaitForSeconds(3f);
        CombineAllWalls();
    }

    /// At the end of the wall phase, combine all wall chunks into a single massive object with one mesh
    public void CombineAllWalls()
    {
        List<GameObject> objects = new List<GameObject>();
        foreach (Transform child in WallParentObject.transform)
        {
            objects.Add(child.gameObject);
        }

        // Mesh combining similar to the chunk mesh combining above.
        if (objects.Count > 0)
        {
            Vector3 worldPosition = new Vector3(0, 0, 0);
            GameObject combinedObject = new GameObject($"All walls combined");
            combinedObject.transform.SetParent(WallParentObject.transform);
            combinedObject.transform.position = worldPosition;
            AddComponentsToGameObject(combinedObject);

            CombineInstance[] combine = new CombineInstance[objects.Count];
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].GetComponent<MeshFilter>() != null && objects[i].GetComponent<MeshRenderer>() != null)
                {
                    combine[i].mesh = objects[i].GetComponent<MeshFilter>().mesh;
                    combine[i].transform = Matrix4x4.Translate(-worldPosition) * objects[i].transform.localToWorldMatrix;
                }
            }
            Mesh mesh = combinedObject.GetComponent<MeshFilter>().mesh;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combine, true, true);

            foreach (GameObject obj in objects)
            {
                obj.SetActive(false); // cpu busiest now as propagation begins, destroy these later
            }
        }
    }

    // GameObject settings/components that are identical between the two wall mesh combining operations
    public void AddComponentsToGameObject(GameObject go)
    {
        go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.sharedMaterial = WallMaterial;
        go.isStatic = true;
    }
}