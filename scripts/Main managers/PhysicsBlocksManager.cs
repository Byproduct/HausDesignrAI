using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using System;

public class Furniture
{
    public int objectId; // number of object in the FurniturePrefabs list
    public GameObject gameObject;

    public Furniture(int id, GameObject obj)
    {
        this.objectId = id;
        this.gameObject = obj;
    }
}

public class PhysicsBlocksManager : MonoBehaviour
{
    public static PhysicsBlocksManager Instance { get; private set; }

    public GameObject PhysicsBlocksParent;
    public GameObject BuildingBlocksParent;
    public GameObject TreesParent;
    public GameObject TreePrefab;
    public GameObject Doghouse;
    public GameObject DoghousePrefab;
    public string Dogname;
    public List<GameObject> PhysicsBlocks = new();
    public List<GameObject> FinalPhysicsBlocks = new();
    public List<GameObject> Trees = new();
    public TextMeshProUGUI Heading;
    private Vector3 centerPoint;
    public GameObject BackWall;
    public Material DissolvingMaterial;
    public GameObject Timestamp;
    public List<Furniture> HouseFurniture;
    public GameObject FurnitureParent;
    public GameObject OriginalGround;
    public GameObject GroundWithoutDriveway;
    public GameObject GroundWithDriveway;
    public GameObject RoadSigns;
    public int RoadSignNumber;
    public AudioSource Honk;
    public AudioSource Bark;
    private AudioSource musicPlayer;
    public AudioSource outroMusicPlayer;

    private int currentBlock = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void Initiate()
    {   
        // Get the gameobjects from display blocks (other existing properties are no longer needed), and add them to new list
        // Rename, add mesh colliders etc.
        int i = 1;
        foreach (DisplayBlock db in DisplayBlocksManager.Instance.DisplayBlocks)
        {
            GameObject go = db.gameObject;
            go.name = $"Physics block #{i}";
            go.transform.parent = PhysicsBlocksParent.transform;
            i++;
            PhysicsBlocks.Add(go);
        }
        Destroy(GetComponent<DisplayBlocksManager>());
        Destroy(GetComponent<BuildingBlocksManager>());
        Destroy(BuildingBlocksParent);
        StartCoroutine(BuildHouse());
        StartCoroutine(FadeMusic());
    }

    IEnumerator FadeMusic()
    {
        GameObject musicPlayer = GameObject.FindWithTag("MusicPlayer");
        AudioSource asrc = musicPlayer.GetComponent<AudioSource>();
        asrc.volume = 100f;
        float elapsedTime = 0f;
        while (elapsedTime < 3f)
        {
            asrc.volume = 1 - (elapsedTime / 3f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        asrc.enabled = false;
    }

    public void SpawnPhysicsBlock(string mode = "leftwall")
    {
        if (currentBlock < PhysicsBlocks.Count)
        {
            GameObject go = PhysicsBlocks[currentBlock];
            go.SetActive(true);
            Rigidbody rb = go.AddComponent<Rigidbody>();
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.convex = true;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            go.AddComponent<CollisionSounds>();

            if (mode == "floor")
            {
                go.transform.position = new Vector3(UnityEngine.Random.Range(3000f, 3500f), UnityEngine.Random.Range(1500f, 1800f), UnityEngine.Random.Range(-500f, 0f));
                rb.velocity = new Vector3(UnityEngine.Random.Range(-3000f, -3500f), -2000f, 0f);
            }
            if (mode == "leftwall")
            {
                go.transform.position = new Vector3(UnityEngine.Random.Range(3000f, 3500f), UnityEngine.Random.Range(1500f, 1800f), UnityEngine.Random.Range(-500f, 0f));
                rb.velocity = new Vector3(UnityEngine.Random.Range(-3000f, -3500f), -2000f, 0f);
            }
            else if (mode == "rightwall")
            {
                go.transform.position = new Vector3(UnityEngine.Random.Range(-2000f, -2500f), UnityEngine.Random.Range(1500f, 1800f), UnityEngine.Random.Range(-500f, 0f));
                rb.velocity = new Vector3(UnityEngine.Random.Range(3000f, 3500f), -2000f, 0f);
            }
            else if (mode == "roof")
            {
                go.transform.position = new Vector3(UnityEngine.Random.Range(-250f, 1250f), UnityEngine.Random.Range(1500f, 1800f), UnityEngine.Random.Range(-500f, 0f));
                rb.velocity = new Vector3(UnityEngine.Random.Range(-10f, 10f), -2000f, 0f);
            }
            currentBlock++;
        }
    }

    IEnumerator BuildHouse()
    {
        float longBuildingDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 4.5f,
            Configuration.SpeedType.Fast => 3f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 4.5f,
        };
        float mediumBuildingDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 3f,
            Configuration.SpeedType.Fast => 2f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 3f,
        };
        float shortBuildingDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 2f,
            Configuration.SpeedType.Fast => 1.5f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 2f,
        };


        yield return new WaitForSeconds(shortBuildingDelay);
        Heading.text = "placing floors";
        yield return new WaitForSeconds(shortBuildingDelay / 2f);
        for (int numberOfBlocks = 0; numberOfBlocks < 25; numberOfBlocks++)
        {
            SpawnPhysicsBlock("floor");
            yield return new WaitForSeconds(0.02f);
        }
        yield return new WaitForSeconds(longBuildingDelay);
        Heading.text = "placing walls";
        for (int walls = 0; walls < 4; walls++)
        {
            // every other wall from the right
            string mode = "leftwall";
            if (walls % 2 == 1)
            {
                mode = "rightwall";
            }
            for (int numberOfBlocks = 0; numberOfBlocks < 10; numberOfBlocks++)
            {
                SpawnPhysicsBlock(mode);
                yield return new WaitForSeconds(0.02f);
            }
            yield return new WaitForSeconds(0.5f);
        }
        Heading.text = "adding furniture";

        yield return new WaitForSeconds(shortBuildingDelay);

        yield return AddFurniture(40);

        yield return new WaitForSeconds(shortBuildingDelay / 2f);
        Heading.text = "placing roof";
        yield return new WaitForSeconds(shortBuildingDelay);
        for (int numberOfBlocks = 0; numberOfBlocks < 35; numberOfBlocks++)
        {
            bool invert = (numberOfBlocks > 20);
            SpawnPhysicsBlock("roof");
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(3.5f);
        centerPoint = MakeHouseStatic();
        Heading.text = "planting garden";
        yield return AddTrees();
        yield return new WaitForSeconds(1.5f);
        Heading.text = "Adding traffic control";
        yield return AddRoadSign();
        yield return new WaitForSeconds(shortBuildingDelay / 2f);
        DogNameHeading();
        yield return new WaitForSeconds(shortBuildingDelay / 2f);
        yield return ActivateTimestamp();
        yield return AddDoghouse();
    }

    IEnumerator AddFurniture(int n)
    {
        HouseFurniture = new List<Furniture>();
        for (int i = 0; i < n; i++)
        {
            int furnitureId = UnityEngine.Random.Range(0, FurniturePrefabs.Instance.FurniturePrefabsList.Count);
            Vector3 randomFurniturePosition = new Vector3(UnityEngine.Random.Range(0f, 1000f), UnityEngine.Random.Range(1500f, 1800f), UnityEngine.Random.Range(-500f, 0f));
            Vector3 randomFurnitureVelocity = new Vector3(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-1000f, -2000f), UnityEngine.Random.Range(-100f, 100f));
            GameObject go = Instantiate(FurniturePrefabs.Instance.FurniturePrefabsList[furnitureId], randomFurniturePosition, Quaternion.identity, FurnitureParent.transform);
            go.GetComponent<Rigidbody>().velocity = randomFurnitureVelocity;
            go.AddComponent<CollisionSounds>();
            HouseFurniture.Add(new Furniture(furnitureId, go));
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator ActivateTimestamp()
    {
        DateTime now = DateTime.Now;
        string datetimeString = now.ToString("yyyy-MM-dd, HH:mm");
        Timestamp.GetComponent<TextMeshPro>().text = $"new house\n{datetimeString}";
        Timestamp.SetActive(true);
        // Wait a couple of frames for the script in timestamp to clear any colliding objects, then destroy the script 
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Destroy(Timestamp.transform.GetChild(0).gameObject);
    }

    // Make blocks within appropriate range static, destroy other blocks (some can fly far away), and return house center point.
    Vector3 MakeHouseStatic()
    {
        Stopwatch sw = new Stopwatch();

        List<float> xCoords = new();
        List<float> zCoords = new();

        List<GameObject> invalidBlocks = new();
        for (int i = PhysicsBlocks.Count - 1; i >= 0; i--)   // reverse iteration for easier destruction of invalid blocks
        {
            GameObject go = PhysicsBlocks[i];
            if (go != null)
            {
                if (ValidFinalPosition(go))
                {
                    FinalPhysicsBlocks.Add(go);
                    go.isStatic = true;
                    Destroy(go.GetComponent<Rigidbody>());
                    Destroy(go.GetComponent<CollisionSounds>());
                    xCoords.Add(go.transform.position.x);
                    zCoords.Add(go.transform.position.z);
                }
                else
                {
                    GameObject dissolvingClone = Instantiate(go);
                    Destroy(go);
                    dissolvingClone.AddComponent<ObjectDissolver>();
                }
            }
        }
        centerPoint = new Vector3(xCoords.Average(), 0, zCoords.Average());
        UnityEngine.Debug.Log($"Chose valid blocks and calculated center in {sw.ElapsedMilliseconds} ms.");

        foreach (Furniture furniture in HouseFurniture)
        {
            if (furniture.gameObject != null)
            {
                Destroy(furniture.gameObject.GetComponent<Rigidbody>());
                Destroy(furniture.gameObject.GetComponent<CollisionSounds>());
                furniture.gameObject.isStatic = true;
            }
        }
        return centerPoint;
    }

    public bool ValidFinalPosition(GameObject go)
    {
        Vector3 pos = go.transform.position;
        if ((pos.x > -1000) && (pos.x < 2500) && (pos.y > -100) && (pos.z > -1500) && (pos.z < 650))
        {
            return true;
        }
        return false;
    }

    IEnumerator AddTrees()
    {
        Trees.Add(DropTree(1000f));
        yield return new WaitForSeconds(0.25f);
        Trees.Add(DropTree(200f));
        yield return new WaitForSeconds(0.25f);
        Trees.Add(DropTree(800f));
        yield return new WaitForSeconds(0.25f);
        Trees.Add(DropTree(0f));
    }

    GameObject DropTree(float xPosition)
    {
        GameObject tree = Instantiate(TreePrefab, new Vector3(xPosition, 1000, 350f), Quaternion.identity, TreesParent.transform);
        tree.name = $"tree {xPosition}";
        tree.GetComponent<Rigidbody>().velocity = new Vector3(0, UnityEngine.Random.Range(-500f, -800f), 0f);
        return tree;
    }

    public void DogNameHeading()
    {
        TextAsset dognames = Resources.Load<TextAsset>("dognames");
        string[] lines = dognames.text.Split('\n');
        int randomIndex = UnityEngine.Random.Range(0, lines.Length);
        Dogname = lines[randomIndex].Trim();
        Heading.text = $"shade for {Dogname} the dog";
    }

    IEnumerator AddRoadSign()
    {
        RoadSignNumber = UnityEngine.Random.Range(0, HouseLoader.Instance.RoadSigns.Count);
        GameObject newRoadSign = HouseLoader.Instance.RoadSigns[RoadSignNumber];
        Vector3 roadSignPosition = new Vector3(1200f, 1000f, 350f);
        GameObject roadSign = Instantiate(newRoadSign, roadSignPosition, newRoadSign.transform.rotation);
        roadSign.transform.parent = TreesParent.transform;
        roadSign.name = "Road sign for new house";
        float currentTime = 0f;
        float duration = 0.6f;
        float startY = 1000f;
        float endY = 0f;
        bool honked = false;
        while (currentTime < duration)
        {
            // honk a little before landing 
            if ((currentTime > (duration * 0.9f)) && honked == false)
            {
                Honk.Play();
                honked = true;
            }
            currentTime += Time.deltaTime;
            float t = Mathf.Clamp01(currentTime / duration);
            float yCoord = Mathf.Lerp(startY, endY, t);
            roadSign.transform.position = new Vector3(1200f, yCoord, 350f);
            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    IEnumerator AddDoghouse()
    {
        // Instantiated Doghouse is invisible and has a script that destroys any other gameobject on contact with it
        Doghouse = Instantiate(DoghousePrefab, new Vector3(500f, 1f, 350f), DoghousePrefab.transform.rotation);
        Doghouse.GetComponent<MeshRenderer>().enabled = false;

        // Wait a couple of frames for any objects in the way to get destroyed, then drop it in smoothly.
        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame();
        Doghouse.GetComponent<MeshRenderer>().enabled = true;
        Doghouse.transform.position = new Vector3(500f, 1000f, 350f);
        Doghouse.GetComponent<MeshRenderer>().enabled = true;
        Destroy(Doghouse.GetComponent<DestroyerCollider>());
        Destroy(Doghouse.GetComponent<BoxCollider>());
        foreach (Transform child in Doghouse.transform)
        {
            child.gameObject.SetActive(true);
            if (child.name == "dogname")
            {
                child.GetComponent<TextMeshPro>().text = Dogname;
            }
        }
        float currentTime = 0f;
        float duration = 2.5f;
        float startY = 1000f;
        float endY = 0f;
        bool barked = false;
        while (currentTime < duration)
        {
            if ((currentTime > (duration * 0.2f)) && barked == false)
            {
                Bark.Play();
                barked = true;
            }
            currentTime += Time.deltaTime;
            float t = Mathf.Clamp01(currentTime / duration);
            float yCoord = Mathf.Lerp(startY, endY, EaseOut(t));
            Doghouse.transform.position = new Vector3(500f, yCoord, 350f);
            yield return new WaitForEndOfFrame();
        }
        foreach (GameObject tree in Trees)
        {
            if (tree != null)
            {
                Destroy(tree.GetComponent<BoxCollider>());
                Destroy(tree.GetComponent<Rigidbody>());
                Destroy(tree.GetComponent<SinglePlop>());
                Destroy(tree.GetComponent<AudioSource>());
            }
        }
        foreach (GameObject go in PhysicsBlocks)
        {
            if (go != null)
            {
                Destroy(go.GetComponent<MeshCollider>());
            }
        }

        // null-conditional operator would be neater but seems to randomly(?) cause a null reference exception here.
        foreach (Furniture furniture in HouseFurniture)
        {
            if (furniture!=null)
            {
                if (furniture.gameObject != null)
                {
                    if (furniture.gameObject.GetComponent<MeshCollider>() != null)
                    {
                        Destroy(furniture.gameObject.GetComponent<MeshCollider>());
                    }
                }
            }
        }
        yield return new WaitForSeconds(2f);
        StartCoroutine(HouseCompletePhase());
    }

    IEnumerator MeltBackWall(float dissolveTime)
    {
        float wallDissolveTime = dissolveTime;
        Renderer renderer = BackWall.GetComponent<Renderer>();
        Color wallColor = renderer.material.color;
        renderer.material = DissolvingMaterial;
        renderer.material.SetColor("_BaseColor", wallColor);
        float elapsedTime = 0f;
        while (elapsedTime < wallDissolveTime)
        {
            elapsedTime += Time.deltaTime;
            renderer.material.SetFloat("_DissolvingAmount", elapsedTime / wallDissolveTime);
            yield return new WaitForEndOfFrame();
        }
        Destroy(BackWall);
    }

    float EaseOut(float t)
    {
        return 1 - Mathf.Pow(1 - t, 4);
    }

    IEnumerator HouseCompletePhase()
    {
        Heading.text = "house complete";
        HouseSaver.Instance.SaveHouseData();
        HouseSpawner.Instance.SpawnHouses();
        StartCoroutine(UpdateRoads());
        yield return StartCoroutine(MeltBackWall(3f));
        CameraManager.Instance.HouseOrbitCamera(centerPoint);
        RoadSigns.SetActive(true);
        if (Configuration.Speed == Configuration.SpeedType.Normal)
        {
            outroMusicPlayer.Play();
        }
    }

    // Remove original ground and replace with ground that has roads
    IEnumerator UpdateRoads()
    {
        // Road with driveway for the first house
        Instantiate(GroundWithDriveway, new Vector3(0, 0, 0), Quaternion.Euler(90, 0, 0));
        yield return new WaitForEndOfFrame();

        Destroy(OriginalGround);
        yield return new WaitForEndOfFrame();

        // Road without driveways into the infinity before the houses
        for (int i = 1; i < 7; i++)
        {
            Instantiate(GroundWithoutDriveway, new Vector3(i * -2500, 0, 0), Quaternion.Euler(90, 0, 0));
            yield return new WaitForEndOfFrame();
        }
    }
}
