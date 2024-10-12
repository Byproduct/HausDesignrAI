using System.Collections;
using TMPro;
using UnityEngine;
using System.Diagnostics;
using static PropagatorManager;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PerformanceMetrics : MonoBehaviour
{
    public GameObject DevUI;

    private WallManager wallManager;

    private PropagatorManager propagatorManager;
    private PropagatorRowManager propagatorRowManager;

    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI propagatorsText;
    public TextMeshProUGUI completeShapes;
//    public TextMeshProUGUI completedShapesText;
    public TextMeshProUGUI propagatorObjectsText;
    public TextMeshProUGUI propagatorRowObjectsText;
    public TextMeshProUGUI propagatorPoolStatsText;
    public TextMeshProUGUI wallPoolStatsText;

    //private TextMeshProUGUI cpuText;
    //private TextMeshProUGUI gpuText;

    private Stopwatch cpuStopwatch;
    private double lastProcessorTime;
    private double lastUserProcessorTime;
    private Process process;

    private float updateFrequency = 0.1f;
    private float time;
    private int frames = 0;
    private Queue<int> frameQueue = new Queue<int>();
    private int rollingFrameSum = 0;
    private int maxQueueSize = 10;
    
    private bool stats = false;
    private bool statDelayCompleted = false;

    private void Start()
    {
        wallManager = WallManager.Instance;
        propagatorManager = PropagatorManager.Instance;
        propagatorRowManager = PropagatorRowManager.Instance;

        //cpuStopwatch = new Stopwatch();
        //cpuStopwatch.Start();
        //process = Process.GetCurrentProcess();
        //lastProcessorTime = process.TotalProcessorTime.TotalMilliseconds;
        //lastUserProcessorTime = process.UserProcessorTime.TotalMilliseconds;

        StartCoroutine(DelayStats(3f));  // delay or there will be null references

        if (!Configuration.OnscreenFps)
        {
            fpsText.text = "";
        }
        if (!Configuration.OnscreenStats)
        {
            propagatorPoolStatsText.text = "";
            wallPoolStatsText.text = "";
            DevUI.SetActive(false);
        }
    }

    
    IEnumerator DelayStats(float delay)
    {
        yield return new WaitForSeconds(delay);
        statDelayCompleted = true;
    }

    private void Update()
    {
        time += Time.deltaTime;
        frames++;

        while (time >= updateFrequency)
        {
            if (Configuration.OnscreenStats && statDelayCompleted)
            {
                ShowPoolStats();
            }
            frameQueue.Enqueue(frames);
            rollingFrameSum += frames;
            if (frameQueue.Count > maxQueueSize)
            {
                rollingFrameSum -= frameQueue.Dequeue();
            }
            UpdateFPS();
            frames = 0;
            time -= updateFrequency;
        }
    }

    void UpdateFPS()
    {
        if (Configuration.OnscreenFps)
        {
            int fps = Mathf.RoundToInt(rollingFrameSum / (1.0f));
            fpsText.text = $"{fps} FPS";
        }
    }

    void ShowPoolStats()
    {
        string str = $"Propagator pool total: {propagatorManager.SingleSquarePropagatorObjectPool.CountAll} \n Propagator pool active: {propagatorManager.SingleSquarePropagatorObjectPool.CountActive} \n Propagator pool inactive: {propagatorManager.SingleSquarePropagatorObjectPool.CountInactive}";
        propagatorPoolStatsText.text = str;
        string wallStr = $"Wall pool total: {wallManager.WallPartPool.CountAll} \n Wall pool active: {wallManager.WallPartPool.CountActive} \n Wall pool inactive: {wallManager.WallPartPool.CountInactive}";
        wallPoolStatsText.text = wallStr;
    }
    //void UpdateStats()
    //{
    //    //UpdateCPUUsage();
    //    //UpdateGPUUsage();

    //    var propagators = propagatorManager.Propagators;
    //    int p = 0;
    //    foreach (var set in propagators.Values)
    //    {
    //        p += set.Count;
    //    }
    //    if (p > 0)
    //    {
    //        propagatorsText.text = $"Propagators: {p}";
    //    }
    //    else
    //    {
    //        propagatorsText.text = "";
    //    }
    //    int groupId = propagatorManager.propagatorGroupId;

    //    //int completedShapes = propagatorManager.CompletedShapes.Count;
    //    //if (completedShapes > 0)
    //    //{
    //    //    completedShapesText.text = $"Completed shapes: {completedShapes}";
    //    //}
    //    //else
    //    //{
    //    //    completedShapesText.text = $"";
    //    //}
    //    var propagatorObjects = propagatorManager.PropagatorObjects;
    //    int po = 0;
    //    foreach (var set in propagatorObjects.Values)
    //    {
    //        po += set.Count;
    //    }
    //    if (po > 0)
    //    {
    //        propagatorObjectsText.text = $"Active objects: {po}";
    //    }
    //    else
    //    {
    //        propagatorObjectsText.text = "";
    //    }
    //    var propagatorRows = propagatorRowManager.CombinedRows;
    //    int cr = 0;
    //    foreach (var set in propagatorRows.Values)
    //    {
    //        cr += set.Count;
    //    }
    //    if (cr > 0)
    //    {
    //        propagatorRowObjectsText.text = $"Active rows: {cr}";
    //    }
    //    else
    //    {
    //        propagatorRowObjectsText.text = "";
    //    }
    //}
}

//void UpdateCPUUsage()
//{
//    process.Refresh();
//    double newProcessorTime = process.TotalProcessorTime.TotalMilliseconds;
//    double newUserProcessorTime = process.UserProcessorTime.TotalMilliseconds;

//    double processorTimeDifference = newProcessorTime - lastProcessorTime;
//    double userProcessorTimeDifference = newUserProcessorTime - lastUserProcessorTime;

//    double usage = (processorTimeDifference / (cpuStopwatch.ElapsedMilliseconds * System.Environment.ProcessorCount)) * 100;

//    cpuText.text = "CPU: " + usage.ToString("F0") + "%";

//    lastProcessorTime = newProcessorTime;
//    lastUserProcessorTime = newUserProcessorTime;
//    cpuStopwatch.Restart();
//}
