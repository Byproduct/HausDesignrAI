// A manager class for fading in propagator object colors.
// The objects are otherwise managed by PropagatorManager.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;

public class PropagatorColorManager : MonoBehaviour
{
    public static PropagatorColorManager Instance { get; private set; }

    public List<Color[]> ColorStorage;         // All colors and their fade-in gradients, generated ahead of time.
    public int ColorsPerPropagator;            // Number of color shades to fade through when object appears
    public int UpperBoundary;                  // This is just colorsPerProagator - 1 cached for the every-frame calculation
    public const short DifferentColors = 100;  // number of different final colors - when exceeded will wrap-around and start from color number 0 again

    // When a propagator object is created, this dict gets its renderer and adjusts the color based on elapsed time.
    private ColorFader[] colorFaders;
    private int colorFaderCount;
    private ObjectPool<ColorFader> colorFaderPool;
    private MaterialPropertyBlock PropBlock;
    private Stopwatch colorStopwatch = new Stopwatch();
    private bool propagating;

    void Awake()
    {
        Instance = this;
        PropBlock = new MaterialPropertyBlock();
        colorFaders = new ColorFader[10000];
        colorFaderCount = 0;
        ColorsPerPropagator = PropagatorManager.Instance.ColorsPerPropagator;
        UpperBoundary = ColorsPerPropagator - 1;
        GenerateColors();

        colorFaderPool = new ObjectPool<ColorFader>(
    () => new ColorFader(),   // Empty constructor for new objects
    null,                     // OnGetFromPool = null
    null,                     // OnReleaseToPool = null
    null,                     // OnDestroyPooledObject = null
    false,                    // No collection check
    10000,                    // Default size
    100000                    // Max size
);
    }

    public class ColorFader
    {
        public Renderer Renderer;
        public float FadeTime;
        public short GroupId;
        public int CurrentColorIndex;
        public Color[] ColorArray;   // The array in ColorStorage specific to this object 

        public ColorFader() { }

        public void Initialize(Renderer renderer, short groupId)
        {
            Renderer = renderer;
            GroupId = groupId;
            FadeTime = 0f;
            MaterialPropertyBlock tempBlock = new MaterialPropertyBlock();
            tempBlock.SetColor("_Color", new Color32(222, 222, 222, 255));
            renderer.SetPropertyBlock(tempBlock);
        }
    }

    public void AddNew(Renderer r, short groupId)
    {
        // Expand the array if needed
        if (colorFaderCount == colorFaders.Length)
        {
            System.Array.Resize(ref colorFaders, colorFaders.Length + 1000);
        }
        ColorFader colorFader = colorFaderPool.Get();
        colorFader.Initialize(r, groupId);
        colorFader.ColorArray = ColorStorage[groupId % DifferentColors];
        colorFaders[colorFaderCount++] = colorFader;
    }

    void Update()
    {
        if (!propagating) return;

        float deltaTime = Time.deltaTime;

        ColorsPerPropagator = PropagatorManager.Instance.ColorsPerPropagator;
        float fadeTime = PropagatorManager.Instance.PropagatorFadeInTime;
        for (int i = colorFaderCount - 1; i >= 0; i--)
        {
            ColorFader colorFader = colorFaders[i];
            colorFader.FadeTime += deltaTime;

            // Calculate new color index based on fade time, and change renderer color if the index has changed
            int colorIndex = (int)((colorFader.FadeTime) / fadeTime * ColorsPerPropagator);

            // Remove instances with color indexes past upper boundary (= fade complete)
            if (colorIndex > UpperBoundary)
            {
                // Swap with last element and decrease count
                colorFaders[i] = colorFaders[--colorFaderCount];
                colorFaderPool.Release(colorFader);
                continue;
            }

            if (colorIndex != colorFader.CurrentColorIndex)
            {
                colorFader.CurrentColorIndex = colorIndex;
                PropBlock.SetColor("_Color", colorFader.ColorArray[colorIndex]);
                if (colorFader.Renderer != null)
                {
                    colorFader.Renderer.SetPropertyBlock(PropBlock);
                }
            }
        }
    }

    public void ActivateColorFades()
    {
        propagating = true;
    }

    void GenerateColors()
    {
        Stopwatch sw = Stopwatch.StartNew();
        ColorStorage = new List<Color[]>();

        float hueMin = UnityEngine.Random.Range(0f, 1f);
        float hueRange = UnityEngine.Random.Range(0.2f, 0.35f);
        Util.WriteLog($"House hue range is {hueMin} + {hueRange}");
        for (int i = 0; i < DifferentColors + 2; i++)
        {
            Color[] newColors = new Color[ColorsPerPropagator];
            Color startColor = new Color(0f, 0.0f, 0.0f);
            Color randomColor = GenerateRandomColor(hueMin, hueRange);
            float h0, s0, v0;
            float h1, s1, v1;
            Color.RGBToHSV(startColor, out h0, out s0, out v0);
            Color.RGBToHSV(randomColor, out h1, out s1, out v1);

            for (int j = 0; j < ColorsPerPropagator; j++)
            {
                float t = (float)j / ColorsPerPropagator;
                float s = Mathf.Lerp(s0, s1, t);

                newColors[j] = Color.HSVToRGB(h1, s, v1);
            }
            ColorStorage.Add(newColors);
        }
        sw.Stop();
        Util.WriteLog($"Generated colors in {sw.ElapsedMilliseconds} ms");
    }

    public Color[] GetColors(short groupId)
    {
        {
            return ColorStorage[groupId % DifferentColors];  // repeat colors from 0 when all have been cycled through
        }
    }

    public Color GetFinalColor(short groupId)
    {
        int lastIndex = ColorStorage[groupId & DifferentColors].Length - 1;
        return ColorStorage[groupId % DifferentColors][lastIndex];
    }


    // Generate random color from a range in the hue circle. 
    // Handle wrapping around if maximum hue exceeds 1
    public Color GenerateRandomColor(float hueMin, float hueRange)
    {
        float saturation = UnityEngine.Random.Range(0.75f, 1f);
        float brightness = UnityEngine.Random.Range(0.75f, 1f);

        float hueMax = hueMin + hueRange;

        float hue;
        if (hueMax > 1f)
        {
            float wrappedHueMax = hueMax - 1f;
            if (UnityEngine.Random.value < (1f - hueMin) / (hueMax - hueMin))
            {
                // Choose hue from hueMin to 1
                hue = UnityEngine.Random.Range(hueMin, 1f);
            }
            else
            {
                // Choose hue from 0 to wrappedHueMax
                hue = UnityEngine.Random.Range(0f, wrappedHueMax);
            }
        }
        else
        {
            // Normal range between hueMin and hueMax
            hue = UnityEngine.Random.Range(hueMin, hueMax);
        }
        return Color.HSVToRGB(hue, saturation, brightness);
    }
}