using System.Collections.Generic;
using UnityEngine;

// Objects are categorised into chunks that span 100x100 units on the xz plane.
// For example an object at coordinates (523,0,123) is in chunk (5,1)
public class WallChunkManager : MonoBehaviour
{
    public static WallChunkManager Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    private const int chunkSize = 95;

    private Dictionary<Vector2Int, List<GameObject>> chunkDict = new();

    void Start()
    {
        InvokeRepeating("CheckForMeshesToCombine", 1.0f, 0.1f);
    }

    /// Add a game object to its corresponding chunk
    public void AddGameObjectToChunk(GameObject obj)
    {
        Vector2Int chunkIndex = GetChunkIndex(obj);
        if (chunkDict.TryGetValue(chunkIndex, out List<GameObject> chunk))
        {
            chunk.Add(obj);
        }
        else
        {
            chunkDict[chunkIndex] = new List<GameObject> { obj };
        }
    }

    private Vector2Int GetChunkIndex(GameObject obj)
    {
        Vector3 position = obj.transform.position;
        int xIndex = Mathf.FloorToInt(position.x / chunkSize);
        int zIndex = Mathf.FloorToInt(position.z / chunkSize);
        return new Vector2Int(xIndex, zIndex);
    }

    // Make a combined mesh if all objects in a chunk have their fade-ins complete (fadeInProgress values are all false)
    private void CheckForMeshesToCombine()
    {
        List<Vector2Int> keysToRemove = new List<Vector2Int>();
        foreach (var entry in chunkDict)
        {
            Vector2Int position = entry.Key;
            List<GameObject> objlist = entry.Value;

            bool fadeInProgress = false;
                for (int i = objlist.Count - 1; i >= 0; i--)    //  foreach (GameObject wall in objlist) but in reverse order, because later indexes are much more likely to break the loop
                {
                    GameObject wall = objlist[i];
                    if (wall.GetComponent<WallColorFade>().FadeInProgress)
                    {
                        fadeInProgress = true;
                        break;
                    }
                }
            if (fadeInProgress == false)
            {
                if (objlist.Count > 0)
                {
                    Vector3 worldCoordinates = new Vector3(position.x * chunkSize, 8, position.y * chunkSize);
                    WallMeshCombiner.Instance.CombineMeshes(objlist, worldCoordinates);   // also deactivates the individual objects and clears the list
                    keysToRemove.Add(entry.Key);
                }
            }
        }
        foreach (var key in keysToRemove)
        {
            chunkDict.Remove(key);
        }
    }
}