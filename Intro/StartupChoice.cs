using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartupChoice: MonoBehaviour
{
    public static StartupChoice Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    public GameObject IntroManager;
    public GameObject MusicPlayer;
    public GameObject SpeedChooserHeading;
    public GameObject SpeedChooserElements;
    public GameObject SliderParent;
    public Slider ProgressSlider;

    private AsyncOperation asyncLoad;



    void Start()
    {
        StartCoroutine(PreloadMainScene("HausDesignr"));
        StartCoroutine(ChooseDemoSpeed());
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

    // Choice between normal and fast demo at the start
    private IEnumerator ChooseDemoSpeed()
    {
        yield return new WaitForSeconds(0.5f);     //A small delay at launch to make sure audio starts in sync
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
