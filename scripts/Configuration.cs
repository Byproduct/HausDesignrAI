using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public static class Configuration
{
    public const int MinimumObjectSize = 100;   // hide generated blocks with fewer vertices
    public const bool OnscreenStats = false;
    public const bool OnscreenFps = true;
    public const bool DebugOutput = false;

    // Speed can be "normal" or "fast" (changed with s key) or "dev" (changed with f key). Other classes can subscribe to this event to react accordingly.
    public enum SpeedType
    {
        Normal,
        Fast,
        Dev
    }

    public static event Action<SpeedType> OnSpeedChanged;
    public static SpeedType _speed = SpeedType.Normal;

    public static SpeedType Speed
    {
        get => _speed;
        set
        {
            if (_speed != value)
            {
                _speed = value;
                OnSpeedChanged?.Invoke(_speed);
            }
        }
    }
}

