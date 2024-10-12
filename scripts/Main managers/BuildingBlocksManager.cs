using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BuildingBlock
{
    public GameObject gameObject;
    public short groupId;
    public int height;
    public Color color;
    public BuildingBlock(GameObject obj, short groupId, int height)
    {
        this.gameObject = obj;
        this.groupId = groupId;
        this.height = height;
        this.color = PropagatorColorManager.Instance.GetFinalColor(groupId);
    }
}

public class BuildingBlocksManager : MonoBehaviour
{
    public static BuildingBlocksManager Instance { get; private set; }

    public Dictionary<short, BuildingBlock> BuildingBlocks = new();              // gets populated by PropagatorManager as blocks complete

    public TextMeshProUGUI Heading;

    public GameObject EnergyTextLine;

    private GameManager gameManager;

    private bool isFading = false;

    public List<BuildingBlock> largestBlocks = new();          // "Cutest blocks"
    public List<BuildingBlock> discardedBlocks = new();        // The blocks that remain black and disappear

    private Stopwatch stopwatch;
    public Material SkyboxMaterial;
    private AudioLowPassFilter audioFilter;


    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameObject musicPlayer = GameObject.FindWithTag("MusicPlayer");
        audioFilter = musicPlayer.GetComponent<AudioLowPassFilter>();
        audioFilter.enabled = false;
    }
    public void ActivateBuildingBlocks()
    {
        gameManager = GameManager.Instance;

        if (!isFading)
        {
            Debug.Log("Starting initial fade");
            StartCoroutine(InitiallyFadeObjects());
        }
    }

    private IEnumerator InitiallyFadeObjects()
    {
        Heading.text = " ";

        // higher fps section, good spot to get rid of some of the massive parent objects that are not needed anymore
        gameManager.terrainGrid = null;
        ComponentDestroyer.Instance.DestroyPhase1();


        StartCoroutine(FindLargestBlocks());
        isFading = true;
        float fadingRate = 0.05f;
        float elapsedTime = 0f;
        Color targetColor = new Color(0.1f, 0.1f, 0.1f);

        float initialFadeDuration = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 1.5f,
            Configuration.SpeedType.Fast => 0.4f,
            Configuration.SpeedType.Dev => 0.4f,
            _ => 2f, // default
        };
        while (elapsedTime < initialFadeDuration)
        {
            float t = elapsedTime / initialFadeDuration;

            foreach (var item in BuildingBlocks)
            {
                Color currentColor = Color.Lerp(item.Value.color, targetColor, t);
                item.Value.gameObject.GetComponent<Renderer>().material.color = currentColor;
            }
            yield return new WaitForSeconds(fadingRate);
            elapsedTime += fadingRate;
        }
        foreach (var item in BuildingBlocks)
        {
            item.Value.gameObject.GetComponent<Renderer>().material.color = targetColor;
        }
        isFading = false;
        Debug.Log("Initial fade complete");
        StartCoroutine(LightUpCutestBlocks());
        RenderSettings.skybox = SkyboxMaterial;
    }

    // Find 100 large building blocks in the list, by comparing the number of vertices. 
    // In the demo obfuscated by randomising the order of results and calling it cuteness. :>

    public IEnumerator FindLargestBlocks()
    {
        List<KeyValuePair<BuildingBlock, int>> blockSizes = new List<KeyValuePair<BuildingBlock, int>>();

        var buildingBlocksValues = BuildingBlocks.Values.ToList();
        int totalBlocks = buildingBlocksValues.Count;

        int i = 0;
        while (i < totalBlocks)
        {
            int processedThisFrame = 0;

            // Process up to 10 blocks per frame
            while (processedThisFrame < 10 && i < totalBlocks)
            {
                BuildingBlock block = buildingBlocksValues[i];
                MeshFilter meshFilter = block.gameObject.GetComponent<MeshFilter>();

                int vertexCount = 0;
                if (meshFilter != null && meshFilter.mesh != null)
                {
                    vertexCount = meshFilter.mesh.vertexCount;
                }

                blockSizes.Add(new KeyValuePair<BuildingBlock, int>(block, vertexCount));
                i++;
                processedThisFrame++;
            }

            // Yield control back to Unity
            yield return new WaitForFixedUpdate();
        }

        yield return null;

        // Sort blocks by size
        blockSizes.Sort((a, b) => b.Value.CompareTo(a.Value));
        yield return null;

        // Skip 2 largest blocks, then take 100 remaining largest blocks.
        largestBlocks = blockSizes.Skip(2).Take(100).Select(pair => pair.Key).ToList();
        yield return null;

        // Randomize the list
        System.Random rng = new System.Random();
        int n = largestBlocks.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            BuildingBlock value = largestBlocks[k];
            largestBlocks[k] = largestBlocks[n];
            largestBlocks[n] = value;
        }

        yield return null;

        foreach (var block in BuildingBlocks.Values)
        {
            if (!largestBlocks.Contains(block))
            {
                discardedBlocks.Add(block);
            }
        }
        Debug.Log("Found and randomized the 100 largest BuildingBlocks.");
    }

    public IEnumerator LightUpCutestBlocks()
    {
        Heading.text = "choosing 100 cutest blocks";
        Destroy(GetComponent<EnergyCounter>());
        Destroy(EnergyTextLine);
        int blockCount = largestBlocks.Count;

        float initialDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 0.4f,  // 0.6f for better music sync later
            Configuration.SpeedType.Fast => 0.05f,
            Configuration.SpeedType.Dev => 0.03f,
            _ => 0.8f, // default
        };

        float minimumDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 0.04f,  // 0.05f for better music sync later
            Configuration.SpeedType.Fast => 0.03f,
            Configuration.SpeedType.Dev => 0.03f,
            _ => 0.05f, // default
        };

        for (int i = 0; i < blockCount; i++)
        {
            if (audioFilter.enabled)
            {
                audioFilter.cutoffFrequency += 1000f;
                if (audioFilter.cutoffFrequency > 20000)
                {
                    audioFilter.enabled = false;
                }
            }
            BuildingBlock bb = largestBlocks[i];
            bb.gameObject.GetComponent<MeshRenderer>().material.color = bb.color;
            yield return new WaitForSeconds(initialDelay);
            initialDelay *= 0.90f;
            if (initialDelay < minimumDelay)
            {
                initialDelay = minimumDelay;
            }
        }
        Invoke("DisplayBlocksPhase", 1.0f);
    }

    public void DisplayBlocksPhase()
    {
        CameraManager.Instance.BlockChoosingCamera();
        DisplayBlocksManager.Instance.Initiate();
    }
}