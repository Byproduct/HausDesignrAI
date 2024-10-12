// A manager class for fading in propagator object colors.
// The objects are otherwise managed by PropagatorManager. This class only holds references to their material blocks.

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class PropagatorColorManager : MonoBehaviour
{
    public static PropagatorColorManager Instance { get; private set; }

    public PropagatorManager propagatorManager;

    public List<Color[]> ColorStorage; // All colors and their fade-in gradients, generated ahead of time.
    private int colorsPerPropagator;   // shades to fade through after object appears
    private int upperBoundary;         // just colorsPerProagator - 1 cached for the every-frame calculation
    private short differentColors = 128; // number of different overall/final colors - has to be power of two for optimisation purposes.

    // Dictionary of premade material property blocks, for each main color and fade steps. This allows setting a premade property block rather than adjusting a new one each time. It's a memory hog but we're not running out of memory.
    public Dictionary<(short, int), MaterialPropertyBlock> MaterialPropertyBlocks = new();

    // When a propagator object is created, this dict gets its renderer and adjusts the color based on elapsed time.
    private RendererFadeInfo[] rendererFadeInfos;
    private int rendererFadeInfosCount;
    public List<RendererFadeInfo> RfiToDelete = new();  // temp list for deleting objects
    private MaterialPropertyBlock PropBlock;
    private Stopwatch colorStopwatch = new Stopwatch();
    private bool propagating = false;

    void Awake()
    {
        Instance = this;
        propagatorManager = PropagatorManager.Instance;
        PropBlock = new MaterialPropertyBlock();
        rendererFadeInfos = new RendererFadeInfo[10000];
        rendererFadeInfosCount = 0;
        colorsPerPropagator = propagatorManager.colorsPerPropagator;
        upperBoundary = colorsPerPropagator - 1;
        GenerateColors();
    }

    public class RendererFadeInfo
    {
        public Renderer Renderer;
        public float FadeTime = 0f;
        public short GroupId;
        public int CurrentColorIndex = 0;

        public RendererFadeInfo(Renderer renderer, short groupId)
        {
            this.Renderer = renderer;
            this.GroupId = groupId;
            MaterialPropertyBlock tempBlock = new MaterialPropertyBlock();
            tempBlock.SetColor("_Color", new Color32(222, 222, 222, 255));
            renderer.SetPropertyBlock(tempBlock);
        }
    }

    public void AddNew(Renderer r, short groupId)
    {
        // Expand the array if needed
        if (rendererFadeInfosCount == rendererFadeInfos.Length)
        {
            System.Array.Resize(ref rendererFadeInfos, rendererFadeInfos.Length + 1000);
        }
        rendererFadeInfos[rendererFadeInfosCount++] = new RendererFadeInfo(r, groupId);
    }

    void FixedUpdate()
    {
        if (!propagating) return;

        float deltaTime = Time.fixedDeltaTime;

        colorsPerPropagator = propagatorManager.colorsPerPropagator;
        float fadeTime = propagatorManager.propagatorFadeInTime;
        for (int i = rendererFadeInfosCount - 1; i >= 0; i--)
        {
            RendererFadeInfo rfi = rendererFadeInfos[i];
            rfi.FadeTime += deltaTime;

            // Calculate new color index based on fade time and change renderer color if the index has changed
            int colorIndex = (int)((rfi.FadeTime + 0.1f) / fadeTime * colorsPerPropagator);          // +0.1f is a kludge, to-do debug wtf is wrong with this...

            // Remove instances with color indexes past upper boundary (= fade complete)
            if (colorIndex > upperBoundary)
            {
                rendererFadeInfos[i] = rendererFadeInfos[--rendererFadeInfosCount];
                continue;
            }

            if (colorIndex != rfi.CurrentColorIndex)
            {
                rfi.CurrentColorIndex = colorIndex;
                PropBlock.SetColor("_Color", ColorStorage[rfi.GroupId % differentColors][colorIndex]);
                if (rfi.Renderer != null)
                {
                    rfi.Renderer.SetPropertyBlock(PropBlock);
                }
            }
        }
    }

    public void StartColorFades()
    {
        propagating = true;
    }

    void GenerateColors()
    {
        Stopwatch sw = Stopwatch.StartNew();
        ColorStorage = new List<Color[]>();

        float hueMin = Random.Range(0f, 1f);
        float hueRange = Random.Range(0.2f, 0.35f);
        UnityEngine.Debug.Log($"House hue range is {hueMin} + {hueRange}");
        for (int i = 0; i < differentColors + 2; i++)
        {
            Color[] newColors = new Color[colorsPerPropagator];
            Color startColor = new Color(0f, 0.0f, 0.0f);
            //Color randomColor = UnityEngine.Random.ColorHSV(0.35f, 0.65f, 0.75f, 1f, 0.75f, 1f);
            Color randomColor = GenerateRandomColor(hueMin, hueRange);
            float h0, s0, v0;
            float h1, s1, v1;
            Color.RGBToHSV(startColor, out h0, out s0, out v0);
            Color.RGBToHSV(randomColor, out h1, out s1, out v1);

            for (int j = 0; j < colorsPerPropagator; j++)
            {
                float t = (float)j / colorsPerPropagator;
                //                float h = Mathf.Lerp(h1, h1, t);
                float s = Mathf.Lerp(s0, s1, t);
                //                float v = Mathf.Lerp(v1, v1, t);

                newColors[j] = Color.HSVToRGB(h1, s, v1);
            }
            ColorStorage.Add(newColors);
        }
        sw.Stop();
        UnityEngine.Debug.Log($"Startup {sw.ElapsedMilliseconds} ms - generated colors.");
    }

    public Color[] GetColors(short groupId)
    {
        {
            return ColorStorage[groupId % differentColors];  // repeat colors from 0 if all are cycled through
        }
    }

    public Color GetFinalColor(short groupId)
    {
        int lastIndex = ColorStorage[groupId & differentColors].Length - 1;
        return ColorStorage[groupId % differentColors][lastIndex];
    }


    public Color GenerateRandomColor(float hueMin, float hueRange)
    {
        float saturation = Random.Range(0.75f, 1f);
        float brightness = Random.Range(0.75f, 1f);

        float hueMax = hueMin + hueRange;

        // If hue maximum is >1, handle wrapping around the hue circle and pick the random from two ranges
        float hue;
        if (hueMax > 1f)
        {
            float wrappedHueMax = hueMax - 1f;
            if (Random.value < (1f - hueMin) / (hueMax - hueMin))
            {
                // Choose hue from hueMin to 1
                hue = Random.Range(hueMin, 1f);
            }
            else
            {
                // Choose hue from 0 to wrappedHueMax
                hue = Random.Range(0f, wrappedHueMax);
            }
        }
        else
        {
            // Normal range between hueMin and hueMax
            hue = Random.Range(hueMin, hueMax);
        }

        return Color.HSVToRGB(hue, saturation, brightness);
    }


    ////Alternate method setting the material property block directly from pregenerated dictionary - seems slightly slower
    //rfi.Renderer.SetPropertyBlock(MaterialPropertyBlocks[(rfi.GroupId, colorIndex)]);



    //int colorIndex = Mathf.Min((int)(rfi.FadeTime * colorsPerPropagator), upperBoundary);
    //// Original code to get color with modulo
    //PropBlock.SetColor("_Color", ColorStorage[rfi.GroupId % differentColors][colorIndex]);

    //// Optimised but less readable code that uses bitwise AND (works when differentColors is a power of two - maybe?)
    //int groupIdMod = rfi.GroupId & (differentColors - 1);
    //PropBlock.SetColor("_Color", ColorStorage[groupIdMod][colorIndex]);

}