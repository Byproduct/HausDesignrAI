using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class DisplayHousesManager : MonoBehaviour
{
    public static DisplayHousesManager Instance { get; private set; }

    public GameObject Camera;
    public GameObject Heading;
    public GameObject HeadingSecondLine;
    public Image FadeOutImage;
    public GameObject FadeOutObject;
    public AudioSource OutroMusic;

    private bool textsComplete;
    private bool enterEnabled;

    void Awake()
    {
        Instance = this;
    }

    public void Initiate()
    {
        StartCoroutine(EnableRestart());
        StartCoroutine(EndTexts());
        StartCoroutine(FinalFadeOut());
    }

    // Enables restarting the scene by pressing enter. Esc = quit is available through the global keyboard handler script.
    // This is intentionally available a little earlier than the text is shown.
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

    IEnumerator EndTexts()
    {
        enterEnabled = true;
        HeadingSecondLine.SetActive(true);
        ClearHeadings();
        yield return new WaitForSeconds(1f);
        Heading.GetComponent<TextMeshProUGUI>().text = "Thanks for watching!";
        yield return new WaitForSeconds(4f);
        ClearHeadings();
        yield return new WaitForSeconds(0.3f);
        Heading.GetComponent<TextMeshProUGUI>().text = "Haus Designr AI";
        yield return new WaitForSeconds(0.5f);
        HeadingSecondLine.GetComponent<TextMeshProUGUI>().text = "is open for venture capital.";
        yield return new WaitForSeconds(6f);
        ClearHeadings();
        yield return new WaitForSeconds(0.3f);
        Heading.GetComponent<TextMeshProUGUI>().text = "ESC = exit";
        yield return new WaitForSeconds(0.5f);
        HeadingSecondLine.GetComponent<TextMeshProUGUI>().text = "enter = new house";
        textsComplete = true;
    }

    public void ClearHeadings()
    {
        Heading.GetComponent<TextMeshProUGUI>().text = "";
        HeadingSecondLine.GetComponent<TextMeshProUGUI>().text = "";
    }

    // Initiate fade-out when the camera passes the last house
    IEnumerator FinalFadeOut()
    {
        while (Camera.transform.position.x < (HouseSpawner.Instance.CurrentHouseNumber - 1) * 2500f)
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
}
