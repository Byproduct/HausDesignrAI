// Applies a dissolve effect to an object, then destroys it
// The dissolving shader is in Assets/Gfx

using UnityEngine;

public class ObjectDissolver : MonoBehaviour
{
    private float elapsedTime = 0f;
    private float dissolvingTime = 3f;
    private Renderer r;

    private void Start()
    {
        Material dissolvingMaterial = Resources.Load<Material>("DissolvingMaterial");
        r = gameObject.GetComponent<Renderer>();
        r.material = dissolvingMaterial;
        r.material.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.1f));
    }

    private void Update()
    {
        if (elapsedTime < dissolvingTime)
        {
            elapsedTime += Time.deltaTime;
            r.material.SetFloat("_DissolvingAmount", elapsedTime / dissolvingTime);
        }
        else if (elapsedTime > dissolvingTime)
        {
            Destroy(gameObject);
        }
    }

    public void SetDissolvingTime(float time)
    {
        dissolvingTime = time;
    }
}