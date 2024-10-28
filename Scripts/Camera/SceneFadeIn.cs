// Fade-in after the intro scene

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    public static SceneFadeIn Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    public Image FadeInImage;
    public GameObject FadeInObject;

    private float fadeInDuration = 1f;


    void Start()
    {
        if (Configuration.Speed == Configuration.SpeedType.Dev)
        {
            fadeInDuration = 0.1f;
        }
        StartCoroutine(FadeIn());
    }
    private IEnumerator FadeIn()
    {
        float startTime = Time.time;
        Color fadeColor = FadeInImage.color;
        while (Time.time < startTime + fadeInDuration)
        {
            float elapsed = Time.time - startTime;
            fadeColor.a = Mathf.Lerp(1, 0, elapsed / fadeInDuration);
            FadeInImage.color = fadeColor;
            yield return null;
        }
        fadeColor.a = 0f;
        Destroy(gameObject.GetComponent<SceneFadeIn>());
        Destroy(FadeInObject);
    }
}