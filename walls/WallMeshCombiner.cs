using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class WallMeshCombiner : MonoBehaviour
{
    private WallManager wallManager;
    public GameObject WallParentObject;
    public Material WallMaterial;

    void Start()
    {
        wallManager = GetComponent<WallManager>();
    }

    // Combine meshes and place the new mesh where the previous individual meshes were (coordinates given as a parameter for this purpose).
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
            //GameObject combinedObject = wallManager.WallPartPool.Get();
            combinedObject.name = $"Combomesh - {objects.Count} objects - chunk {x}, 0, {z};";
            //        Debug.Log($"Combined mesh: {objects.Count} objects - chunk {x}, 0, {z}");
            combinedObject.AddComponent<MeshFilter>();
            var mr = combinedObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            //mr.sharedMaterial = objects[0].GetComponent<MeshRenderer>().sharedMaterial;
            mr.sharedMaterial = WallMaterial;
            combinedObject.transform.SetParent(WallParentObject.transform);

            // Set the position of the combined mesh and make it static
            combinedObject.transform.position = worldPosition;
            combinedObject.isStatic = true;


            // Prepare the combine instances array
            CombineInstance[] combine = new CombineInstance[objects.Count];
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].GetComponent<MeshFilter>() != null && objects[i].GetComponent<MeshRenderer>() != null)
                {
                    combine[i].mesh = objects[i].GetComponent<MeshFilter>().mesh;
                    // Adjust the transformation matrix by subtracting the new position
                    combine[i].transform = Matrix4x4.Translate(-worldPosition) * objects[i].transform.localToWorldMatrix;
                }
            }

            // Create and assign the new mesh
            combinedObject.GetComponent<MeshFilter>().mesh = new Mesh();
            combinedObject.GetComponent<MeshFilter>().mesh.CombineMeshes(combine, true, true);

            // Deactivate the original objects
            foreach (GameObject obj in objects)
            {
                //wallManager.WallPartPool.Release(obj);
                obj.SetActive(false);
                obj.isStatic = false;
            }
            objects.Clear();
        }
    }

    // Check which objects are adjacent along the z-axis. These rows of individual objects can be replaced with one cuboid object.
    private List<GameObject> CombineAdjacentBlocks(List<GameObject> objects)
    {
        // initial capacity of 100 is just a guesstimate to reduce resizing - not an exact number of objects/groups.
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
            // Do nothing with objects that are of size 1 (=don't have any adjacent blocks)
            if (indices.Count < 2) continue;

            // Get an inactive wall GameObject from object pool
            var wallPart = wallManager.WallPartPool.Get();

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
                wallManager.WallPartPool.Release(obj);
                //obj.SetActive(false);
                //obj.isStatic = false;
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
        // Wait for 3 seconds
        yield return new WaitForSeconds(3f);

        // Proceed with the rest of the function after 3 seconds
        CombineAllWalls();
    }

    public void CombineAllWalls()
    {
        List<GameObject> objects = new List<GameObject>();
        foreach (Transform child in WallParentObject.transform)
        {
            objects.Add(child.gameObject);
        }

        if (objects.Count > 0)   // fast-forwarding can cause an empty list
        {
            Vector3 worldPosition = new Vector3(0, 0, 0);
            GameObject combinedObject = new GameObject($"All walls combined");
            combinedObject.AddComponent<MeshFilter>();
            var mr = combinedObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.sharedMaterial = objects[0].GetComponent<MeshRenderer>().sharedMaterial;
            combinedObject.transform.SetParent(WallParentObject.transform);
            combinedObject.transform.position = worldPosition;
            combinedObject.isStatic = true;

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
                obj.SetActive(false); // cpu busiest now, destroy these later
            }
        }
    }
}