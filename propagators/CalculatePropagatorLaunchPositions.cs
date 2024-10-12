// Once the walls are drawn, the play area (2d array) is invisibly filled with a flood fill algorithm.
// This way we get safe starting points for the shapes, which are then flood filled visibly.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

// Flood fills a temporary, invisble copy of the terrain to see where each propagator should start from. 
public class CalculatePropagatorLaunchPositions : MonoBehaviour
{
    private GameManager gm;
    
    private static short[,] terrainGrid;
    private short[,] terrainCopy;
    private int WorldSizeX, WorldSizeZ;

    public List<Vector2Int> Run()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        gm = GetComponent<GameManager>();
        WorldSizeX = gm.WorldSizeX;
        WorldSizeZ = gm.WorldSizeZ;
        terrainGrid = gm.terrainGrid;

        terrainCopy = Copy2DArray(terrainGrid);
        List<Vector2Int> startingPositions = FillAllEmptySpaces();

        //Save a copy of the terrain to disk for dev purposes
        //SaveArrayToFile(terrainCopy, Application.dataPath + "/arrayData.bin");

        terrainCopy = null;
        System.GC.Collect();

        stopwatch.Stop();
        UnityEngine.Debug.Log($"Calculated starting positions for propagators in {stopwatch.ElapsedMilliseconds} ms.");
        return startingPositions;
    }

    private static short[,] Copy2DArray(short[,] original)
    {
        int rows = original.GetLength(0);
        int cols = original.GetLength(1);
        short[,] copy = new short[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            Array.Copy(original, i * cols, copy, i * cols, cols);
        }
        return copy;
    }

    List<Vector2Int> FillAllEmptySpaces()
    {
        List<Vector2Int> startingPositions = new List<Vector2Int>();
        int fillValue = 2;

        for (int z = 0; z < WorldSizeZ; z++)
        {
            for (int x = 0; x < WorldSizeX; x++)
            {
                if (terrainCopy[x, z] == 0)
                {
                    //UnityEngine.Debug.Log($"Shape number {fillValue} starting from position {x}  {z}  ");
                    startingPositions.Add(new Vector2Int(x, z));
                    FloodFill(x, z, fillValue);
                    fillValue++;
                }
            }
        }
        return startingPositions;
    }

    void FloodFill(int startX, int startZ, int fillValue)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startZ));

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int x = current.x;
            int z = current.y;

            // Check bounds
            if (x < 0 || x >= WorldSizeX || z < 0 || z >= WorldSizeZ)
                continue;

            // If it's already filled or it's a wall, skip it
            if (terrainCopy[x, z] != 0)
                continue;

            // Fill the current cell
            terrainCopy[x, z] = (short)fillValue;
            queue.Enqueue(new Vector2Int(x + 1, z));
            queue.Enqueue(new Vector2Int(x - 1, z));
            queue.Enqueue(new Vector2Int(x, z + 1));
            queue.Enqueue(new Vector2Int(x, z - 1));
        }
    }

    void SaveArrayToFile(short[,] array, string filePath)
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(filePath);

        int rows = array.GetLength(0);
        int cols = array.GetLength(1);

        bf.Serialize(file, rows);
        bf.Serialize(file, cols);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                bf.Serialize(file, array[i, j]);
            }
        }

        file.Close();
    }
}
