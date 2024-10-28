using UnityEngine;

public class CollisionSoundPlayer : MonoBehaviour
{
    // Cooldown and pitch settings
    private const float lightSoundRate = 0.01f;
    private const float mediumSoundRate = 0.02f;
    private const float heavySoundRate = 0.04f;
    private const float minPitch = 0.9f;
    private const float maxPitch = 1.1f;

    // One AudioSource per category
    public AudioSource lightAudioSource;
    public AudioSource mediumAudioSource;
    public AudioSource heavyAudioSource;

    // Arrays of AudioClips for each category
    public AudioClip[] lightClips;
    public AudioClip[] mediumClips;
    public AudioClip[] heavyClips;

    public float lightCooldown = 0;
    public float mediumCooldown = 0;
    public float heavyCooldown = 0;

    // Update audio cooldowns
    void FixedUpdate()
    {
        lightCooldown -= Time.deltaTime;
        mediumCooldown -= Time.deltaTime;
        heavyCooldown -= Time.deltaTime;
    }

    public void PlayHeavy(Vector3 position)
    {
        if (heavyCooldown <= 0)
        {
            heavyCooldown = heavySoundRate;
            PlayRandomSound(heavyAudioSource, heavyClips, position);
        }
    }

    public void PlayMedium(Vector3 position)
    {
        if (mediumCooldown <= 0)
        {
            mediumCooldown = mediumSoundRate;
            PlayRandomSound(mediumAudioSource, mediumClips, position);
        }
    }

    public void PlayLight(Vector3 position)
    {
        if (lightCooldown <= 0)
        {
            lightCooldown = lightSoundRate;
            PlayRandomSound(lightAudioSource, lightClips, position);
        }
    }

    /// Helper method to play a random sound from a given AudioClip array
    private void PlayRandomSound(AudioSource source, AudioClip[] clips, Vector3 position)
    {
        if (clips.Length > 0)
        {
            int randomIndex = Random.Range(0, clips.Length);
            float pitch = Random.Range(minPitch, maxPitch);

            source.transform.position = position;
            source.pitch = pitch;
            source.clip = clips[randomIndex];

            source.PlayOneShot(source.clip, 0.3f);
        }
    }
}