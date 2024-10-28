// Global configuration
using System;

public static class Configuration
{
    public const bool OnscreenStats = false;
    public const bool OnscreenFps = true;
    public const bool ConsoleLogging = true;
    public const bool VerboseLogging = false;

    // Speed can be "normal" or "fast" (changed with o key) or "dev" (changed with p key). Other classes can subscribe to this event to react accordingly.
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

