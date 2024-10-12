using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading;
using UnityEngine.SceneManagement;

public class SpeedChooser : MonoBehaviour
{
    public static SpeedChooser Instance { get; private set; }

    public GameObject IntroManager;
    public GameObject SpeedChooserHeading;
    public GameObject SpeedChooserElements;
    public GameObject SliderParent;
    public Slider ProgressSlider;

    private AsyncOperation asyncLoad;
    public GameObject MusicPlayer;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(PreloadMainScene("HausDesignr"));
        StartCoroutine(ChoiceDelay());
    }

    private void FixedUpdate()
    {
        if (Configuration.Speed == Configuration.SpeedType.Dev || Configuration.Speed == Configuration.SpeedType.Fast)
        {
            SceneFadeOut.Instance.StartSceneTransition();
        }
    }


    IEnumerator PreloadMainScene(string sceneName)
    {
        asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        yield return new WaitForSeconds(1f);

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
    }

    public void LaunchNextScene()
    {
        asyncLoad.allowSceneActivation = true;
    }

    private IEnumerator ChoiceDelay()
    {
        //A small delay at start to make sure audio starts in sync
        yield return new WaitForSeconds(0.5f);
        MusicPlayer.GetComponent<AudioLowPassFilter>().enabled = false;
        MusicPlayer.GetComponent<AudioSource>().Play();


        float duration = 4f;
        float elapsedTime = 0f;

        while (elapsedTime <= duration)
        {
            elapsedTime += Time.deltaTime;
            ProgressSlider.value = Mathf.Clamp01(elapsedTime / duration);
            yield return null;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Configuration.Speed = Configuration.SpeedType.Fast;
                SliderParent.SetActive(false);
                MusicPlayer.GetComponent<AudioSource>().Stop();
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                Configuration.Speed = Configuration.SpeedType.Dev;
                SliderParent.SetActive(false);
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                elapsedTime = duration;
            }
        }
        IntroManager.GetComponent<TextSlider>().enabled = true;
        Destroy(SpeedChooserHeading);
        Destroy(SpeedChooserElements);
        Destroy(this);
    }
}
