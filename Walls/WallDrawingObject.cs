// This script is attached to each wall drawing object. The objects spawn walls as they move.

using UnityEngine;

public class WallDrawingObject : MonoBehaviour
{
    public int LastX = 0;
    public int LastZ = 0;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private float speed = 79.0f;
    private float turnSpeed = 50.0f;
    private float currentAngle = 0f;
    private float targetAngle = 0f;
    private float cooldown = 0f;

    private int xMin = 0;
    private int xMax;
    private int zMax;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentAngle = Random.Range(-45f, 45f);
        targetAngle = Random.Range(-45f, 45f);
        cooldown = Random.Range(0.5f, 3f);
        xMax = MainManager.Instance.WorldSizeX;
        zMax = MainManager.Instance.WorldSizeZ;
    }

    void FixedUpdate()
    {
        // Update the object's movement and constrain it to within outer walls
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime / 180.0f);
        Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
        moveDirection = rotation * Vector3.forward;
        rb.velocity = moveDirection * speed;
        if (transform.position.x < xMin)
        {
            transform.position = new Vector3(xMin, transform.position.y, transform.position.z);
        }
        if (transform.position.x > xMax)
        {
            transform.position = new Vector3(xMax, transform.position.y, transform.position.z);
        }
        if (transform.position.z > zMax)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, zMax);
        }

        int z = (int)(transform.position.z);
        int x = Mathf.RoundToInt(transform.position.x);

        if (z > zMax - 1)
        {
            rb.velocity = Vector3.zero;
            WallManager.Instance.WallDrawingObjects.Remove(this.gameObject);
            gameObject.SetActive(false);
        }

        // If current and last integer Z coordinate are different, spawn a wall to all z and x positions between them
        // (could move more than one position per frame in fast-forward mode)
        if (x > LastX)
        {
            int wallSpawningPosition = LastX + 1;
            while (wallSpawningPosition < (x + 1))
            {
                WallManager.Instance.SpawnWall(new Vector3Int(wallSpawningPosition, 8, LastZ));
                wallSpawningPosition++;
            }
            LastX = x;
        }
        if (x < LastX)
        {
            int wallSpawningPosition = LastX - 1;
            while (wallSpawningPosition > (x - 1))
            {
                WallManager.Instance.SpawnWall(new Vector3Int(wallSpawningPosition, 8, LastZ));
                wallSpawningPosition--;
            }
            LastX = x;
        }

        if (z > LastZ)
        {
            int wallSpawningPosition = LastZ + 1;
            while (wallSpawningPosition < (z + 1))
            {
                WallManager.Instance.SpawnWall(new Vector3Int(x, 8, wallSpawningPosition));
                wallSpawningPosition++;
            }
            LastZ = z;
        }

        // Whenever cooldown expires, randomise a new target angle and cooldown
        cooldown -= Time.deltaTime;
        if (cooldown < 0)
        {
            cooldown = UnityEngine.Random.Range(0.5f, 3f);
            targetAngle = UnityEngine.Random.Range(-45f, 45f);
        }
    }

    public void SetSpeed(float spd)
    {
        speed = spd;
    }
}