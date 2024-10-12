using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;


// Destroy components that are no longer used
public class ComponentDestroyer : MonoBehaviour
{
    public GameObject CombinedWallMeshes;
    public GameObject WallDrawingObjectsParent;
    public GameObject CombinedPropagatorMeshesParent;
    public GameObject WallPartsParent;
    public GameObject SingleSquarePropagatorObjectPool;


    public static ComponentDestroyer Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void DestroyParentObjects(List<GameObject> gameObjects)
    {
        StartCoroutine(DestroyParentObjectsOverTime(gameObjects));
    }

    public void DestroyParentObject(GameObject obj)
    {
        List<GameObject> toDestroy = new List<GameObject> { obj };
        StartCoroutine(DestroyParentObjectsOverTime(toDestroy));
    }

    //When blocks are built and the initial fade to dark begins, this phase is easier on the CPU so start destroying leftover objects
    public void DestroyPhase1()
    {
        Destroy(GetComponent<CalculatePropagatorLaunchPositions>());
        Destroy(GetComponent<MeshOptimizer>());
        Destroy(GetComponent<PropagatorColorManager>());
        Destroy(GetComponent<PropagatorManager>());
        Destroy(GetComponent<PropagatorRowManager>());
        Destroy(GetComponent<WallChunkManager>());
        Destroy(GetComponent<WallManager>());
        Destroy(GetComponent<WallMeshCombiner>());

        List<GameObject> parentObjectsToDestroy = new List<GameObject>();
        parentObjectsToDestroy.Add(CombinedWallMeshes);
        parentObjectsToDestroy.Add(WallDrawingObjectsParent);
        parentObjectsToDestroy.Add(CombinedPropagatorMeshesParent);
        parentObjectsToDestroy.Add(WallPartsParent);
        parentObjectsToDestroy.Add(SingleSquarePropagatorObjectPool);
        StartCoroutine(DestroyParentObjectsOverTime(parentObjectsToDestroy));

    }
    IEnumerator DestroyParentObjectsOverTime(List<GameObject> objectsToDestroy)
    {
        Debug.Log($"Destroying objects that are no longer needed - {objectsToDestroy.Count} parent objects.");

        int batchSize = 150;
        int num_objects = 0;
        foreach (GameObject obj in objectsToDestroy)
        {
            while (obj.transform.childCount > 0)
            {
                int currentBatchSize = Mathf.Min(batchSize, obj.transform.childCount);
                int startIndex = obj.transform.childCount - 1;
                int endIndex = obj.transform.childCount - currentBatchSize;

                for (int i = startIndex; i >= endIndex; i--)
                {
                    Destroy(obj.transform.GetChild(i).gameObject);
                }

                num_objects += currentBatchSize;

                // Wait for 5 frames, to ensure the objects marked for deletion actually get deleted
                int framesToWait = 5;
                for (int frame = 0; frame < framesToWait; frame++)
                {
                    yield return new WaitForFixedUpdate();
                }
            }
            Debug.Log($"Completed destruction of {obj.name} - {num_objects} child objects destroyed so far.");
            Destroy(obj);
        }
        PropagatorManager.Instance.SingleSquarePropagatorObjectPool.Dispose();
        WallManager.Instance.WallPartPool.Dispose();
        Debug.Log($"Obsolete object destruction complete.");
    }
}
