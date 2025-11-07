using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple Catmull-Rom spline for creating a road path
/// </summary>
public class SplineRoad : MonoBehaviour
{
    [Header("Spline Settings")]
    [SerializeField] private List<Transform> controlPoints = new List<Transform>();
    [SerializeField] private bool closedLoop = false;
    [SerializeField] private int resolution = 10; // Points per segment
    
    [Header("Road Dimensions")]
    [SerializeField] private float roadWidth = 10f;
    [SerializeField] private int widthSegments = 10;
    
    [Header("Gizmos")]
    [SerializeField] private bool showSpline = true;
    [SerializeField] private bool showControlPoints = true;
    [SerializeField] private Color splineColor = Color.yellow;
    
    private List<Vector3> splinePoints = new List<Vector3>();
    private List<Vector3> splineNormals = new List<Vector3>();
    private List<Vector3> splineTangents = new List<Vector3>();
    
    void Start()
    {
        GenerateSpline();
    }
    
    public void GenerateSpline()
    {
        splinePoints.Clear();
        splineNormals.Clear();
        splineTangents.Clear();
        
        if (controlPoints.Count < 2) return;
        
        int segments = closedLoop ? controlPoints.Count : controlPoints.Count - 1;
        
        for (int i = 0; i < segments; i++)
        {
            Vector3 p0 = GetControlPoint(i - 1);
            Vector3 p1 = GetControlPoint(i);
            Vector3 p2 = GetControlPoint(i + 1);
            Vector3 p3 = GetControlPoint(i + 2);
            
            for (int j = 0; j < resolution; j++)
            {
                float t = j / (float)resolution;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                Vector3 tangent = CatmullRomDerivative(p0, p1, p2, p3, t).normalized;
                
                splinePoints.Add(point);
                splineTangents.Add(tangent);
                splineNormals.Add(Vector3.up); // Default, can be customized
            }
        }
    }
    
    private Vector3 GetControlPoint(int index)
    {
        if (closedLoop)
        {
            index = (index + controlPoints.Count) % controlPoints.Count;
        }
        else
        {
            index = Mathf.Clamp(index, 0, controlPoints.Count - 1);
        }
        return controlPoints[index].position;
    }
    
    // Catmull-Rom spline interpolation
    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
    
    private Vector3 CatmullRomDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        
        return 0.5f * (
            (-p0 + p2) +
            2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t +
            3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2
        );
    }
    
    public Vector3 GetPointAtDistance(float distance, out Vector3 normal, out Vector3 tangent)
    {
        if (splinePoints.Count == 0)
        {
            normal = Vector3.up;
            tangent = Vector3.forward;
            return transform.position;
        }
        
        float totalLength = GetTotalLength();
        distance = Mathf.Repeat(distance, totalLength);
        
        float currentDistance = 0f;
        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(splinePoints[i], splinePoints[i + 1]);
            if (currentDistance + segmentLength >= distance)
            {
                float t = (distance - currentDistance) / segmentLength;
                normal = Vector3.Lerp(splineNormals[i], splineNormals[i + 1], t);
                tangent = Vector3.Lerp(splineTangents[i], splineTangents[i + 1], t);
                return Vector3.Lerp(splinePoints[i], splinePoints[i + 1], t);
            }
            currentDistance += segmentLength;
        }
        
        normal = splineNormals[splineNormals.Count - 1];
        tangent = splineTangents[splineTangents.Count - 1];
        return splinePoints[splinePoints.Count - 1];
    }
    
    public float GetTotalLength()
    {
        float length = 0f;
        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            length += Vector3.Distance(splinePoints[i], splinePoints[i + 1]);
        }
        return length;
    }
    
    public float RoadWidth => roadWidth;
    public int WidthSegments => widthSegments;
    public List<Vector3> SplinePoints => splinePoints;
    
    void OnDrawGizmos()
    {
        if (showControlPoints && controlPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (var point in controlPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.3f);
                }
            }
        }
        
        if (showSpline && splinePoints != null && splinePoints.Count > 1)
        {
            Gizmos.color = splineColor;
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                Gizmos.DrawLine(splinePoints[i], splinePoints[i + 1]);
            }
        }
    }
}
