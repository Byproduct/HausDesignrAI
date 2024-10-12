using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;


public class End : MonoBehaviour
{
    public static End Instance { get; private set; }

    public GameObject Heading;
    public GameObject HeadingSecondLine;
    public bool textsComplete;
    public bool enterEnabled;
    public GameObject Camera;
    public GameObject FadeOutObject;
    public Image FadeOutImage;
    public AudioSource OutroMusic;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        textsComplete = false;
        enterEnabled = false;
        StartCoroutine(EndTexts());
        StartCoroutine(FinalFadeOut());
        StartCoroutine(EnableRestart());
    }

    IEnumerator EndTexts()
    {
        enterEnabled = true;
        Heading.GetComponent<TextMeshProUGUI>().text = "";
        yield return new WaitForSeconds(1f);
        Heading.GetComponent<TextMeshProUGUI>().text = "Thanks for watching!";
        yield return new WaitForSeconds(4f);
        Heading.GetComponent<TextMeshProUGUI>().text = "";
        yield return new WaitForSeconds(0.3f);
        Heading.GetComponent<TextMeshProUGUI>().text = "Haus Designr AI";
        yield return new WaitForSeconds(0.5f);
        HeadingSecondLine.SetActive(true);
        HeadingSecondLine.GetComponent<TextMeshProUGUI>().text = "is open for venture capital.";
        yield return new WaitForSeconds(6f);
        Heading.GetComponent<TextMeshProUGUI>().text = "";
        HeadingSecondLine.GetComponent<TextMeshProUGUI>().text = "";
        yield return new WaitForSeconds(0.3f);
        Heading.GetComponent<TextMeshProUGUI>().text = "ESC = exit";
        yield return new WaitForSeconds(0.5f);
        HeadingSecondLine.GetComponent<TextMeshProUGUI>().text = "enter = new house";
        textsComplete = true;
    }

    IEnumerator FinalFadeOut()
    {
        while (Camera.transform.position.x < (HouseSpawner.Instance.HouseNumber - 1) * 2500f)
        {
            yield return new WaitForSeconds(0.5f);
        }
        while (textsComplete == false)
        {
            yield return new WaitForSeconds(0.5f);
        }

        float elapsedFade = 0f;
        float totalFade = 5f;
        Color fadeColor = FadeOutImage.color;

        fadeColor.a = 0;
        FadeOutImage.color = fadeColor;
        FadeOutImage.enabled = true;
        FadeOutObject.SetActive(true);
        while (elapsedFade < totalFade)
        {
            elapsedFade += Time.deltaTime;
            fadeColor.a = Mathf.Lerp(0, 1, elapsedFade / totalFade);
            float musicVolume = Mathf.Lerp(1, 0, elapsedFade / totalFade);
            OutroMusic.volume = musicVolume;
            FadeOutImage.color = fadeColor;
            yield return null;
        }
        fadeColor.a = 1f;
    }

    // Enables restarting the scene by pressing enter. Esc = quit is available through the keyboard handler script.
    IEnumerator EnableRestart()
    {
        while (true)
        {
            if (Input.GetKey(KeyCode.Return))
            {
                Configuration.Speed = Configuration.SpeedType.Fast;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            yield return null;
        }
    }
}
