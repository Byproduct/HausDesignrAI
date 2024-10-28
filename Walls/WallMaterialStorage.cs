// Contains materials for the fade-in effect of appearing walls.
// The materials are generated once here, and each wall accesses this same list.

using System.Collections.Generic;
using UnityEngine;

public class WallMaterialStorage : MonoBehaviour
{
    public static WallMaterialStorage Instance { get; private set; }

    public int NumberOfMaterials = 20;
    public List<Material> Materials;

    void Awake()
    {
        Instance = this;

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
