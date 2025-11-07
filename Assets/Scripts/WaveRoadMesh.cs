using UnityEngine;

/// <summary>
/// Generates and animates a road mesh with waves
/// Efficient CPU-based mesh deformation
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaveRoadMesh : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineRoad splineRoad;
    [SerializeField] private WaveGenerator waveGenerator;
    
    [Header("Mesh Settings")]
    [SerializeField] private int lengthSegments = 100;
    [SerializeField] private bool updateMeshEveryFrame = true;
    
    [Header("UV Settings")]
    [SerializeField] private float uvTileLength = 10f;
    
    private Mesh mesh;
    private Vector3[] baseVertices; // Original vertex positions without waves
    private Vector3[] vertices;
    private Vector3[] normals;
    private int[] triangles;
    private int[] backfaceTriangles; // For two-sided rendering
    private Vector2[] uvs;
    
    void Start()
    {
        GenerateRoadMesh();
        
        // Immediately update with waves so we don't see the flat version
        if (waveGenerator != null && updateMeshEveryFrame)
        {
            UpdateWaveDeformation();
        }
    }
    
    void Update()
    {
        if (updateMeshEveryFrame && waveGenerator != null)
        {
            UpdateWaveDeformation();
        }
    }
    
    public void GenerateRoadMesh()
    {
        if (splineRoad == null)
        {
            Debug.LogError("WaveRoadMesh: SplineRoad reference is missing! Please assign it in the inspector.");
            return;
        }
        
        Debug.Log("WaveRoadMesh: Generating spline...");
        splineRoad.GenerateSpline();
        
        if (splineRoad.SplinePoints.Count < 2)
        {
            Debug.LogError($"WaveRoadMesh: Not enough spline points! Got {splineRoad.SplinePoints.Count}, need at least 2. Check that control points are assigned in SplineRoad.");
            return;
        }
        
        Debug.Log($"WaveRoadMesh: Spline has {splineRoad.SplinePoints.Count} points, length {splineRoad.GetTotalLength():F2}");
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "Wave Road Mesh";
        meshFilter.mesh = mesh;
        
        int widthSegs = splineRoad.WidthSegments;
        int lengthSegs = lengthSegments;
        
        int vertexCount = (widthSegs + 1) * (lengthSegs + 1);
        baseVertices = new Vector3[vertexCount];
        vertices = new Vector3[vertexCount];
        normals = new Vector3[vertexCount];
        uvs = new Vector2[vertexCount];
        
        float roadWidth = splineRoad.RoadWidth;
        float totalLength = splineRoad.GetTotalLength();
        
        // Generate vertices
        int vertIndex = 0;
        for (int z = 0; z <= lengthSegs; z++)
        {
            float tLength = z / (float)lengthSegs;
            float distance = tLength * totalLength;
            
            Vector3 normal, tangent;
            Vector3 centerPoint = splineRoad.GetPointAtDistance(distance, out normal, out tangent);
            Vector3 right = Vector3.Cross(normal, tangent).normalized;
            
            for (int x = 0; x <= widthSegs; x++)
            {
                float tWidth = x / (float)widthSegs - 0.5f;
                Vector3 offset = right * (tWidth * roadWidth);
                
                baseVertices[vertIndex] = centerPoint + offset;
                vertices[vertIndex] = baseVertices[vertIndex];
                normals[vertIndex] = normal;
                uvs[vertIndex] = new Vector2(x / (float)widthSegs, distance / uvTileLength);
                
                vertIndex++;
            }
        }
        
        // Generate triangles (double-sided for two-sided rendering)
        int singleSideTriCount = widthSegs * lengthSegs * 6;
        int totalTriCount = singleSideTriCount * 2; // Front and back
        triangles = new int[totalTriCount];
        int triIndex = 0;
        
        // Front faces
        for (int z = 0; z < lengthSegs; z++)
        {
            for (int x = 0; x < widthSegs; x++)
            {
                int bottomLeft = z * (widthSegs + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * (widthSegs + 1) + x;
                int topRight = topLeft + 1;
                
                // First triangle
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;
                
                // Second triangle
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
        }
        
        // Back faces (reversed winding order)
        for (int z = 0; z < lengthSegs; z++)
        {
            for (int x = 0; x < widthSegs; x++)
            {
                int bottomLeft = z * (widthSegs + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * (widthSegs + 1) + x;
                int topRight = topLeft + 1;
                
                // First triangle (reversed)
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                
                // Second triangle (reversed)
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = topLeft;
            }
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;
        
        Debug.Log($"WaveRoadMesh: Mesh generated with {vertices.Length} vertices and {triangles.Length / 3} triangles");
        
        // Add mesh collider for physics
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        meshCollider.sharedMesh = mesh;
        
        Debug.Log("WaveRoadMesh: Generation complete! Mesh should now be visible.");
    }
    
    private void UpdateWaveDeformation()
    {
        if (mesh == null || baseVertices == null) return;
        
        // Update vertex positions based on waves
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 basePos = baseVertices[i];
            float waveHeight = waveGenerator.GetWaveHeight(basePos);
            
            vertices[i] = basePos + Vector3.up * waveHeight;
            normals[i] = waveGenerator.GetWaveNormal(basePos);
        }
        
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        
        // Update collider
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }
    
    /// <summary>
    /// Get the closest point on the road surface to a world position
    /// </summary>
    public bool GetSurfaceInfoAtPosition(Vector3 worldPos, out Vector3 surfacePoint, out Vector3 surfaceNormal)
    {
        surfacePoint = Vector3.zero;
        surfaceNormal = Vector3.up;
        
        if (waveGenerator == null) return false;
        
        // Simple vertical projection for now
        surfacePoint = new Vector3(worldPos.x, 0, worldPos.z);
        surfacePoint.y = waveGenerator.GetWaveHeight(surfacePoint);
        surfaceNormal = waveGenerator.GetWaveNormal(surfacePoint);
        
        return true;
    }
}
