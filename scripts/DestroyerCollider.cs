using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An object with this script attached will destroy any other object it collides with
public class DestroyerCollider: MonoBehaviour
{
    void OnTriggerStay(Collider other)
    {
        // Clone the other object for dissolving effect.
        // Destroy original immediately so it doesn't collide anymore and isn't present in any lists
        GameObject clone = Instantiate(other.gameObject);  
        Destroy(other.gameObject);
        Collider collider = clone.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        Rigidbody rigidbody = clone.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            Destroy(rigidbody);
        }
        clone.AddComponent<ObjectDissolver>();
        clone.isStatic = true;
    }
}
