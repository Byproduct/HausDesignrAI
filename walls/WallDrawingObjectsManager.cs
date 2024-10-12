using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For now, just contains a debug speedup button
public class WallDrawingObjectsManager : MonoBehaviour
{
    public void RefreshBoundaries()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            WallDrawingObject wdo = child.GetComponent<WallDrawingObject>();
            if (wdo != null)
            {
                wdo.RefreshBoundaries();
            }
        }
    }
}
