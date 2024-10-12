// Contains materials for the fade-in effect of appearing walls. When a wall gameobject is activated, this object is passed as a parameter to enable quick material fetching.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMaterialStorage : MonoBehaviour
{
    public int NumberOfMaterials = 20;
    public List<Material> Materials;
    // Start is called before the first frame update
    void Awake()
    {
        Materials = new List<Material>();
        Color startColor = Color.red;
        Color endColor = Color.blue;
        Shader shader = Shader.Find("Standard");

        for (int i = 0; i < NumberOfMaterials; i++)
        {
            Material newMaterial = new Material(shader);

            float t = (float)i / NumberOfMaterials;
            newMaterial.color = Color.Lerp(startColor, endColor, t);
            Materials.Add(newMaterial);
        }
    }
}
