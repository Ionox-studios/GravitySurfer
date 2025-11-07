using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for WaveRoadMesh to allow generation in edit mode
/// </summary>
[CustomEditor(typeof(WaveRoadMesh))]
public class WaveRoadMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        WaveRoadMesh roadMesh = (WaveRoadMesh)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click 'Generate Road Mesh' to create/update the road mesh in the editor.", MessageType.Info);
        
        if (GUILayout.Button("Generate Road Mesh", GUILayout.Height(30)))
        {
            roadMesh.GenerateRoadMesh();
            EditorUtility.SetDirty(roadMesh);
            SceneView.RepaintAll();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Clear Mesh"))
        {
            ClearMesh(roadMesh);
        }
    }
    
    private void ClearMesh(WaveRoadMesh roadMesh)
    {
        MeshFilter meshFilter = roadMesh.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            if (meshFilter.sharedMesh != null)
            {
                DestroyImmediate(meshFilter.sharedMesh);
            }
            meshFilter.sharedMesh = null;
        }
        
        MeshCollider meshCollider = roadMesh.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
        }
        
        EditorUtility.SetDirty(roadMesh);
    }
}
