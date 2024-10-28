// Script attached to the individual wall GameObjects, to control the color fade-in of each one

using UnityEngine;

public class WallColorFade : MonoBehaviour
{
    public bool FadeInProgress = false;
    public float ElapsedTime = 0.0f;

    private int currentMaterial = 0;
    private WallMaterialStorage materialStorage;
    private int newMaterial = 0;
    private int numberOfMaterials;
    private Renderer r;

    void Start()
    {
        r = GetComponent<Renderer>();
        materialStorage = WallMaterialStorage.Instance;
        numberOfMaterials = materialStorage.NumberOfMaterials;
    }

    void FixedUpdate()
    {
        if (FadeInProgress)
        {
            ElapsedTime += Time.deltaTime;
            newMaterial = (int)(ElapsedTime * 20);
            if (newMaterial >= numberOfMaterials)
            {
                r.sharedMaterial = materialStorage.Materials[numberOfMaterials - 1];
                FadeInProgress = false;
                newMaterial = numberOfMaterials - 1;
            }
            if (newMaterial != currentMaterial)
            {
                r.sharedMaterial = materialStorage.Materials[currentMaterial];
                currentMaterial = newMaterial;
            }
        }
    }

    public void Reset()
    {
        ElapsedTime = 0.0f;
        currentMaterial = 0;
        newMaterial = 0;
        materialStorage = WallMaterialStorage.Instance;
        GetComponent<Renderer>().sharedMaterial = materialStorage.Materials[0];
        FadeInProgress = true;
        transform.localScale = new Vector3(1, 16, 1);
    }
}