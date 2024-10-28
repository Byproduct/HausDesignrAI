// Fade in a groud material when switching from "imagining shapes" to "casting blocks"

using UnityEngine;

public class GroundFadeIn : MonoBehaviour
{
    private const float fadeDuration = 2f;

    public float elapsedTime = 0f;

    private Color initialColor;
    private bool isFading = false;
    private Renderer objectRenderer;

    void Start()
    {
        elapsedTime = 0f;
        objectRenderer = GetComponent<Renderer>();
        objectRenderer.enabled = false;
        initialColor = objectRenderer.material.color;
        initialColor.a = 0;
        objectRenderer.material.color = initialColor;
        SetMaterialTransparent();
        isFading = true;
    }

    void Update()
    {
        if (isFading)
        {
            elapsedTime += Time.deltaTime;

            // Avoid flashing glitch from initial settings
            if (elapsedTime > 0.1f)
            {
                objectRenderer.enabled = true;
            }

            float alphaValue = Mathf.Clamp01(elapsedTime / fadeDuration);
            Color newColor = initialColor;
            newColor.a = alphaValue;
            objectRenderer.material.color = newColor;

            // If the fade-in is complete, set to opaque and disable the script
            if (elapsedTime >= fadeDuration)
            {
                SetMaterialOpaque();
                isFading = false;
                enabled = false;
            }
        }
    }

    private void SetMaterialTransparent()
    {
        objectRenderer.material.SetFloat("_Mode", 3); 
        objectRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        objectRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        objectRenderer.material.SetInt("_ZWrite", 0);
        objectRenderer.material.DisableKeyword("_ALPHATEST_ON");
        objectRenderer.material.EnableKeyword("_ALPHABLEND_ON");
        objectRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        objectRenderer.material.renderQueue = 3000;
    }

    private void SetMaterialOpaque()
    {
        objectRenderer.material.SetFloat("_Mode", 0); 
        objectRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        objectRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        objectRenderer.material.SetInt("_ZWrite", 1);
        objectRenderer.material.DisableKeyword("_ALPHATEST_ON");
        objectRenderer.material.DisableKeyword("_ALPHABLEND_ON");
        objectRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        objectRenderer.material.renderQueue = -1;
        Color opaqueColor = objectRenderer.material.color;
        opaqueColor.a = 1;
        objectRenderer.material.color = opaqueColor;
    }
}
