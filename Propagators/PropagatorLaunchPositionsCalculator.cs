// Once walls are drawn, the play area (2d array) is invisibly filled with a flood fill algorithm.
// This way we get the total number of building blocks, a unique number for each block, starting positions for the propagators, 
// and a way to reconstruct a simpler version of each building block later. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

/// Flood fills a temporary, invisble copy of the terrain to see where each propagator should start from. 
public class PropagatorLaunchPositionsCalculator : MonoBehaviour
{
    public static PropagatorLaunchPositionsCalculator Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }
 
    private static short[,] terrainGrid;
    private short[,] terrainCopy;
    private int worldSizeX, worldSizeZ;

    public List<Vector2Int> Run()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        worldSizeX = MainManager.Instance.WorldSizeX;
        worldSizeZ = MainManager.Instance.WorldSizeZ;
        terrainGrid = MainManager.Instance.terrainGrid;

        terrainCopy = Copy2DArray(terrainGrid);
        List<Vector2Int> startingPositions = FillAllEmptySpaces();

        ////Save a copy of the terrain to disk for dev purposes
        //SaveArrayToFile(terrainCopy, Application.dataPath + "/arrayData.bin");

        terrainCopy = null;
        System.GC.Collect();
        stopwatch.Stop();
        Util.WriteLog($"Calculated starting positions for propagators in {stopwatch.ElapsedMilliseconds} ms.");
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

        for (int z = 0; z < worldSizeZ; z++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                if (terrainCopy[x, z] == 0)
                {
                    Util.WriteVerboseLog($"Shape number {fillValue} starting from position {x}  {z}  ");
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

            // Check play area bounds
            if (x < 0 || x >= worldSizeX || z < 0 || z >= worldSizeZ)
                continue;

            // If it's already filled or wall, skip it
            if (terrainCopy[x, z] != 0)
                continue;

            // Fill the current cell
            terrainCopy[x, z] = (short)fillValue;

            // Attempt to fill adjacent cells
            queue.Enqueue(new Vector2Int(x + 1, z));
            queue.Enqueue(new Vector2Int(x - 1, z));
            queue.Enqueue(new Vector2Int(x, z + 1));
            queue.Enqueue(new Vector2Int(x, z - 1));
        }
    }

    /// Save array to file, for dev purposes
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
