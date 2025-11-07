using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for SplineRoad to show live preview
/// </summary>
[CustomEditor(typeof(SplineRoad))]
public class SplineRoadEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SplineRoad splineRoad = (SplineRoad)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Update Spline", GUILayout.Height(25)))
        {
            splineRoad.GenerateSpline();
            EditorUtility.SetDirty(splineRoad);
            SceneView.RepaintAll();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Spline Points: {splineRoad.SplinePoints.Count}\nTotal Length: {splineRoad.GetTotalLength():F2}", MessageType.Info);
    }
    
    // Draw handles in scene view
    private void OnSceneGUI()
    {
        SplineRoad splineRoad = (SplineRoad)target;
        
        // Regenerate spline when control points move
        if (GUI.changed)
        {
            splineRoad.GenerateSpline();
        }
    }
}
