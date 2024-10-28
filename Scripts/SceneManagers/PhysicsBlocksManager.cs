// Physics blocks a.k.a. the house building scene

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;

public class Furniture
{
    public int ObjectId;            // number of object in the FurniturePrefabs list
    public GameObject GameObject;

    public Furniture(int id, GameObject obj)
    {
        ObjectId = id;
        GameObject = obj;
    }
}

public class PhysicsBlocksManager : MonoBehaviour
{
    public static PhysicsBlocksManager Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    public GameObject BuildingBlocksParent;

    public GameObject PhysicsBlocksParent;
    public List<GameObject> PhysicsBlocks = new();
    public List<GameObject> FinalPhysicsBlocks = new();

    public GameObject TreePrefab;
    public GameObject TreesParent;
    public List<GameObject> Trees = new();

    public GameObject Doghouse;
    public GameObject DoghousePrefab;

    public TextMeshProUGUI Heading;
    private Vector3 centerPoint;
    public GameObject BackWall;
    public Material DissolvingMaterial;
    public GameObject Timestamp;
    public GameObject SnakingRoadSign;

    public GameObject FurnitureParent;
    public List<Furniture> HouseFurniture;

    public GameObject OriginalGround;
    public GameObject GroundWithoutDriveway;
    public GameObject GroundWithDriveway;

    public AudioSource Honk;
    public AudioSource Bark;
    private AudioSource musicPlayer;
    public AudioSource outroMusicPlayer;

    public string Dogname;
    public int RoadSignNumber;

    private int currentBlock = 0;


    public void Initiate()
    {
        // Get only the gameobjects from the chosen & displayed blocks, add them to a new list, and add physics. 
        // None of their previous properties are needed at this point. 

        int i = 1;
        foreach (DisplayBlock db in DisplayBlocksManager.Instance.DisplayBlocks)
        {
            GameObject go = db.GameObject;
            go.name = $"Physics block #{i}";
            go.transform.parent = PhysicsBlocksParent.transform;
            i++;
            PhysicsBlocks.Add(go);
        }

        // Cleanup
        Destroy(GetComponent<DisplayBlocksManager>());
        Destroy(GetComponent<BuildingBlocksManager>());
        Destroy(BuildingBlocksParent);

        // Initiate house building and fade out music.
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


    IEnumerator BuildHouse()
    {
        float shortBuildingDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 2f,
            Configuration.SpeedType.Fast => 1.5f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 2f,
        };
        float mediumBuildingDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 3f,
            Configuration.SpeedType.Fast => 2f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 3f,
        };
        float longBuildingDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 4.5f,
            Configuration.SpeedType.Fast => 3f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 4.5f,
        };


        yield return new WaitForSeconds(shortBuildingDelay);
        Heading.text = "placing floors";
        yield return new WaitForSeconds(shortBuildingDelay / 2f);

        yield return SpawnPhysicsBlocks(mode: "floor", numberOfBlocks: 25, delayBetweenBlocks: 0.02f);
        yield return new WaitForSeconds(longBuildingDelay);

        Heading.text = "placing walls";
        yield return SpawnPhysicsBlocks(mode: "leftwall", numberOfBlocks: 10, delayBetweenBlocks: 0.02f);
        yield return new WaitForSeconds(0.5f);
        yield return SpawnPhysicsBlocks(mode: "rightwall", numberOfBlocks: 10, delayBetweenBlocks: 0.02f);
        yield return new WaitForSeconds(0.5f);
        yield return SpawnPhysicsBlocks(mode: "leftwall", numberOfBlocks: 10, delayBetweenBlocks: 0.02f);
        yield return new WaitForSeconds(0.5f);
        yield return SpawnPhysicsBlocks(mode: "rightwall", numberOfBlocks: 10, delayBetweenBlocks: 0.02f);
        yield return new WaitForSeconds(0.5f);

        Heading.text = "adding furniture";
        yield return new WaitForSeconds(shortBuildingDelay);
        yield return AddFurniture(40);

        yield return new WaitForSeconds(shortBuildingDelay / 2f);
        Heading.text = "placing roof";
        yield return new WaitForSeconds(shortBuildingDelay);
        yield return SpawnPhysicsBlocks(mode: "roof", numberOfBlocks: 35, delayBetweenBlocks: 0.05f);

        yield return new WaitForSeconds(3.5f);
        Heading.text = "planting garden";
        yield return AddTrees();
        yield return new WaitForSeconds(1.5f);

        centerPoint = MakeHouseStatic();

        Heading.text = "Adding traffic control";
        yield return AddRoadSign();
        yield return new WaitForSeconds(shortBuildingDelay / 2f);

        DogNameHeading();
        yield return new WaitForSeconds(shortBuildingDelay / 2f);
        yield return ActivateTimestamp();
        yield return AddDoghouse();

        yield return FinalCleanup();

        StartCoroutine(HouseComplete());
    }


    /// Mode parameter determines the type of block being spawned (floor, left/right wall or roof)
    IEnumerator SpawnPhysicsBlocks(string mode, int numberOfBlocks, float delayBetweenBlocks)
    {
        for (int i = 0; i < numberOfBlocks; i++)
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
                yield return new WaitForSeconds(delayBetweenBlocks);
            }
        }
    }

    IEnumerator AddFurniture(int numberOfFurniture)
    {
        HouseFurniture = new List<Furniture>();
        for (int i = 0; i < numberOfFurniture; i++)
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


    /// Make blocks within appropriate range static, destroy any others that might've bounced too far away, then get the house's center point as an average of block positions.
    Vector3 MakeHouseStatic()
    {
        Stopwatch sw = Stopwatch.StartNew();

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
                    // For invalid blocks, create a visually dissolving clone and destroy the original
                    GameObject dissolvingClone = Instantiate(go);
                    Destroy(go);
                    dissolvingClone.AddComponent<ObjectDissolver>();
                }
            }
        }
        centerPoint = new Vector3(xCoords.Average(), 0, zCoords.Average());
        Util.WriteLog($"Chose valid blocks, made the house static, and calculated center in {sw.ElapsedMilliseconds} ms.");
        sw.Stop();

        // Furniture is allowed to be anywhere and isn't part of center calculation.
        foreach (Furniture furniture in HouseFurniture)
        {
            if (furniture.GameObject != null)
            {
                Destroy(furniture.GameObject.GetComponent<Rigidbody>());
                Destroy(furniture.GameObject.GetComponent<CollisionSounds>());
                furniture.GameObject.isStatic = true;
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


    public void DogNameHeading()
    {
        TextAsset dognames = Resources.Load<TextAsset>("dognames");
        string[] lines = dognames.text.Split('\n');
        int randomIndex = UnityEngine.Random.Range(0, lines.Length);
        Dogname = lines[randomIndex].Trim();
        Heading.text = $"shade for {Dogname} the dog";
    }



    /// Activate the timestamp on the ground
    IEnumerator ActivateTimestamp()
    {
        DateTime now = DateTime.Now;
        string datetimeString = now.ToString("yyyy-MM-dd, HH:mm");
        Timestamp.GetComponent<TextMeshPro>().text = $"new house\n{datetimeString}";
        Timestamp.SetActive(true);

        // The timestamp has a child object, which contains a collider script.
        // Wait a couple of frames for it to remove any objects on the way, then destroy the script 
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Destroy(Timestamp.transform.GetChild(0).gameObject);
    }


    IEnumerator AddDoghouse()
    {
        // Instantiated Doghouse is invisible at the final location and clears anything on the way
        Doghouse = Instantiate(DoghousePrefab, new Vector3(500f, 1f, 350f), DoghousePrefab.transform.rotation);
        Doghouse.GetComponent<MeshRenderer>().enabled = false;

        // Wait a couple of frames for any objects in the final location to get destroyed, then lower the doghouse in smoothly.
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
            float yCoord = Mathf.Lerp(startY, endY, Util.EaseOut(t, 4));
            Doghouse.transform.position = new Vector3(500f, yCoord, 350f);
            yield return new WaitForEndOfFrame();
        }
    }


    /// Final cleanup after all of the house is "built"
    IEnumerator FinalCleanup()
    { 
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
                if (furniture.GameObject != null)
                {
                    if (furniture.GameObject.GetComponent<MeshCollider>() != null)
                    {
                        Destroy(furniture.GameObject.GetComponent<MeshCollider>());
                    }
                }
            }
        }
        yield return new WaitForSeconds(2f);
    }


    IEnumerator HouseComplete()
    {
        Heading.text = "house complete";
        HouseSaver.Instance.SaveHouseData();
        HouseSpawner.Instance.SpawnHouses();
        StartCoroutine(UpdateRoads());
        yield return StartCoroutine(MeltBackWall(3f));
        CameraManager.Instance.HouseOrbitCamera(centerPoint);
        SnakingRoadSign.SetActive(true);
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

        // Road without driveways some way into the infinity before the houses  (to-do: add fog?)
        for (int i = 1; i < 7; i++)
        {
            Instantiate(GroundWithoutDriveway, new Vector3(i * -2500, 0, 0), Quaternion.Euler(90, 0, 0));
            yield return new WaitForEndOfFrame();
        }
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
}
