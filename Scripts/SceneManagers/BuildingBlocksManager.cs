// Choosing 100 cutest building blocks, and freeing previously used memory in the background

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;

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

    void Awake()
    {
        Instance = this;
    }

    public GameObject EnergyTextLine;
    public Material SkyboxMaterial;

    public Dictionary<short, BuildingBlock> BuildingBlocks = new();     // Gets populated by PropagatorManager as blocks complete
    public List<BuildingBlock> largestBlocks = new();                   // "Cutest blocks"
    public List<BuildingBlock> discardedBlocks = new();                 // The blocks that remain black and disappear

    private bool isFading = false;
    private Stopwatch stopwatch;


    public void Initiate()
    {
        if (!isFading)
        {
            Util.WriteLog("Starting initial fade");
            StartCoroutine(InitiallyFadeObjects());
            isFading = true;
        }
    }

    private IEnumerator InitiallyFadeObjects()
    {
        // This higher fps section is a good spot to get rid of some of the massive parent objects that are not needed anymore
        ComponentDestroyer.Instance.DestroyComponents();

        StartCoroutine(FindLargestBlocks());
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
        Util.WriteLog("Initial fade complete");

        StartCoroutine(LightUpCutestBlocks());
        RenderSettings.skybox = SkyboxMaterial;
    }

    // Find 100 largest building blocks by comparing the number of vertices. 
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
            yield return new WaitForFixedUpdate();
        }

        // Sort blocks by size
        blockSizes.Sort((a, b) => b.Value.CompareTo(a.Value));
        yield return null;

        // Skip 2 largest blocks, then take 100 remaining largest blocks.
        largestBlocks = blockSizes.Skip(2).Take(100).Select(pair => pair.Key).ToList();
        yield return null;

        // Randomize the order of blocks in the list
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
        Util.WriteLog("Found and randomized the 100 largest BuildingBlocks.");
    }

    public IEnumerator LightUpCutestBlocks()
    {
        Destroy(GetComponent<EnergyCounter>());
        Destroy(EnergyTextLine);
        int blockCount = largestBlocks.Count;

        // Delay between blocks lighting up is progressively reduced
        float initialDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 0.4f,
            Configuration.SpeedType.Fast => 0.05f,
            Configuration.SpeedType.Dev => 0.03f,
            _ => 0.8f, // default
        };
        float minimumDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 0.04f,
            Configuration.SpeedType.Fast => 0.03f,
            Configuration.SpeedType.Dev => 0.03f,
            _ => 0.05f, // default
        };

        for (int i = 0; i < blockCount; i++)
        {
            BuildingBlock bb = largestBlocks[i];
            bb.gameObject.GetComponent<MeshRenderer>().material.color = bb.color;
            yield return new WaitForSeconds(initialDelay);
            initialDelay *= 0.90f;
            if (initialDelay < minimumDelay)
            {
                initialDelay = minimumDelay;
            }
        }
        MainManager.Instance.StartDisplayBlocks();
    }
}