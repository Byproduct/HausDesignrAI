using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Creates a completely new final object (with simpler but visually identical mesh) to replace the object used while propagating.
public class MeshSimplifier : MonoBehaviour
{
    public static MeshSimplifier Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    private const int minimumObjectVertices = 100;   // objects with fever verts will be discarded

    public GameObject ParentObject;

    private Stopwatch stopwatch = new Stopwatch();
    private HashSet<Vector2Int> uniquePoints; // hashset to keep track of unique points in the mesh so that duplicates won't be added.


    public void SpawnNewObject(short objectIndex)
    {
        stopwatch.Restart();
        uniquePoints = new HashSet<Vector2Int>();
        int height = PropagatorManager.Instance.BlockHeights[objectIndex];
        GameObject go = CreateGameObjectFromArray(objectIndex, height);
        if (go != null)
        {
            go.name = objectIndex.ToString();
            BuildingBlock bb = new BuildingBlock(go, objectIndex, 1);
            BuildingBlocksManager.Instance.BuildingBlocks.Add(objectIndex, bb);
            go.transform.SetParent(ParentObject.transform);
            stopwatch.Stop();
            Util.WriteVerboseLog($"Spawned object #{objectIndex} in {stopwatch.ElapsedMilliseconds} ms.");
        }
    }

    public GameObject CreateGameObjectFromArray(short objectNumber, int height)
    {     
        GameObject gameObject = new GameObject();
        gameObject.name = objectNumber.ToString();
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Standard"));
        meshRenderer.material.color = PropagatorColorManager.Instance.GetFinalColor(objectNumber);
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        List<Vector2Int> contourPoints = TraceContour(GetComponent<MainManager>().terrainGrid, objectNumber);

        Mesh topMesh = CreateHorizontalMeshFromContourPoints(contourPoints, height, false);

        Mesh bottomMesh = CreateHorizontalMeshFromContourPoints(contourPoints, 0, true);

        Mesh sideMesh = CreateSideMeshFromContourPoints(contourPoints, height);

        List<Mesh> meshestToCombine = new List<Mesh> { topMesh, bottomMesh, sideMesh };
        Mesh mesh = CombineMeshes(meshestToCombine);

        if (mesh.vertexCount < minimumObjectVertices)
        {
            gameObject.name += " (discarded small object)";
            gameObject.transform.SetParent(ParentObject.transform);
            if (Configuration.ConsoleLogging)
            {
                Util.WriteVerboseLog($"Object #{objectNumber} too small with {mesh.vertexCount} verts - not creating GameObject.");
            }
            return null;
        }

        Vector3 centroid = CenterMesh(mesh); // centers the mesh and returns the centroid so that the object can be moved accordingly   
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssignUVs(mesh);
        gameObject.transform.position += transform.TransformVector(centroid);
        meshFilter.mesh = mesh;

        return gameObject;
    }

    /// Vertices in unity meshes are stored in clockwise order.
    /// This method iterates through rows (z-coordinates) in the array and gets the leftmost and rightmost point of each row.
    /// The leftmost point is added to the contourPoints list directly, while the rightmost point is added to a separate list which is first completed, then reversed and appended to contourPoints.
    /// This creates a list of contour points of the object in clockwise order.
    public List<Vector2Int> TraceContour(short[,] array, int objectNumber)
    {
        List<Vector2Int> contourPoints = new List<Vector2Int>();
        List<Vector2Int> rightSideContourPoints = new List<Vector2Int>();

        bool foundPoint = false;

        for (int z = 0; z < array.GetLength(1); z++)
        {
            bool foundPointInRow = false;
            for (int x = 0; x < array.GetLength(0); x++)
            {
                foundPointInRow = false;
                if (array[x, z] != objectNumber)
                {
                    continue;
                }
                else
                {
                    foundPoint = true;
                    foundPointInRow = true;
                    AddToList(contourPoints, new Vector2Int(x, z));
                    AddToList(contourPoints, new Vector2Int(x, z + 1)); // An extra point to create the appearance with right angles - to-do: add edge cases to start and end
                    while (array[x, z] == objectNumber)
                    {
                        x++;
                    }
                    AddToList(rightSideContourPoints, new Vector2Int(x - 1, z));
                    AddToList(rightSideContourPoints, new Vector2Int(x - 1, z + 1)); // An extra point to create the appearance with right angles - to-do: add edge cases to start and end
                    break;
                }
            }
            // If the end of the row was reached without finding a point, and a point had been found previously, 
            // it means that this shape has completed and no points will be found in any following rows.
            if (foundPoint == true && foundPointInRow == false)
            {
                break;
            }
        }
        rightSideContourPoints.Reverse();
        contourPoints.AddRange(rightSideContourPoints);
        contourPoints = OptimizeSideContourPoints(contourPoints);
        return contourPoints;
    }

    /// With straight lines along the z axis in the object's contour, optimise the list of points by preserving only the start and end points of the lines
    /// Except if the line is too long (more than 15 points), then create intermediate points to help with mesh triangulation.
    public List<Vector2Int> OptimizeSideContourPoints(List<Vector2Int> sideContourPoints)
    {
        // Create a dictionary to map x-values to a list of y-values
        Dictionary<int, List<int>> xToYValues = new Dictionary<int, List<int>>();
        foreach (Vector2Int vec in sideContourPoints)
        {
            if (!xToYValues.ContainsKey(vec.x))
            {
                xToYValues[vec.x] = new List<int>();
            }
            xToYValues[vec.x].Add(vec.y);
        }

        // HashSet to keep track of points to preserve
        HashSet<Vector2Int> pointsToKeep = new HashSet<Vector2Int>();

        // Process each vertical line (constant x)
        foreach (int x in xToYValues.Keys)
        {
            List<int> yValues = xToYValues[x];
            yValues.Sort();
            int startIdx = 0;
            while (startIdx < yValues.Count)
            {
                int startY = yValues[startIdx];
                int endIdx = startIdx;

                // Find the end of the consecutive sequence
                while (endIdx + 1 < yValues.Count && yValues[endIdx + 1] == yValues[endIdx] + 1)
                {
                    endIdx++;
                }
                int endY = yValues[endIdx];
                int length = endY - startY + 1; // Length of the straight line

                if (length <= 15)
                {
                    // Keep only the endpoints for short lines
                    pointsToKeep.Add(new Vector2Int(x, startY));
                    if (startY != endY)
                    {
                        pointsToKeep.Add(new Vector2Int(x, endY));
                    }
                }
                else
                {
                    // Preserve points every 20 units for long lines
                    for (int y = startY; y <= endY; y += 20)
                    {
                        pointsToKeep.Add(new Vector2Int(x, y));
                    }
                    // Ensure the last point is included
                    if ((endY - startY) % 20 != 0)
                    {
                        pointsToKeep.Add(new Vector2Int(x, endY));
                    }
                }
                // Move to the next sequence
                startIdx = endIdx + 1;
            }
        }

        // Build the optimized contour points list
        List<Vector2Int> optimisedContourPoints = new List<Vector2Int>();
        foreach (Vector2Int vec in sideContourPoints)
        {
            if (pointsToKeep.Contains(vec))
            {
                optimisedContourPoints.Add(vec);
            }
        }

        //        Debug.Log($"{stopwatch.ElapsedMilliseconds} ms - contour point list optimised. Points reduced to {optimisedContourPoints.Count}.");

        return optimisedContourPoints;
    }


    /// Add a xz point to the list of points in a contour, but don't create duplicates if the point already exists.
    private void AddToList(List<Vector2Int> contourPoints, Vector2Int point)
    {
        if (uniquePoints.Contains(point))
        {
            return;
        }
        else
        {
            contourPoints.Add(point);
            uniquePoints.Add(point);
        }
    }

    /// Triangulate a horizontal mesh by connecting the contour points to extra points generated along the middle of the mesh.
    public Mesh CreateHorizontalMeshFromContourPoints(List<Vector2Int> contourPoints, int height, bool reverse)
    {
        // Create XZ contour points to avoid naming confusion later. (Vector2 contains variables x and y, better to use x and z).
        List<Vector2XZint> contourPointsXZ = new List<Vector2XZint>();
        foreach (Vector2Int point in contourPoints)
        {
            contourPointsXZ.Add(new Vector2XZint(point.x, point.y));
        }

        Mesh mesh = new Mesh();

        // Step 1: Calculate min and max z-values
        float minZ = contourPointsXZ.Min(p => p.z);
        float maxZ = contourPointsXZ.Max(p => p.z);

        // Step 2: Generate z-values every 20 units along the z-axis
        List<float> zValues = new List<float>();
        float totalZDistance = maxZ - minZ;
        float interval = 20f;
        int steps = Mathf.CeilToInt(totalZDistance / interval);
        for (int i = 1; i <= steps; i++)
        {
            float z = minZ + i * interval;
            if (z > maxZ - interval)
            {
                z = maxZ - interval;
            }
            zValues.Add(z);
        }

        // Step 3: Compute centroid
        var contour2DPoints = contourPointsXZ.Select(p => new Vector2XZ(p.x, p.z)).ToList();
        Vector2XZ centroid = new Vector2XZ(
            contour2DPoints.Average(p => p.x),
            contour2DPoints.Average(p => p.z)
        );

        // Step 4: For each z-value, create a central point by computing the average x-coordinate
        List<Vector2XZ> centralPoints = new List<Vector2XZ>();
        foreach (float z in zValues)
        {
            // Find all x-intersections of the polygon with the horizontal line at z
            List<float> xIntersections = new List<float>();
            int n = contour2DPoints.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2XZ a = contour2DPoints[i];
                Vector2XZ b = contour2DPoints[(i + 1) % n];

                // Check if the horizontal line at z intersects with the edge (a, b)
                if ((a.z <= z && b.z >= z) || (b.z <= z && a.z >= z))
                {
                    if (Mathf.Abs(a.z - b.z) < 0.0001f)
                    {
                        // Horizontal edge, skip to avoid division by zero
                        continue;
                    }
                    float t = (z - a.z) / (b.z - a.z);
                    float x = a.x + t * (b.x - a.x);
                    xIntersections.Add(x);
                }
            }

            if (xIntersections.Count >= 2)
            {
                // Sort the intersections and compute the average x
                xIntersections.Sort();
                float averageX = xIntersections.Average();
                Vector2XZ centerPoint = new Vector2XZ(averageX, z);
                centralPoints.Add(centerPoint);
            }
            else
            {
                // If no intersections found, try using the centroid's x-coordinate
                Vector2XZ centerPoint = new Vector2XZ(centroid.x, z);
                if (IsPointInsidePolygon(centerPoint, contour2DPoints))
                {
                    centralPoints.Add(centerPoint);
                }
            }
        }

        if (centralPoints.Count == 0)
        {
            Util.WriteVerboseLog("Central points list for this block is empty. Adding centroid as central point.");
            centralPoints.Add(centroid);
        }

        // Step 5: Create vertices array with central points and contour points
        int vertexCount = centralPoints.Count + contourPoints.Count;
        Vector3[] vertices = new Vector3[vertexCount];

        // Add central points
        for (int i = 0; i < centralPoints.Count; i++)
        {
            vertices[i] = new Vector3(centralPoints[i].x, height, centralPoints[i].z);
        }

        // Add contour points
        for (int i = 0; i < contourPointsXZ.Count; i++)
        {
            vertices[centralPoints.Count + i] = new Vector3(contourPointsXZ[i].x, height, contourPointsXZ[i].z);
        }
        mesh.vertices = vertices;

        // Step 6: Map each contour point to the nearest central point
        List<int> contourPointToCentralPoint = new List<int>();
        for (int i = 0; i < contourPointsXZ.Count; i++)
        {
            Vector2XZ contourPoint = contour2DPoints[i];
            float minDist = float.MaxValue;
            int closestCentralPointIndex = -1;

            for (int j = 0; j < centralPoints.Count; j++)
            {
                // mapping based on z-distance only - seems to be better for triangulation
                float dist = Mathf.Abs(contourPoint.z - centralPoints[j].z);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestCentralPointIndex = j;
                }
            }
            contourPointToCentralPoint.Add(closestCentralPointIndex);
        }

        // Step 7: Create triangles
        // Reverse bool refers to reverse winding order (mesh viewed from below vs above)
        List<int> triangles = new List<int>();

        for (int i = 0; i < contourPointsXZ.Count; i++)
        {
            int nextI = (i + 1) % contourPointsXZ.Count;

            int contourVertexIndex1 = centralPoints.Count + i; // Index of the current contour point in vertices array
            int contourVertexIndex2 = centralPoints.Count + nextI; // Index of the next contour point

            int centralIndex1 = contourPointToCentralPoint[i]; // Index of central point closest to current contour point
            int centralIndex2 = contourPointToCentralPoint[nextI]; // Index of central point closest to next contour point

            // If both contour points share the same central point,
            // create triangle between central point and contour edge
            if (centralIndex1 == centralIndex2)
            {
                if (!reverse)
                {
                    triangles.Add(centralIndex1);
                    triangles.Add(contourVertexIndex1);
                    triangles.Add(contourVertexIndex2);
                }
                else
                {
                    triangles.Add(centralIndex1);
                    triangles.Add(contourVertexIndex2);
                    triangles.Add(contourVertexIndex1);
                }
            }
            else
            {
                // If the contour points are connected to different central points,
                // create quad between the central points and contour points
                if (!reverse)
                {
                    triangles.Add(centralIndex1);
                    triangles.Add(contourVertexIndex1);
                    triangles.Add(centralIndex2);

                    triangles.Add(centralIndex2);
                    triangles.Add(contourVertexIndex1);
                    triangles.Add(contourVertexIndex2);
                }
                else
                {
                    triangles.Add(centralIndex1);
                    triangles.Add(centralIndex2);
                    triangles.Add(contourVertexIndex1);

                    triangles.Add(centralIndex2);
                    triangles.Add(contourVertexIndex2);
                    triangles.Add(contourVertexIndex1);
                }
            }
        }

        // Also create triangles between central points (missing in some edge cases)
        for (int i = 0; i < centralPoints.Count - 2; i++)
        {
            int index1 = i;
            int index2 = i + 1;
            int index3 = i + 2;

            if (!reverse)
            {
                triangles.Add(index1);
                triangles.Add(index2);
                triangles.Add(index3);
            }
            else
            {
                // Reverse winding order
                triangles.Add(index1);
                triangles.Add(index3);
                triangles.Add(index2);
            }
        }

        mesh.triangles = triangles.ToArray();

        // Step 8: Recalculate normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }


    // Side meshes require custom normal handling because they're made of right angles and calculating normals doesn't work correctly by default.
    // To-do: fix a small x-shift when the mesh is created.
    public Mesh CreateSideMeshFromContourPoints(List<Vector2Int> contourPoints, int height)
    {
        Mesh mesh = new Mesh();

        int numSides = contourPoints.Count;
        Vector3[] sideVertices = new Vector3[numSides * 4];
        Vector3[] normals = new Vector3[numSides * 4];
        int[] sideTriangles = new int[numSides * 6];

        for (int i = 0; i < numSides; i++)
        {
            int vertexIndex = i * 4;
            int triangleIndex = i * 6;

            // Get current and next contour point, wrapping around at the end
            Vector2Int currentPoint = contourPoints[i];
            Vector2Int nextPoint = contourPoints[(i + 1) % numSides];

            // Define the four vertices of the quad
            Vector3 v0 = new Vector3(currentPoint.x, 0, currentPoint.y); // Bottom current
            Vector3 v1 = new Vector3(currentPoint.x, height, currentPoint.y); // Top current
            Vector3 v2 = new Vector3(nextPoint.x, 0, nextPoint.y); // Bottom next
            Vector3 v3 = new Vector3(nextPoint.x, height, nextPoint.y); // Top next

            // Assign vertices to the array
            sideVertices[vertexIndex + 0] = v0;
            sideVertices[vertexIndex + 1] = v1;
            sideVertices[vertexIndex + 2] = v2;
            sideVertices[vertexIndex + 3] = v3;

            // Define the two triangles of the quad
            sideTriangles[triangleIndex + 0] = vertexIndex + 0;
            sideTriangles[triangleIndex + 1] = vertexIndex + 2;
            sideTriangles[triangleIndex + 2] = vertexIndex + 1;

            sideTriangles[triangleIndex + 3] = vertexIndex + 1;
            sideTriangles[triangleIndex + 4] = vertexIndex + 2;
            sideTriangles[triangleIndex + 5] = vertexIndex + 3;

            // Calculate the normal for the face
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 normal = Vector3.Cross(edge1, edge2).normalized;

            // Assign the normal to all four vertices of the quad
            normals[vertexIndex + 0] = normal;
            normals[vertexIndex + 1] = normal;
            normals[vertexIndex + 2] = normal;
            normals[vertexIndex + 3] = normal;
        }

        // Assign the vertices, triangles, and normals to the mesh
        mesh.vertices = sideVertices;
        mesh.triangles = sideTriangles;
        mesh.normals = normals;

        // Recalculate bounds (normals are already set)
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh CombineMeshes(List<Mesh> meshesToCombine)
    {
        CombineInstance[] combine = new CombineInstance[meshesToCombine.Count];

        for (int i = 0; i < meshesToCombine.Count; i++)
        {
            combine[i].mesh = meshesToCombine[i];
            combine[i].transform = Matrix4x4.identity;
        }
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);

        return combinedMesh;
    }

    public static Vector3 CenterMesh(Mesh mesh)
    {
        if (mesh == null) return Vector3.zero;

        // Get the vertices of the mesh
        Vector3[] vertices = mesh.vertices;

        // Calculate the centroid of the mesh
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 vertex in vertices)
        {
            centroid += vertex;
        }
        centroid /= vertices.Length;

        // Offset the vertices by the centroid
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= centroid;
        }

        // Apply the modified vertices back to the mesh
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();  // Optionally recalculate normals

        return centroid;
    }

    public struct Vector2XZ
    {
        public float x;
        public float z;

        public Vector2XZ(float x, float z)
        {
            this.x = x;
            this.z = z;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, z);
        }

        public Vector3 ToVector3(float y = 0)
        {
            return new Vector3(x, y, z);
        }
    }

    public struct Vector2XZint
    {
        public int x;
        public int z;

        public Vector2XZint(int x, int z)
        {
            this.x = x;
            this.z = z;
        }
    }




    // Helper function to determine if a point is inside a polygon
    private bool IsPointInsidePolygon(Vector2XZ point, List<Vector2XZ> polygon)
    {
        int n = polygon.Count;
        bool inside = false;
        float edgePadding = 10f; // Adjust this value as needed

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2XZ a = polygon[i];
            Vector2XZ b = polygon[j];

            // Check if point is close to the edge
            float distanceToEdge = DistancePointToSegment(point, a, b);
            if (distanceToEdge < edgePadding)
            {
                return false; // Considered outside if too close to an edge
            }

            if (((a.z > point.z) != (b.z > point.z)) &&
                (point.x < (b.x - a.x) * (point.z - a.z) / ((b.z - a.z) + 0.00001f) + a.x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    // Helper function to calculate the distance from a point to a line segment
    private float DistancePointToSegment(Vector2XZ p_xz, Vector2XZ a_xz, Vector2XZ b_xz)
    {
        Vector2 p = new Vector2(p_xz.x, p_xz.z);
        Vector2 a = new Vector2(a_xz.x, b_xz.z);
        Vector2 b = new Vector2(a_xz.x, b_xz.z);

        Vector2 ap = p - a;
        Vector2 ab = b - a;
        float ab2 = Vector2.Dot(ab, ab);
        float ap_ab = Vector2.Dot(ap, ab);
        float t = ap_ab / ab2;
        t = Mathf.Clamp01(t);
        Vector2 closestPoint = a + ab * t;
        return Vector2.Distance(p, closestPoint);
    }

    // Assign uvs to the mesh in some random way (not thought out properly, this is just to make the dissolve effect work)
    public void AssignUVs(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = new Vector2[vertices.Length];  

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int vertexIndex1 = triangles[i];
            int vertexIndex2 = triangles[i + 1];
            int vertexIndex3 = triangles[i + 2];

            uvs[vertexIndex1] = new Vector2(0, 0);
            uvs[vertexIndex2] = new Vector2(1, 0);
            uvs[vertexIndex3] = new Vector2(0, 1);
        }
        mesh.uv = uvs; 
    }
}