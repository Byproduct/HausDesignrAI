// Saves the freshly created house into persistentDataPath

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using UnityEngine;

// Serializable versions of Vec3, vec2, quaternion and color are needed so that they can be saved in JSON.
// To-do: the coordinates have excess precision and file size could be smaller with some rounding.
[Serializable]
public class SerializableVector3
{
    public float x, y, z;
    public SerializableVector3() { }
    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable]
public class SerializableVector2
{
    public float x, y;
    public SerializableVector2() { }
    public SerializableVector2(Vector2 v)
    {
        x = v.x;
        y = v.y;
    }
    public Vector2 ToVector2() => new Vector2(x, y);
}

[Serializable]
public class SerializableQuaternion
{
    public float x, y, z, w;
    public SerializableQuaternion() { }
    public SerializableQuaternion(Quaternion q)
    {
        x = q.x;
        y = q.x;
        z = q.z;
        w = q.w;
    }
    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
}

[Serializable]
public class SerializableColor
{
    public float r, g, b, a;
    public SerializableColor() { }
    public SerializableColor(Color c)
    {
        r = Mathf.Round(c.r * 1000f) / 1000f;
        g = Mathf.Round(c.g * 1000f) / 1000f;
        b = Mathf.Round(c.b * 1000f) / 1000f;
        a = Mathf.Round(c.a * 1000f) / 1000f;
    }
    public Color ToColor() => new Color(r, g, b, a);
}

// Serializable classes for blocks, trees and furniture
[Serializable]
public class BuildingBlockData
{
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
    public SerializableColor materialColor;
    public List<SerializableVector3> vertices = new List<SerializableVector3>();
    public List<int> triangles = new List<int>();
}

[Serializable]
public class TreeData
{
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
}

[Serializable]
public class FurnitureData
{
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
    public int furnitureId;
}


// Finally, the actual house class to save
[Serializable]
public class HouseData
{
    public string datetime;
    public string dogName;
    public int roadSign;
    public List<BuildingBlockData> buildingBlocks = new List<BuildingBlockData>();
    public List<TreeData> trees = new List<TreeData>();
    public List<FurnitureData> furniture = new List<FurnitureData>();
}

public class HouseSaver : MonoBehaviour
{
    public static HouseSaver Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    public GameObject houseBlocksParent;
    public string dogName;
    public string fileName;
    public int roadSign;


    public void SaveHouseData()
    {
        Stopwatch sw = Stopwatch.StartNew();

        // Save everything using the serializable classes defined above
        HouseData houseData = new HouseData
        {
            datetime = DateTime.Now.ToString("yyyy-MM-dd, HH:mm"),
            dogName = PhysicsBlocksManager.Instance.Dogname,
            roadSign = PhysicsBlocksManager.Instance.RoadSignNumber,
            buildingBlocks = new List<BuildingBlockData>(),
            trees = new List<TreeData>()
        };

        foreach (Transform block in houseBlocksParent.transform)
        {
            BuildingBlockData buildingBlockData = new BuildingBlockData
            {
                position = new SerializableVector3(block.position),
                rotation = new SerializableQuaternion(block.rotation)
            };

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null && renderer.material.HasProperty("_Color"))
            {
                buildingBlockData.materialColor = new SerializableColor(renderer.material.color);
            }
            else
            {
                buildingBlockData.materialColor = new SerializableColor(Color.white);
            }

            MeshFilter meshFilter = block.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Mesh mesh = meshFilter.sharedMesh;

                foreach (Vector3 v in mesh.vertices)
                {
                    buildingBlockData.vertices.Add(new SerializableVector3(v));
                }
                buildingBlockData.triangles.AddRange(mesh.triangles);
            }
            houseData.buildingBlocks.Add(buildingBlockData);
        }
        foreach (GameObject tree in PhysicsBlocksManager.Instance.Trees)
        {
            if (tree != null)
            {
                TreeData treeData = new TreeData()
                {
                    position = new SerializableVector3(tree.transform.position),
                    rotation = new SerializableQuaternion(tree.transform.rotation)
                };
                houseData.trees.Add(treeData);
            }
        }
        foreach (Furniture furniture in PhysicsBlocksManager.Instance.HouseFurniture)
        {
            if (furniture.GameObject != null)
            {
                FurnitureData furnitureData = new FurnitureData()
                {
                    position = new SerializableVector3(furniture.GameObject.transform.position),
                    rotation = new SerializableQuaternion(furniture.GameObject.transform.rotation),
                    furnitureId = furniture.ObjectId
                };
                houseData.furniture.Add(furnitureData);
            }
        }

        Util.WriteLog($"Collected data to save in {sw.ElapsedMilliseconds} ms");
        sw.Restart();

        // To-do: see if saving to binary without Json is better - human-readability in files not required
        string json = JsonUtility.ToJson(houseData, true);
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
        fileName = GetValidFileName();
        string path = Path.Combine(Application.persistentDataPath, fileName);

        // To-do: some other than the default Gzip library could be more efficient
        // (this one doesn't seem to have compression level parameters, and manual recompression seems to save more space)
        using (FileStream fileStream = new FileStream(path, FileMode.Create))
        using (GZipStream compressionStream = new GZipStream(fileStream, CompressionMode.Compress))
        {
            compressionStream.Write(jsonBytes, 0, jsonBytes.Length);
        }
        Util.WriteLog($"House data saved to {path} in {sw.ElapsedMilliseconds} ms");
        sw.Stop();
    }

    // Get the next house number that doesn't yet exist on disk
    private string GetValidFileName()
    {
        for (int i = 0; i <= 999; i++)
        {
            string fileName = $"house{i:D3}.hus";
            string path = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(path))
            {
                return fileName;
            }
        }      
        // If all filenames are taken, return "house999.hus" and overwrite it
        return "house999.hus";
    }
}