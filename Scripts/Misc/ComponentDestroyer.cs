// Cleanup after the propagation phase, to free memory and simplify editor.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ComponentDestroyer : MonoBehaviour
{
    public static ComponentDestroyer Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    private const int batchSize = 100; // objects to destroy per frame

    public GameObject CombinedWallMeshes;
    public GameObject WallDrawingObjectsParent;
    public GameObject CombinedPropagatorMeshesParent;
    public GameObject WallPartsParent;
    public GameObject SingleSquarePropagatorObjectPool;

    // Destroy a list of GameObjects
    public void DestroyParentObjects(List<GameObject> gameObjects)
    {
        StartCoroutine(DestroyParentObjectsOverTime(gameObjects));
    }

    // Destroy a single object
    public void DestroyParentObject(GameObject obj)
    {
        List<GameObject> toDestroy = new List<GameObject> { obj };
        StartCoroutine(DestroyParentObjectsOverTime(toDestroy));
    }

    public void DestroyComponents()
    {
        Destroy(GetComponent<PropagatorLaunchPositionsCalculator>());
        Destroy(GetComponent<MeshSimplifier>());
        Destroy(GetComponent<PropagatorColorManager>());
        Destroy(GetComponent<PropagatorManager>());
        Destroy(GetComponent<PropagatorRowManager>());
        Destroy(GetComponent<WallChunkManager>());
        Destroy(GetComponent<WallManager>());
        Destroy(GetComponent<WallMeshCombiner>());
        MainManager.Instance.terrainGrid = null;

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
        Util.WriteLog($"Destroying objects that are no longer needed - {objectsToDestroy.Count} parent objects.");

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

                // Wait a few frames, to ensure the objects marked for deletion have actually gotten deleted.
                // They should always be destroyed by the end of current frame, but here there is no harm in waiting to be sure.
                int framesToWait = 3;
                for (int frame = 0; frame < framesToWait; frame++)
                {
                    yield return new WaitForFixedUpdate();
                }
            }
            Util.WriteLog($"Completed destruction of {obj.name} - {num_objects} child objects destroyed so far.");
            Destroy(obj);
        }
        PropagatorManager.Instance.PropagatorGameObjectPool.Dispose();
        WallManager.Instance.WallPartPool.Dispose();
        Util.WriteLog($"Obsolete object destruction complete.");
    }
}
