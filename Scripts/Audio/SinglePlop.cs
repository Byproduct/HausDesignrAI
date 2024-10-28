// A single plop sound for the trees

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SinglePlop : MonoBehaviour
{
    private AudioSource audioSource;
    private bool hasPlayed = false;
    private float elapsedTime = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if ((!hasPlayed) && (elapsedTime > 0.5f))
        {
            audioSource.pitch = Random.Range(0.5f, 1.5f);
            audioSource.Play();
            hasPlayed = true;
        }
    }
}
