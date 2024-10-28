// A script attached to any physics objects that make collision sounds.

using UnityEngine;

public class CollisionSounds : MonoBehaviour
{
    private GameObject CollisionSoundPlayer;

    private const int lightCollisionThreshold = 20;
    private const int mediumCollisionThreshold = 200;
    private const int heavyCollisionThreshold = 200;

    public void Start()
    {
        CollisionSoundPlayer = GameObject.Find("CollisionSoundPlayer");
    }
    void OnCollisionEnter(Collision collision)
    {
        if (CollisionSoundPlayer != null)
        {
            if ((collision.relativeVelocity.magnitude > lightCollisionThreshold) && (collision.relativeVelocity.magnitude < mediumCollisionThreshold))
            {
                CollisionSoundPlayer.GetComponent<CollisionSoundPlayer>().PlayLight(transform.position);
            }
            else if ((collision.relativeVelocity.magnitude >= mediumCollisionThreshold) && (collision.relativeVelocity.magnitude < heavyCollisionThreshold))
            {
                CollisionSoundPlayer.GetComponent<CollisionSoundPlayer>().PlayMedium(transform.position);
            }
            else if (collision.relativeVelocity.magnitude >= heavyCollisionThreshold)
            {
                CollisionSoundPlayer.GetComponent<CollisionSoundPlayer>().PlayHeavy(transform.position);
            }
        }
    }
}
