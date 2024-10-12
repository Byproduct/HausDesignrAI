using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

// Objects are in chunks of 100x100 coordinates on the xz plane. So for example an object at coordinates (523,0,123) is in chunk (5,1)
public class WallChunkManager : MonoBehaviour
{
    private GameManager gameManager;
    private WallMeshCombiner meshCombiner;
    private const int CHUNK_SIZE = 95;
    private Dictionary<Vector2Int, List<GameObject>> chunkDict = new Dictionary<Vector2Int, List<GameObject>>();


    void Start()
    {
        gameManager = GetComponent<GameManager>();
        meshCombiner = gameManager.GetComponent<WallMeshCombiner>();
        InvokeRepeating("CheckForMeshesToCombine", 1.0f, 0.1f);
    }

    void Update()
    {
        //// Dump chunk contents in the debug log when C is pressed
        //if (Input.GetKeyDown(KeyCode.C))
        //{
        //    LogChunks();
        //}
    }

    private void PrintLargestChunk()
    {
        var result = GetLargestChunk();
        (Vector2Int largestKey, int largestCount) = result.Value;
        UnityEngine.Debug.Log($"Largest Key: {largestKey}, Count: {largestCount}");
    }

    private void LogChunks()
    {
        string debugString = "";
        foreach (var entry in chunkDict)
        {
            debugString = debugString + "\n" + ($"Chunk {entry.Key}: {entry.Value.Count} objects");
        }
        UnityEngine.Debug.Log(debugString);
    }

    // Method to add a game object to the correct chunk
    public void AddGameObjectToChunk(GameObject obj)
    {
        Vector2Int chunkIndex = GetChunkIndex(obj.transform.position);
        if (chunkDict.TryGetValue(chunkIndex, out List<GameObject> chunk))
        {
            chunk.Add(obj);
        }
        else
        {
            chunkDict[chunkIndex] = new List<GameObject> { obj };
        }
    }

    private Vector2Int GetChunkIndex(Vector3 position)
    {
        int xIndex = Mathf.FloorToInt(position.x / CHUNK_SIZE);
        int zIndex = Mathf.FloorToInt(position.z / CHUNK_SIZE);
        return new Vector2Int(xIndex, zIndex);
    }

    private (Vector2Int, int)? GetLargestChunk()
    {
        if (chunkDict.Count == 0)
            return null;

        Vector2Int largestKey = new Vector2Int();
        int largestCount = 0;

        foreach (var entry in chunkDict)
        {
            int count = entry.Value.Count;
            if (count > largestCount)
            {
                largestCount = count;
                largestKey = entry.Key;
            }
        }
        return (largestKey, largestCount);
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
                    if (wall.GetComponent<WallPart>().fadeInProgress)
                    {
                        fadeInProgress = true;
                        break;
                    }
                }
            if (fadeInProgress == false)
            {
                if (objlist.Count > 0)
                {
                    Vector3 worldCoordinates = new Vector3(position.x * CHUNK_SIZE, 8, position.y * CHUNK_SIZE);
                    meshCombiner.CombineMeshes(objlist, worldCoordinates);   // also deactivates the individual objects and clears the list
                    keysToRemove.Add(entry.Key);
                }
            }
        }
        foreach (var key in keysToRemove)
        {
            chunkDict.Remove(key);
        }
    }

    public void RemoveGameObjectFromChunk(GameObject obj)
    {
        Vector2Int chunkIndex = GetChunkIndex(obj.transform.position);
        if (chunkDict.ContainsKey(chunkIndex))
        {
            chunkDict[chunkIndex].Remove(obj);
        }
    }
}