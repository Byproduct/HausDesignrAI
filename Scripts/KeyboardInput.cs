using UnityEngine;

public class KeyboardInput : MonoBehaviour
{
    public static KeyboardInput Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    private const float maxSpeedChangeRate = 0.1f;

    public AudioSource JazzPlayer;

    private float cooldown = 0;


    private void FixedUpdate()
    {
        if (cooldown > 0)
        {
            cooldown -= Time.fixedDeltaTime;
        }
    }
    void Update()
    {
        if (cooldown <= 0)
        {
            if (Input.GetKey("m"))
            {
                JazzPlayer.Stop();
            }
            if (Input.GetKey("o"))
            {
                cooldown = maxSpeedChangeRate;
                if (Configuration.Speed == Configuration.SpeedType.Normal)
                {
                    Debug.Log($"Speed changed from {Configuration.Speed} to fast");
                    Configuration.Speed = Configuration.SpeedType.Fast;
                }
                else
                {
                    Debug.Log($"Speed changed from {Configuration.Speed} to normal");
                    Configuration.Speed = Configuration.SpeedType.Normal;
                }
            }
            if (Input.GetKey("p"))
            {
                cooldown = maxSpeedChangeRate;
                if (Configuration.Speed != Configuration.SpeedType.Dev)
                {
                    Debug.Log($"Speed changed from {Configuration.Speed} to dev");
                    Configuration.Speed = Configuration.SpeedType.Dev;
                }
            }
        }

        // Check if the escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
