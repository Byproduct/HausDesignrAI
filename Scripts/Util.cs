// Common functions shared across multiple scripts
using UnityEngine;

public class Util
{
    public static Util Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    // Log all debug messages in a coordinated fashion. Nothing besides Unity console output is needed for the time being though.
    public static void WriteLog(string msg)
    {
        if (Configuration.ConsoleLogging)
        {
            Debug.Log(msg);
        }
    }

    public static void WriteVerboseLog(string msg)
    {
        if (Configuration.VerboseLogging)
        {
            Debug.Log(msg);
        }
    }


    // Various ease-in/out functions to try for smoother movements. Not necessarily using all of these in the final version.
    public static float EaseInOut(float t)
    {
        return t < 0.5f
            ? 2 * t * t              // Ease-in
            : -1 + (4 - 2 * t) * t;  // Ease-out
    }

    public static float EaseOut(float t, int exponent)
    {
        return 1 - Mathf.Pow(1 - t, exponent);
    }

    public static float EaseInQuadratic(float t)
    {
        return t * t;
    }

    public static float EaseInCubic(float t)
    {
        return t * t * t;
    }

    public static float EaseInExponential(float t)
    {
        return t == 0 ? 0 : Mathf.Pow(2, 10 * (t - 1));
    }
}