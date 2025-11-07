using UnityEngine;
using UnityEditor;

/// <summary>
/// Validates WaveRoad setup and shows helpful error messages
/// </summary>
[CustomEditor(typeof(WaveGenerator))]
public class WaveGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        WaveGenerator waveGen = (WaveGenerator)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wave Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Cyan arrow shows wave direction. Blue spheres show wave peaks.", MessageType.Info);
        
        // Validation
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Setup Validation", EditorStyles.boldLabel);
        
        WaveRoadMesh roadMesh = waveGen.GetComponent<WaveRoadMesh>();
        if (roadMesh == null)
        {
            EditorGUILayout.HelpBox("Missing WaveRoadMesh component on this GameObject!", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("✓ WaveRoadMesh found", MessageType.Info);
        }
        
        MeshRenderer meshRenderer = waveGen.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            EditorGUILayout.HelpBox("Missing MeshRenderer! Add one to see the road.", MessageType.Error);
        }
        else if (meshRenderer.sharedMaterial == null)
        {
            EditorGUILayout.HelpBox("MeshRenderer has no material! Add a material to see the road.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("✓ MeshRenderer with material", MessageType.Info);
        }
        
        MeshFilter meshFilter = waveGen.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            EditorGUILayout.HelpBox("Missing MeshFilter! Add one.", MessageType.Error);
        }
        else if (meshFilter.sharedMesh == null)
        {
            EditorGUILayout.HelpBox("No mesh generated yet. Click 'Generate Road Mesh' on WaveRoadMesh component.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"✓ Mesh has {meshFilter.sharedMesh.vertexCount} vertices", MessageType.Info);
        }
    }
}
