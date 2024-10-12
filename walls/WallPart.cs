// Script in the wall GameObjects, to adjust their material/color

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PropagatorManager;

public class WallPart : MonoBehaviour
{
    public float elapsedTime = 0.0f;
    private int currentMaterial = 0;
    private int newMaterial = 0;
    public WallMaterialStorage wms;   
    public int numberOfMaterials;     
    public bool fadeInProgress = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Material colour fade-in based on elapsed time
    void FixedUpdate()
    {
        if (fadeInProgress)
        {
            elapsedTime += Time.deltaTime;
            newMaterial = (int)(elapsedTime * 20);
            if (newMaterial >= numberOfMaterials)
            {
                this.GetComponent<Renderer>().sharedMaterial = wms.Materials[numberOfMaterials - 1];
                fadeInProgress = false;
                newMaterial = numberOfMaterials - 1;
            }
            if (newMaterial != currentMaterial)
            {
                this.GetComponent<Renderer>().sharedMaterial = wms.Materials[currentMaterial];
                currentMaterial = newMaterial;
            }
        }
    }

    public void Reset()
    {
        elapsedTime = 0.0f;
        currentMaterial = 0;
        newMaterial = 0;
        GetComponent<Renderer>().sharedMaterial = wms.Materials[0];
        fadeInProgress = true;
        transform.localScale = new Vector3(1, 16, 1);
    }
}