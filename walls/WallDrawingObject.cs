using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WallDrawingObject : MonoBehaviour
{
    private GameObject GameManager;
    private GameManager gm;
    public GameObject WallManager;
    private WallManager wm;

    private int xMin = 0;
    private int xMax;
    private int zMax;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private float speed = 79.0f;
    private float turnSpeed = 50.0f;
    private float currentAngle = 0f;
    private float targetAngle = 0f;
    private float cooldown = 0f;

    public int lastX = 0;
    public int lastZ = 0;

    void Start()
    {
        GameManager = GameObject.Find("Game manager");
        gm = GameManager.GetComponent<GameManager>();
        wm = GameManager.GetComponent<WallManager>();

        rb = GetComponent<Rigidbody>();
        currentAngle = UnityEngine.Random.Range(-45f, 45f);
        targetAngle = UnityEngine.Random.Range(-45f, 45f);
        cooldown = UnityEngine.Random.Range(0.5f, 3f);
        xMax = gm.WorldSizeX;
        zMax = gm.WorldSizeZ;
    }

    private void Update()
    {
    }

    void FixedUpdate()
    {
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
            wm.WallDrawingObjects.Remove(this.gameObject);
            this.gameObject.SetActive(false);
        }

        // If current and last Z coordinate are different, spawn a wall to all z and x positions between them (could move more than one position per frame in fast-forward mode)
        if (x > lastX)
        {
            int wallSpawningPosition = lastX + 1;
            while (wallSpawningPosition < (x + 1))
            {
                wm.SpawnWall(new Vector3Int(wallSpawningPosition, 8, lastZ));
                wallSpawningPosition++;
            }
            lastX = x;
        }
        if (x < lastX)
        {
            int wallSpawningPosition = lastX - 1;
            while (wallSpawningPosition > (x - 1))
            {
                wm.SpawnWall(new Vector3Int(wallSpawningPosition, 8, lastZ));
                wallSpawningPosition--;
            }
            lastX = x;
        }

        if (z > lastZ)
        {

            int wallSpawningPosition = lastZ + 1;
            while (wallSpawningPosition < (z + 1))
            {
                wm.SpawnWall(new Vector3Int(x, 8, wallSpawningPosition));
                wallSpawningPosition++;
            }

            lastZ = z;
        }

        // Whenever cooldown expires, randomise a new target angle and cooldown
        cooldown -= Time.deltaTime;
        if (cooldown < 0)
        {
            cooldown = UnityEngine.Random.Range(0.5f, 3f);
            targetAngle = UnityEngine.Random.Range(-45f, 45f);
        }
    }

    public void RefreshBoundaries()
    {
        xMax = gm.WorldSizeX;
        zMax = gm.WorldSizeZ;
    }

    public void SetSpeed(float spd)
    {
        this.speed = spd;
    }
}