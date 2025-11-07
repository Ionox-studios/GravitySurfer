using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu item to create a complete wave road setup automatically
/// </summary>
public class WaveRoadSetup : MonoBehaviour
{
    [MenuItem("GameObject/3D Object/Wave Road System", false, 10)]
    static void CreateWaveRoadSystem()
    {
        // Create main road object
        GameObject roadObj = new GameObject("WaveRoad");
        roadObj.AddComponent<SplineRoad>();
        roadObj.AddComponent<WaveGenerator>();
        roadObj.AddComponent<WaveRoadMesh>();
        roadObj.AddComponent<MeshFilter>();
        roadObj.AddComponent<MeshRenderer>();
        
        // Create default material
        Material defaultMat = new Material(Shader.Find("Standard"));
        defaultMat.color = new Color(0.3f, 0.3f, 0.8f);
        roadObj.GetComponent<MeshRenderer>().material = defaultMat;
        
        // Create control points
        GameObject controlPointsParent = new GameObject("ControlPoints");
        
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 20),
            new Vector3(0, 0, 40),
            new Vector3(0, 0, 60),
        };
        
        SplineRoad splineRoad = roadObj.GetComponent<SplineRoad>();
        
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject cp = new GameObject($"ControlPoint{i + 1}");
            cp.transform.parent = controlPointsParent.transform;
            cp.transform.position = positions[i];
            
            // Add to spline road
            SerializedObject so = new SerializedObject(splineRoad);
            SerializedProperty controlPoints = so.FindProperty("controlPoints");
            controlPoints.arraySize = i + 1;
            controlPoints.GetArrayElementAtIndex(i).objectReferenceValue = cp.transform;
            so.ApplyModifiedProperties();
        }
        
        // Set up wave road mesh references
        WaveRoadMesh roadMesh = roadObj.GetComponent<WaveRoadMesh>();
        SerializedObject meshSO = new SerializedObject(roadMesh);
        meshSO.FindProperty("splineRoad").objectReferenceValue = splineRoad;
        meshSO.FindProperty("waveGenerator").objectReferenceValue = roadObj.GetComponent<WaveGenerator>();
        meshSO.ApplyModifiedProperties();
        
        Selection.activeGameObject = roadObj;
        
        Debug.Log("Wave Road System created! Now:\n1. Select WaveRoad object\n2. Click 'Generate Road Mesh' button in inspector\n3. Adjust control points to shape your road\n4. Enter Play Mode to see waves!");
        
        EditorUtility.DisplayDialog("Wave Road Created", 
            "Wave Road System created successfully!\n\n" +
            "Next steps:\n" +
            "1. Click 'Generate Road Mesh' in the WaveRoadMesh component\n" +
            "2. Move control points to shape your road\n" +
            "3. Adjust wave settings in WaveGenerator\n" +
            "4. Enter Play Mode to see animated waves!", 
            "OK");
    }
}
