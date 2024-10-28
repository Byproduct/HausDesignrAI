// Fade-out after the intro scene

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SceneFadeOut : MonoBehaviour
{
    public static SceneFadeOut Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    private const string nextSceneName = "HausDesignr";

    public Image fadeOutImage;
    public GameObject FadeOutObject;
    public TextMeshProUGUI SpeedChoosingHeading;
    public TextMeshProUGUI SpeedChoosingElements;

    private float fadeOutDuration = 1f;
    private bool isTransitioning = false;

    public void StartSceneTransition()
    {
        if (!isTransitioning)
        {
            isTransitioning = true;
            FadeOutObject.SetActive(true);
            if (Configuration.Speed == Configuration.SpeedType.Dev)
            {
                fadeOutDuration = 0.1f;
            }
            StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut()
    {
        Color fadeColor = fadeOutImage.color;
        Color textFadeColor = new Color(0.78f, 1f, 1f);
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeColor.a = Mathf.Lerp(0, 1, elapsedTime / fadeOutDuration);
            fadeOutImage.color = fadeColor;

            if (SpeedChoosingHeading != null)
            {
                textFadeColor = new Color(0.78f, 1f, 1f, Mathf.Lerp(1, 0, elapsedTime / fadeOutDuration));
                SpeedChoosingHeading.color = textFadeColor;
                SpeedChoosingElements.color = textFadeColor;
            }
            yield return null;
        }
        fadeColor.a = 1f;
        fadeOutImage.color = fadeColor;
        StartupChoice.Instance.LaunchNextScene();
    }
}