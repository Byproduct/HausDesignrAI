// The final version of the demo shows only the FPS counter. Pool stats are disabled but can be re-enabled in Configuration.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PerformanceMetrics : MonoBehaviour
{
    private const int maxQueueSize = 10;
    private const float updateFrequency = 0.1f;

    public GameObject DevUI;
    public TextMeshProUGUI fpsText;

    public TextMeshProUGUI completeShapes;
    public TextMeshProUGUI propagatorsText;
    public TextMeshProUGUI propagatorObjectsText;
    public TextMeshProUGUI propagatorRowObjectsText;
    public TextMeshProUGUI propagatorPoolStatsText;
    public TextMeshProUGUI wallPoolStatsText;

    private int frames = 0;
    private Queue<int> frameQueue = new Queue<int>();
    private int rollingFrameSum = 0;
    private bool statDelayCompleted = false;
    private float time;

    private void Start()
    {
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
        string str = $"Propagator pool total: {PropagatorManager.Instance.PropagatorGameObjectPool.CountAll} \n Propagator pool active: {PropagatorManager.Instance.PropagatorGameObjectPool.CountActive} \n Propagator pool inactive: {PropagatorManager.Instance.PropagatorGameObjectPool.CountInactive}";
        propagatorPoolStatsText.text = str;
        string wallStr = $"Wall pool total: {WallManager.Instance.WallPartPool.CountAll} \n Wall pool active: {WallManager.Instance.WallPartPool.CountActive} \n Wall pool inactive: {WallManager.Instance.WallPartPool.CountInactive}";
        wallPoolStatsText.text = wallStr;
    }
}
