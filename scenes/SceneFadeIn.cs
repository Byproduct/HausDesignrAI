using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    public static SceneFadeIn Instance { get; private set; }

    private float fadeInDuration = 1f;
    private bool isTransitioning = false;
    public Image FadeInImage;

    void Awake()
    {
        Instance = this;
        if (!isTransitioning)
        {
            isTransitioning = true;
            if (Configuration.Speed == Configuration.SpeedType.Dev)
            {
                fadeInDuration = 0.1f;
            }
            StartCoroutine(FadeIn());
        }
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
        gameObject.SetActive(false);
        Destroy(gameObject.GetComponent<SceneFadeIn>());
    }
}