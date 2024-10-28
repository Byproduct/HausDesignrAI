using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class HouseLoader : MonoBehaviour
{
    public static HouseLoader Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    public GameObject DoghousePrefab;
    public GameObject LoadedHouses;
    public GameObject RoadWithDriveway;
    public GameObject TimestampPrefab;
    public GameObject TreePrefab;
    public List<GameObject> RoadSigns;

    private Stopwatch stopwatch;

    public void LoadHouse(string fileName, int houseNumber)
    {
        stopwatch = Stopwatch.StartNew();

        GameObject houseRoot = new GameObject();
        houseRoot.name = fileName;
        houseRoot.transform.parent = LoadedHouses.transform;
        houseRoot.transform.position = new Vector3((houseNumber) * 2500f, 0, 0);
        ConstructHouseFromPath(fileName, houseRoot, houseNumber);

        // Create a chunk of ground under the house, except for house 0 for which this has already been done
        if (houseNumber > 0)
        {
            GameObject ground = Instantiate(RoadWithDriveway, houseRoot.transform);
            ground.transform.position = houseRoot.transform.position;
            ground.name = $"chunk of ground for house {houseNumber}";
        }
    }

    async void ConstructHouseFromPath(string fileName, GameObject houseRoot, int houseNumber)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError($"File not found: {path}");
            return;
        }

        // load and decompress house data file in a background thread
        string json = await Task.Run(() =>
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            using (GZipStream decompressionStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                decompressionStream.CopyTo(memoryStream);
                byte[] decompressedBytes = memoryStream.ToArray();
                return System.Text.Encoding.UTF8.GetString(decompressedBytes);
            }
        });

        // deserialize house data in a background thread
        HouseData houseData = await Task.Run(() =>
        {
            return JsonUtility.FromJson<HouseData>(json);
        });

        // house number on file, displayed on the ground, is different than houseNumber used internally for coordinates etc
        int fileNumber = int.Parse(fileName.Substring(5, 3));
        ConstructHouseFromDeserializedData(houseData, houseRoot, houseNumber, fileNumber);
    }

    public void ConstructHouseFromDeserializedData(HouseData houseData, GameObject houseRoot, int houseNumber, int fileNumber)
    {
        string timeString = houseData.datetime;
        GameObject Timestamp = Instantiate(TimestampPrefab, houseRoot.transform);
        Timestamp.transform.localPosition = new Vector3(750, 0.1f, 500);
        Timestamp.transform.localRotation = TimestampPrefab.transform.rotation;
        Timestamp.GetComponent<TextMeshPro>().text = $"house #{fileNumber}\n{timeString}";
        Destroy(Timestamp.transform.GetChild(0).gameObject);
        Timestamp.SetActive(true);

        string dogname = houseData.dogName;
        GameObject Doghouse = Instantiate(DoghousePrefab, houseRoot.transform);
        Doghouse.transform.localPosition = new Vector3(500f, 0.1f, 350f);
        Doghouse.transform.localRotation = DoghousePrefab.transform.rotation;
        Doghouse.transform.GetChild(0).GetComponent<TextMeshPro>().text = dogname;
        Destroy(Doghouse.GetComponent<Rigidbody>());
        Destroy(Doghouse.GetComponent<BoxCollider>());
        Destroy(Doghouse.GetComponent<DestroyerCollider>());

        foreach (TreeData treeData in houseData.trees)
        {
            GameObject newTree = Instantiate(TreePrefab, houseRoot.transform);
            newTree.name = "potted tree";
            newTree.transform.localPosition = treeData.position.ToVector3();
            newTree.transform.rotation = treeData.rotation.ToQuaternion();
            Destroy(newTree.GetComponent<Rigidbody>());
            Destroy(newTree.GetComponent<BoxCollider>());
        }

        int roadSign = houseData.roadSign;
        GameObject roadSignPrefab = RoadSigns[roadSign];
        GameObject newRoadSign = Instantiate(roadSignPrefab, houseRoot.transform);
        newRoadSign.transform.localPosition = new Vector3(1200f, 0.1f, 350f);
        newRoadSign.name = $"Road sign #{roadSign}";

        StartCoroutine(ReconstructHouseBlocks(houseData, houseRoot));

        StartCoroutine(ReconstructHouseFurniture(houseData, houseRoot));

        Util.WriteLog($"Initiated building {houseRoot.name} in {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Stop();
    }

    /// Reconstruct house blocks one per frame to avoid stutter
    IEnumerator ReconstructHouseBlocks(HouseData houseData, GameObject houseRoot)
    {
        foreach (BuildingBlockData buildingBlockData in houseData.buildingBlocks)
        {
            GameObject newBlock = new GameObject("House block");
            newBlock.transform.SetParent(houseRoot.transform);
            newBlock.transform.localPosition = buildingBlockData.position.ToVector3();
            newBlock.transform.localRotation = buildingBlockData.rotation.ToQuaternion();

            MeshFilter meshFilter = newBlock.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = newBlock.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh
            {
                vertices = buildingBlockData.vertices.ConvertAll(v => v.ToVector3()).ToArray(),
                triangles = buildingBlockData.triangles.ToArray(),
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            MeshSimplifier.Instance.AssignUVs(mesh);
            meshFilter.mesh = mesh;
            Renderer renderer = newBlock.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"))
            {
                color = buildingBlockData.materialColor.ToColor()
            };
            yield return new WaitForEndOfFrame();
        }
    }

    /// Reconstruct house furniture one per frame to avoid stutter
    IEnumerator ReconstructHouseFurniture(HouseData houseData, GameObject houseRoot)
    {
        foreach (FurnitureData furnitureData in houseData.furniture)
        {
            int furnitureId = furnitureData.furnitureId;
            GameObject newFurniture = Instantiate(FurniturePrefabs.Instance.FurniturePrefabsList[furnitureId], houseRoot.transform);
            newFurniture.name = "furniture";
            newFurniture.transform.localPosition = furnitureData.position.ToVector3();
            newFurniture.transform.localRotation = furnitureData.rotation.ToQuaternion();
            Destroy(newFurniture.GetComponent<Rigidbody>());
            Destroy(newFurniture.GetComponent<MeshCollider>());
            yield return new WaitForEndOfFrame();
        }
    }
}