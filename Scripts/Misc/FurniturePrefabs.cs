// Just a list of furniture prefabs. Managed in Editor.

using System.Collections.Generic;
using UnityEngine;
public class FurniturePrefabs : MonoBehaviour
{
    public static FurniturePrefabs Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    public List<GameObject> FurniturePrefabsList;
}
