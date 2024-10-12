using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DisplayBlock
{
    public GameObject gameObject;
    public bool isActive;
    public float startupTime;
    public float elapsedTime;
    public Vector3 rotationSpeed;
    public Vector3 currentPosition;
    public Vector3 targetPosition;
    public Vector3 finalPosition;
    public bool isAtFinalPosition;
    public bool isLastOne;
    public Color color;
    public bool isFlashing;
    public float elapsedFlash;

    public DisplayBlock(GameObject go, float startupTime, Vector3 finalPosition)
    {
        this.gameObject = go;
        this.isActive = false;
        this.startupTime = startupTime;
        this.elapsedTime = 0f;
        this.isLastOne = false;
        this.color = go.GetComponent<MeshRenderer>().material.color;
        this.isFlashing = false;
        this.elapsedFlash = 0f;
        this.isAtFinalPosition = false;

        // Full rotation around each axis in 1-3 seconds
        float xrot = 360f / UnityEngine.Random.Range(5f, 20f);
        float yrot = 360f / UnityEngine.Random.Range(5f, 20f);
        float zrot = 360f / UnityEngine.Random.Range(5f, 20f);
        this.rotationSpeed = new Vector3(xrot, yrot, zrot);
        this.currentPosition = go.transform.position;
        this.targetPosition = new Vector3(500, 250, 800);
        this.finalPosition = finalPosition;
    }
    public void Flash()
    {
        this.isFlashing = true;
        this.gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
        this.elapsedFlash = 0f;
        this.gameObject.transform.localScale = new Vector3(2, 2, 2);
    }
}

public class DisplayBlocksManager : MonoBehaviour
{
    public static DisplayBlocksManager Instance { get; private set; }
    public List<DisplayBlock> DisplayBlocks = new();
    public float elapsedTime;
    private bool blocksChosen = false;
    private bool analysisComplete = false;
    private float launchInterval;       // Interval at which cutest blocks are launched onto display
    private float timeToFirstTarget;    // Time to move from initial to first target
    private float timeToFinalTarget;    // Time to move from first target to final analysis position
    private float flyAwayLaunchTime;         // Random time between 0 and f seconds for blocks flying away when analysis is complete
    private float rotationAccelerationTime = 10f; // Time to accelerate rotation

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
    private bool hidingDiscardedBlocks;
    private bool flashingHasEnded = false;

    void Awake()
    {
        Instance = this;

        InitializeDesignTextLines();
    }

    public void Initiate()
    {
        StartCoroutine(LightUpWallGradually());
        WallQuad.SetActive(true);
        blocksChosen = true;

        launchInterval = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 0.07f,  // 0.08f for better music sync later
            Configuration.SpeedType.Fast => 0.03f,
            Configuration.SpeedType.Dev => 0.01f,
            _ => 0.1f, // default
        };

        timeToFirstTarget = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 1.2f,
            Configuration.SpeedType.Fast => 0.8f,
            Configuration.SpeedType.Dev => 0.3f,
            _ => 2f, // default
        };
        timeToFinalTarget = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 1.8f,  // 2f for better music sync later
            Configuration.SpeedType.Fast => 0.5f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 4f, // default
        };
        flyAwayLaunchTime = Configuration.Speed switch
        {
            Configuration.SpeedType.Normal => 2f,
            Configuration.SpeedType.Fast => 1f,
            Configuration.SpeedType.Dev => 0.5f,
            _ => 2f, // default
        };

        //Debug.Log($"times {launchInterval}  {timeToFirstTarget}  {timeToFinalTarget}");

        float startTime = 0f;

        // Get the building blocks from the previous scene and assign them to objects with target positions and rotation speeds.
        BuildingBlocksManager bbmgr = BuildingBlocksManager.Instance;
        float finalX = 3000;
        float finalY = 1500;
        float finalZ = -2000;

        foreach (BuildingBlock bb in bbmgr.largestBlocks)
        {
            startTime += launchInterval;
            Vector3 finalPosition = new Vector3(finalX, finalY, finalZ);
            DisplayBlock db = new DisplayBlock(bb.gameObject, startTime, finalPosition);
            DisplayBlocks.Add(db);

            finalX -= 250;
            if (finalX <= -2000)
            {
                finalX = 3000;
                finalY -= 300;
            }
        }
        DisplayBlocks[DisplayBlocks.Count - 1].isLastOne = true;
        elapsedTime = 0f;
    }

    void Update()
    {
        if (blocksChosen)
        {
            MoveBlocksToAnalysisPosition();  // will also start coroutines for block flashing and text lines
        }
        if (analysisComplete)
        {
            MoveBlocksAway();
        }
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

    void MoveBlocksToAnalysisPosition()
    {
        elapsedTime += Time.deltaTime;
        foreach (DisplayBlock db in DisplayBlocks)
        {
            if ((!db.isActive) && (elapsedTime > db.startupTime))
            {
                db.isActive = true;
            }
            if (db.isActive)
            {
                db.elapsedTime += Time.deltaTime;

                // rotating object, interpolated rotation speed
                float rotationMultiplier = Mathf.Clamp01(db.elapsedTime / rotationAccelerationTime);
                Vector3 currentRotationSpeed = db.rotationSpeed * rotationMultiplier;
                db.gameObject.transform.Rotate(currentRotationSpeed * Time.deltaTime);

                // flashing
                if (db.isFlashing)
                {
                    db.elapsedFlash += Time.deltaTime;
                    if (db.elapsedFlash > 1f)
                    {
                        db.isFlashing = false;
                        db.elapsedFlash = 0;
                        db.gameObject.GetComponent<MeshRenderer>().material.color = db.color;
                        db.gameObject.transform.localScale = new Vector3(1, 1, 1);
                    }
                }

                // moving to position
                if (!db.isAtFinalPosition)
                {
                    // **Phase 1: Move from currentPosition to targetPosition over timeToFirstTarget seconds**
                    if (db.elapsedTime < timeToFirstTarget)
                    {
                        float t = db.elapsedTime / timeToFirstTarget;
                        t = EaseInCubic(t);
                        db.gameObject.transform.position = Vector3.Lerp(db.currentPosition, db.targetPosition, t);
                        if (db.isLastOne == true)
                        {
                            if (!hidingDiscardedBlocks)
                            {
                                hidingDiscardedBlocks = true;
                                StartCoroutine(HideDiscardedBlocks());
                            }
                        }
                    }
                    // **Phase 2: Move from targetPosition to finalPosition over timeToFinalTarget seconds**
                    else if (db.elapsedTime < (timeToFirstTarget + timeToFinalTarget))
                    {
                        float t = (db.elapsedTime - timeToFirstTarget) / timeToFinalTarget;
                        t = EaseOutCubic(t);
                        db.gameObject.transform.position = Vector3.Lerp(db.targetPosition, db.finalPosition, t);
                    }
                    // **Phase 3: Ensure the object is at finalPosition after interpolation**
                    else
                    {
                        db.gameObject.transform.position = db.finalPosition;
                        db.isAtFinalPosition = true;
                        db.currentPosition = db.finalPosition;
                        db.targetPosition = db.finalPosition;

                        // Activate next scene when the last object reaches its final position
                        if (db.isLastOne == true)
                        {
                            Heading.text = "analysing house construction";
                            StartCoroutine(FlashRandomObject());
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

    IEnumerator FlashRandomObject()
    {
        while (!flashingHasEnded)
        {
            int randomIndex = UnityEngine.Random.Range(0, DisplayBlocks.Count - 1);
            DisplayBlocks[randomIndex].Flash();
            yield return new WaitForSeconds(0.334f);
        }
    }

    // Different acceleration functions to try for the objects' movement.
    float EaseInOut(float t)
    {
        return t < 0.5f
            ? 2 * t * t              // Ease-in
            : -1 + (4 - 2 * t) * t;  // Ease-out
    }

    float EaseOutQuadratic(float t)
    {
        return -t * (t - 2);
    }

    float EaseOutCubic(float t)
    {
        return 1 - Mathf.Pow(1 - t, 3);
    }
    float EaseOutExponential(float t)
    {
        return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
    }

    float EaseInQuadratic(float t)
    {
        return t * t;
    }

    float EaseInCubic(float t)
    {
        return t * t * t;
    }

    float EaseInExponential(float t)
    {
        return t == 0 ? 0 : Mathf.Pow(2, 10 * (t - 1));
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
                designPhasesTextLines.RemoveAt(randomIndex);
            }
        }
    }

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
        flashingHasEnded = true;
        blocksChosen = false;
        analysisComplete = true;
        elapsedTime = 0f;
        Vector3 finalPosition = new Vector3(5000, 4000, -2000);
        foreach (DisplayBlock db in DisplayBlocks)
        {
            db.startupTime = UnityEngine.Random.Range(0, flyAwayLaunchTime);
            if (db.isLastOne == true)
            {
                if (Configuration.Speed == Configuration.SpeedType.Normal)
                {
                    db.startupTime += 2f;
                }
                else
                {
                    db.startupTime += 1f;
                }
            }
            db.elapsedTime = 0;
            db.finalPosition = finalPosition;
            db.isAtFinalPosition = false;
            db.isActive = false;
        }
    }

    void MoveBlocksAway()
    {
        // complete any remaining flashing gracefully
        foreach (DisplayBlock db in DisplayBlocks)
        {
            float rotationMultiplier = 1;   // just set a rotation speed at this point rather than accelerate from 0 again
            Vector3 currentRotationSpeed = db.rotationSpeed * rotationMultiplier;
            db.gameObject.transform.Rotate(currentRotationSpeed * Time.deltaTime);

            if (db.isFlashing)
            {
                db.isFlashing = false;
                db.elapsedFlash = 0;
                db.gameObject.GetComponent<MeshRenderer>().material.color = db.color;
                db.gameObject.transform.localScale = new Vector3(1, 1, 1);
            }
        }

        elapsedTime += Time.deltaTime;
        foreach (DisplayBlock db in DisplayBlocks)
        {
            if ((!db.isActive) && (elapsedTime > db.startupTime))
            {
                db.isActive = true;
            }
            if (db.isActive)
            {
                db.elapsedTime += Time.deltaTime;

                if (!db.isAtFinalPosition)
                {
                    // **Phase 1: Move from currentPosition to new finalPosition over timeToFirstTarget seconds**
                    if (db.elapsedTime < timeToFirstTarget)
                    {
                        float t = db.elapsedTime / timeToFirstTarget;
                        if (db.isLastOne)
                        {
                            t = db.elapsedTime / (timeToFirstTarget / 2f);
                        }
                        t = EaseInCubic(t);
                        db.gameObject.transform.position = Vector3.Lerp(db.currentPosition, db.finalPosition, t);
                        if (db.isLastOne == true)
                        {
                            Heading.text = "arranging house";
                        }
                    }
                    // **Phase 3: Ensure the object is at finalPosition after interpolation**
                    else
                    {
                        db.gameObject.transform.position = db.finalPosition;
                        db.isAtFinalPosition = true;
                        db.gameObject.SetActive(false);

                        // Activate next scene when the last object reaches its final position
                        if (db.isLastOne == true)
                        {
                            db.isLastOne = false;
                            LeftTextLines.text = "";
                            CenterTextLines.text = "";
                            RightTextLines.text = "";
                            Destroy(textLinesParent);
                            PhysicsBlocksManager.Instance.Initiate();
                        }
                    }
                }
            }
        }
    }
}