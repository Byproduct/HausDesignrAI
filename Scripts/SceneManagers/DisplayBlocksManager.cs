// Chosen blocks fly up for display and faux analysis

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DisplayBlock
{
    public GameObject GameObject;
    public bool IsActive;
    public float StartupTime;
    public float ElapsedTime;
    public Vector3 RotationSpeed;
    public Vector3 CurrentPosition;
    public Vector3 TargetPosition;
    public Vector3 FinalPosition;
    public bool IsAtFinalPosition;
    public bool IsLastOne;
    public Color Color;
    public bool IsHighlighted;
    public float ElapsedHighlightTime;

    public DisplayBlock(GameObject go, float startupTime, Vector3 finalPosition)
    {
        GameObject = go;
        IsActive = false;
        StartupTime = startupTime;
        ElapsedTime = 0f;
        TargetPosition = new Vector3(500, 250, 800);
        FinalPosition = finalPosition;
        IsAtFinalPosition = false;
        IsLastOne = false;
        Color = go.GetComponent<MeshRenderer>().material.color;
        IsHighlighted = false;
        ElapsedHighlightTime = 0f;

        // Full rotation around each axis in random time
        float xrot = 360f / UnityEngine.Random.Range(5f, 20f);
        float yrot = 360f / UnityEngine.Random.Range(5f, 20f);
        float zrot = 360f / UnityEngine.Random.Range(5f, 20f);
        RotationSpeed = new Vector3(xrot, yrot, zrot);
        CurrentPosition = go.transform.position;
    }
    public void Highlight()
    {
        IsHighlighted = true;
        GameObject.GetComponent<MeshRenderer>().material.color = Color.red;
        ElapsedHighlightTime = 0f;
        GameObject.transform.localScale = new Vector3(2, 2, 2);
    }
}


public class DisplayBlocksManager : MonoBehaviour
{
    public static DisplayBlocksManager Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    private const float rotationAccelerationTime = 10f;  // Time over which rotation is accelerated

    public GameObject WallQuad;
    public GameObject WallLight;
    public TextMeshProUGUI Heading;
    public GameObject textLinesParent;
    public GameObject designPhasesTextLinesParent;
    public TextMeshProUGUI LeftTextLines;
    public TextMeshProUGUI CenterTextLines;
    public TextMeshProUGUI RightTextLines;
    public List<string> designPhasesDisplayLines;
    public Material DissolvingMaterial;

    public List<DisplayBlock> DisplayBlocks = new();
    public float elapsedTime;

    private bool blocksChosen;
    private bool analysisComplete;
    private bool highlightingComplete;
    private bool hidingDiscardedBlocks;

    private float launchInterval;                 // Interval at which cutest blocks are launched onto display
    private float timeToFirstTarget;              // Time to move from initial to first target
    private float timeToFinalTarget;              // Time to move from first target to final analysis position
    private float flyAwayLaunchTime;              // Random time between 0 and f seconds for blocks flying away when analysis is complete

    private void Start()
    {
        InitializeDesignTextLines();
    }

    /// Load "designphases.txt" and pick 30 lines at random to be shown as display lines.
    /// Line 17 is always "Planning building block placement", which "fails".
    void InitializeDesignTextLines()
    {
        LeftTextLines.text = "";
        CenterTextLines.text = "";
        RightTextLines.text = "";

        string designPhasesFileName = "designphases";
        List<string> designPhasesTextLines;

        string text = Resources.Load<TextAsset>(designPhasesFileName).text;
        designPhasesTextLines = new List<string>(text.Split('\n'));
        designPhasesTextLines.RemoveAll(line => string.IsNullOrWhiteSpace(line));
        designPhasesDisplayLines = new List<string>();
        for (int i = 0; i < 30; i++)
        {
            if (i == 17)
            {
                designPhasesDisplayLines.Add("planning building block placement... ");
            }
            else
            {
                int randomIndex = UnityEngine.Random.Range(0, designPhasesTextLines.Count);
                designPhasesDisplayLines.Add($"{designPhasesTextLines[randomIndex]}... ");
                designPhasesTextLines.RemoveAt(randomIndex); // no repeats
            }
        }
    }

    public void Initiate()
    {
        WallQuad.SetActive(true);
        blocksChosen = true;

        launchInterval = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 0.07f,
            Configuration.SpeedType.Fast => 0.03f,
            Configuration.SpeedType.Dev => 0.01f,
            _ => 0.1f, // default (now redundant)
        };

        timeToFirstTarget = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 1.2f,
            Configuration.SpeedType.Fast => 0.8f,
            Configuration.SpeedType.Dev => 0.3f,
            _ => 1.2f,
        };
        timeToFinalTarget = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 1.8f,
            Configuration.SpeedType.Fast => 0.5f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 1.8f,
        };
        flyAwayLaunchTime = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 2f,
            Configuration.SpeedType.Fast => 1f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 2f,
        };

        // Get the building blocks from the previous scene and assign them to objects with target positions and rotation speeds.
        float startTime = 0f;
        float finalX = 3000;
        float finalY = 1500;
        float finalZ = -2000;
        foreach (BuildingBlock bb in BuildingBlocksManager.Instance.largestBlocks)
        {
            startTime += launchInterval;
            Vector3 finalPosition = new Vector3(finalX, finalY, finalZ);
            DisplayBlock db = new DisplayBlock(bb.gameObject, startTime, finalPosition);
            DisplayBlocks.Add(db);

            // Blocks' final positions are spread out to 5 rows and 20 blocks per row, X=3000 to X=-2000
            finalX -= 250;
            if (finalX <= -2000)
            {
                finalX = 3000;
                finalY -= 300;
            }
        }
        DisplayBlocks[DisplayBlocks.Count - 1].IsLastOne = true;
        elapsedTime = 0f;
        StartCoroutine(LightUpWallGradually());
    }

    IEnumerator LightUpWallGradually()
    {
        float wallLightRate = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 0.005f,
            Configuration.SpeedType.Fast => 0.01f,
            Configuration.SpeedType.Dev => 0.01f,
            _ => 0.005f,
        };

        WallLight.SetActive(true);
        float i = 0f;
        while (i < 1)
        {
            i = i + wallLightRate;
            WallLight.GetComponent<Light>().intensity = i;
            yield return new WaitForFixedUpdate();
        }
        WallLight.GetComponent<Light>().intensity = 1;
    }

    void Update()
    {
        if (blocksChosen)
        {
            MoveBlocksToAnalysisPosition();  // will also start coroutines for block highlighting and text lines
        }
        if (analysisComplete)
        {
            MoveBlocksAway();
        }
    }

    void MoveBlocksToAnalysisPosition()
    {
        elapsedTime += Time.deltaTime;
        foreach (DisplayBlock db in DisplayBlocks)
        {
            // Activate blocks gradually based on their individual startup times
            if ((!db.IsActive) && (elapsedTime > db.StartupTime))
            {
                db.IsActive = true;
            }
            if (db.IsActive)
            {
                db.ElapsedTime += Time.deltaTime;

                // Block rotation accelerates over time
                float rotationMultiplier = Mathf.Clamp01(db.ElapsedTime / rotationAccelerationTime);
                Vector3 currentRotationSpeed = db.RotationSpeed * rotationMultiplier;
                db.GameObject.transform.Rotate(currentRotationSpeed * Time.deltaTime);

                // Calculate duration for blocks that are being highlighted
                if (db.IsHighlighted)
                {
                    db.ElapsedHighlightTime += Time.deltaTime;
                    if (db.ElapsedHighlightTime > 1f)
                    {
                        db.IsHighlighted = false;
                        db.ElapsedHighlightTime = 0;
                        db.GameObject.GetComponent<MeshRenderer>().material.color = db.Color;
                        db.GameObject.transform.localScale = new Vector3(1, 1, 1);
                    }
                }

                // Move blocks to position
                if (!db.IsAtFinalPosition)
                {
                    // Phase 1: Move from currentPosition to targetPosition over timeToFirstTarget seconds
                    if (db.ElapsedTime < timeToFirstTarget)
                    {
                        float t = db.ElapsedTime / timeToFirstTarget;
                        t = Util.EaseInCubic(t);
                        db.GameObject.transform.position = Vector3.Lerp(db.CurrentPosition, db.TargetPosition, t);
                        if (db.IsLastOne == true)
                        {
                            if (!hidingDiscardedBlocks)
                            {
                                hidingDiscardedBlocks = true;
                                StartCoroutine(HideDiscardedBlocks());
                            }
                        }
                    }
                    // Phase 2: Move from targetPosition to finalPosition over timeToFinalTarget seconds
                    else if (db.ElapsedTime < (timeToFirstTarget + timeToFinalTarget))
                    {
                        float t = (db.ElapsedTime - timeToFirstTarget) / timeToFinalTarget;
                        t = Util.EaseOut(t, 3);
                        db.GameObject.transform.position = Vector3.Lerp(db.TargetPosition, db.FinalPosition, t);
                    }

                    // Phase 3: Ensure the object is at finalPosition after interpolation
                    else
                    {
                        db.GameObject.transform.position = db.FinalPosition;
                        db.IsAtFinalPosition = true;
                        db.CurrentPosition = db.FinalPosition;
                        db.TargetPosition = db.FinalPosition;

                        // Activate next scene when the last object reaches its final position
                        if (db.IsLastOne == true)
                        {
                            Heading.text = "analysing house construction";
                            StartCoroutine(HighlightRandomObject());
                            StartCoroutine(DisplayTextLines());
                        }
                    }
                }
            }
        }
    }

    // Dissolve discarded blocks
    IEnumerator HideDiscardedBlocks()
    {
        List<BuildingBlock> discardedBlocks = BuildingBlocksManager.Instance.discardedBlocks;
        foreach (BuildingBlock block in discardedBlocks)
        {
            Renderer renderer = block.gameObject.GetComponent<Renderer>();
            renderer.material = new Material(DissolvingMaterial);
            renderer.material.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.1f));
            ObjectDissolver od = block.gameObject.AddComponent<ObjectDissolver>();
            od.SetDissolvingTime(10f);
            yield return new WaitForSeconds(0.1f);
        }
    }


    IEnumerator HighlightRandomObject()
    {
        while (!highlightingComplete)
        {
            int randomIndex = UnityEngine.Random.Range(0, DisplayBlocks.Count - 1);
            DisplayBlocks[randomIndex].Highlight();
            yield return new WaitForSeconds(0.334f);
        }
    }

 
    // The 30 text lines are split into three separate objects (columns).
    IEnumerator DisplayTextLines()
    {
        designPhasesTextLinesParent.SetActive(true);
        float randomDelayMin = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 0.08f,
            Configuration.SpeedType.Fast => 0.025f,
            Configuration.SpeedType.Dev => 0.01f,
            _ => 0.2f,
        };
        float randomDelayMax = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 1.35f,
            Configuration.SpeedType.Fast => 0.4f,
            Configuration.SpeedType.Dev => 0.01f,
            _ => 0.2f,
        };
        float errorDelay = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 4.5f,
            Configuration.SpeedType.Fast => 2f,
            Configuration.SpeedType.Dev => 0.01f,
            _ => 0.2f,
        };

        string leftStr = "";
        string centerStr = "";
        string rightStr = "";
        int i = 0;
        while (i < 30)
        {
            if (i < 10)
            {
                leftStr += $"{designPhasesDisplayLines[i]}";
            }
            if (i >= 10 && i < 20)
            {
                centerStr += $"{designPhasesDisplayLines[i]}";
            }
            if (i >= 20)
            {
                rightStr += $"{designPhasesDisplayLines[i]}";
            }
            LeftTextLines.text = leftStr;
            CenterTextLines.text = centerStr;
            RightTextLines.text = rightStr;
            if (i == 17)
            {
                yield return new WaitForSeconds(errorDelay);
            }
            else
            {
                float randomDelay = UnityEngine.Random.Range(randomDelayMin, randomDelayMax);
                yield return new WaitForSeconds(randomDelay);
            }
            if (i < 10)
            {
                leftStr += "ok\n";
            }
            if (i >= 10 && i < 20 && i != 17)
            {
                centerStr += "ok\n";
            }
            if (i == 17)
            {
                centerStr += "<color=red>error</color>\n";
            }
            if (i >= 20)
            {
                rightStr += $"ok\n";
            }
            LeftTextLines.text = leftStr;
            CenterTextLines.text = centerStr;
            RightTextLines.text = rightStr;
            i++;
        }

        // Analysis complete, reconfigure objects for flying out of the screen
        Heading.text = "analysis complete";
        highlightingComplete = true;
        blocksChosen = false;
        analysisComplete = true;
        elapsedTime = 0f;
        Vector3 finalPosition = new Vector3(5000, 4000, -2000);
        foreach (DisplayBlock db in DisplayBlocks)
        {
            db.StartupTime = UnityEngine.Random.Range(0, flyAwayLaunchTime);
            db.ElapsedTime = 0;
            db.FinalPosition = finalPosition;
            db.IsAtFinalPosition = false;
            db.IsActive = false;

            // The last object snoozes and misses its launch time a little
            if (db.IsLastOne == true)
            {
                if (Configuration.Speed == Configuration.SpeedType.Normal)
                {
                    db.StartupTime += 2f;
                }
                else
                {
                    db.StartupTime += 1f;
                }
            }

        }
    }

    void MoveBlocksAway()
    {
        // complete any remaining highlighting gracefully
        foreach (DisplayBlock db in DisplayBlocks)
        {
            Vector3 currentRotationSpeed = db.RotationSpeed;
            db.GameObject.transform.Rotate(currentRotationSpeed * Time.deltaTime);

            if (db.IsHighlighted)
            {
                db.IsHighlighted = false;
                db.ElapsedHighlightTime = 0;
                db.GameObject.GetComponent<MeshRenderer>().material.color = db.Color;
                db.GameObject.transform.localScale = new Vector3(1, 1, 1);
            }
        }

        elapsedTime += Time.deltaTime;
        foreach (DisplayBlock db in DisplayBlocks)
        {
            if ((!db.IsActive) && (elapsedTime > db.StartupTime))
            {
                db.IsActive = true;
            }
            if (db.IsActive)
            {
                db.ElapsedTime += Time.deltaTime;

                if (!db.IsAtFinalPosition)
                {
                    // Phase 1: Move from currentPosition to new finalPosition over timeToFirstTarget seconds
                    if (db.ElapsedTime < timeToFirstTarget)
                    {
                        float t = db.ElapsedTime / timeToFirstTarget;
                        if (db.IsLastOne)
                        {
                            t = db.ElapsedTime / (timeToFirstTarget / 2f);
                        }
                        t = Util.EaseInCubic(t);
                        db.GameObject.transform.position = Vector3.Lerp(db.CurrentPosition, db.FinalPosition, t);
                        if (db.IsLastOne == true)
                        {
                            Heading.text = "arranging house";  // Display "arranging house" a bit early so "placing floors" has some time to display
                        }
                    }
                    else
                    {
                        db.IsAtFinalPosition = true;
                        db.GameObject.SetActive(false);

                        // Activate next scene when the last object exceeds its time
                        if (db.IsLastOne == true)
                        {
                            db.IsLastOne = false;
                            LeftTextLines.text = "";
                            CenterTextLines.text = "";
                            RightTextLines.text = "";
                            Destroy(textLinesParent);
                            MainManager.Instance.StartPhysicsBlocks();
                        }
                    }
                }
            }
        }
    }
}