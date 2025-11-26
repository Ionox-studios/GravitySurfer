using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BuildingToTexture : MonoBehaviour
{
    [Header("Render Settings")]
    public GameObject buildingToRender;
    public int textureWidth = 512;
    public int textureHeight = 512;
    public Color backgroundColor = Color.clear;
    
    [Header("Camera Settings")]
    public float cameraDistance = 10f;
    public Vector3 cameraOffset = Vector3.zero;
    
    [Header("Output")]
    public string fileName = "BuildingTexture";

    public void RenderToPNG()
    {
        if (buildingToRender == null)
        {
            Debug.LogError("No building assigned to render!");
            return;
        }

        // Store original materials
        Renderer[] renderers = buildingToRender.GetComponentsInChildren<Renderer>();
        Material[][] originalMaterials = new Material[renderers.Length][];
        
        // Replace with unlit materials temporarily
        Material unlitMat = new Material(Shader.Find("Unlit/Transparent"));
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
            Material[] unlitMats = new Material[renderers[i].sharedMaterials.Length];
            
            for (int j = 0; j < unlitMats.Length; j++)
            {
                unlitMats[j] = new Material(unlitMat);
                // Copy main texture from original
                if (originalMaterials[i][j].HasProperty("_MainTex"))
                {
                    unlitMats[j].mainTexture = originalMaterials[i][j].mainTexture;
                }
                if (originalMaterials[i][j].HasProperty("_Color"))
                {
                    unlitMats[j].color = originalMaterials[i][j].color;
                }
            }
            renderers[i].sharedMaterials = unlitMats;
        }

        // Create temporary camera
        GameObject camGO = new GameObject("TempRenderCamera");
        Camera renderCam = camGO.AddComponent<Camera>();
        renderCam.clearFlags = CameraClearFlags.SolidColor;
        renderCam.backgroundColor = backgroundColor;
        renderCam.orthographic = true;

        // Position camera to capture building
        Bounds bounds = CalculateBounds(buildingToRender);
        Vector3 center = bounds.center;
        
        renderCam.transform.position = center + new Vector3(0, 0, -cameraDistance) + cameraOffset;
        renderCam.transform.LookAt(center);
        
        // Set orthographic size to fit building
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y);
        renderCam.orthographicSize = maxSize * 0.6f;

        // Create RenderTexture
        RenderTexture rt = new RenderTexture(textureWidth, textureHeight, 24);
        renderCam.targetTexture = rt;

        // Render
        renderCam.Render();

        // Read pixels from RenderTexture
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        texture.Apply();

        // Save to file
        byte[] bytes = texture.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, fileName + ".png");
        File.WriteAllBytes(path, bytes);

        Debug.Log($"Building texture saved to: {path}");

        // Restore original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            // Clean up temporary materials
            foreach (Material mat in renderers[i].sharedMaterials)
            {
                DestroyImmediate(mat);
            }
            renderers[i].sharedMaterials = originalMaterials[i];
        }

        // Cleanup
        RenderTexture.active = null;
        renderCam.targetTexture = null;
        DestroyImmediate(rt);
        DestroyImmediate(camGO);
        DestroyImmediate(texture);
        DestroyImmediate(unlitMat);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BuildingToTexture))]
public class BuildingToTextureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BuildingToTexture script = (BuildingToTexture)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Render Building to PNG", GUILayout.Height(40)))
        {
            script.RenderToPNG();
        }
    }
}
#endif
