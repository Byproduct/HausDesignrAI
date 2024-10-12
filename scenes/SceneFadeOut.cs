using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using TMPro;

public class SceneFadeOut : MonoBehaviour
{
    public static SceneFadeOut Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    private float fadeOutDuration = 1f;
    private bool isTransitioning = false;
    private string nextSceneName = "HausDesignr";
    public GameObject FadeOutObject;
    public Image fadeOutImage;

    public TextMeshProUGUI SpeedChoosingHeading;
    public TextMeshProUGUI SpeedChoosingElements;

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
            StartCoroutine(TransitionScene());
        }
    }

    private IEnumerator TransitionScene()
    {
        yield return StartCoroutine(FadeOut());
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
        SpeedChooser.Instance.LaunchNextScene();
    }
}