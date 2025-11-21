using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BuildingGen : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridWidth = 1f;
    public float gridHeight = 1f;
    public float surfaceOffset = 0.01f;

    [Header("Building Wall")]
    public Sprite wallSprite;
    public Material wallMaterial;

    [Header("Ground Floor")]
    public List<Sprite> doorSprites;
    public List<Sprite> wallDetailSprites;
    [Range(0f, 1f)] public float doorChance = 0.2f;
    [Range(0f, 1f)] public float wallDetailChance = 0.3f;

    [Header("Windows")]
    public List<Sprite> windowSprites;
    [Range(0f, 1f)] public float windowDensity = 0.9f;

    [Header("Window Glow")]
    public bool enableWindowGlow = true;
    [Range(0f, 1f)] public float glowChance = 0.7f;
    public Color glowColor = new Color(1f, 0.9f, 0.6f, 1f);
    [Range(0f, 5f)] public float glowIntensity = 1.5f;

    [Header("Sprite Rendering")]
    public int sortingOrder = 0;
    public string sortingLayerName = "Default";

    [HideInInspector] public List<GameObject> spawnedObjects = new List<GameObject>();

    public void Generate()
    {
        Clear();
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("BuildingGen requires a MeshFilter with a mesh!");
            return;
        }

        // Use real mesh bounds (in local space)
        Bounds bounds = meshFilter.sharedMesh.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        float width  = bounds.size.x;
        float height = bounds.size.y;
        float depth  = bounds.size.z;

        // FRONT (Z+)
        GenerateFace(width, height,
            new Vector3(center.x, center.y, center.z + extents.z),
            Quaternion.identity,
            "Front_Outer");

        GenerateFace(width, height,
            new Vector3(center.x, center.y, center.z + extents.z),
            Quaternion.Euler(0, 180, 0),
            "Front_Inner");

        // BACK (Z-)
        GenerateFace(width, height,
            new Vector3(center.x, center.y, center.z - extents.z),
            Quaternion.identity,
            "Back_Outer");

        GenerateFace(width, height,
            new Vector3(center.x, center.y, center.z - extents.z),
            Quaternion.Euler(0, 180, 0),
            "Back_Inner");

        // RIGHT (X+), width = depth
        GenerateFace(depth, height,
            new Vector3(center.x + extents.x, center.y, center.z),
            Quaternion.Euler(0, 90, 0),
            "Right_Outer");

        GenerateFace(depth, height,
            new Vector3(center.x + extents.x, center.y, center.z),
            Quaternion.Euler(0, -90, 0),
            "Right_Inner");

        // LEFT (X-), width = depth
        GenerateFace(depth, height,
            new Vector3(center.x - extents.x, center.y, center.z),
            Quaternion.Euler(0, -90, 0),
            "Left_Outer");

        GenerateFace(depth, height,
            new Vector3(center.x - extents.x, center.y, center.z),
            Quaternion.Euler(0, 90, 0),
            "Left_Inner");
    }

    void GenerateFace(float faceWidth, float faceHeight, Vector3 faceCenterLocal, Quaternion rotation, string faceName)
    {
        // Wall background
        if (wallSprite != null)
        {
            GameObject wallObj = CreateSpriteObject($"Wall_{faceName}", faceCenterLocal, wallSprite);
            wallObj.transform.localRotation = rotation;

            SpriteRenderer sr = wallObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(faceWidth, faceHeight);
                sr.sortingOrder = sortingOrder - 10;

                if (wallMaterial != null)
                {
                    sr.material = wallMaterial;
                }
            }
        }

        // Grid in face-local coordinates (origin is face center)
        int columns = Mathf.FloorToInt(faceWidth / gridWidth);
        int rows = Mathf.FloorToInt(faceHeight / gridHeight);

        if (columns <= 0 || rows <= 0)
            return;

        float startX = -faceWidth / 2f;
        float startY = -faceHeight / 2f;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                // Position in face-local space (XY plane, Z = 0)
                Vector3 faceLocalPos = new Vector3(
                    startX + (x * gridWidth) + (gridWidth / 2f),
                    startY + (y * gridHeight) + (gridHeight / 2f),
                    0f
                );

                // Convert to object-local space, apply offset later in GenerateTile
                Vector3 localPos = faceCenterLocal + rotation * faceLocalPos;
                GenerateTile(localPos, rotation, x, y, rows, faceName);
            }
        }
    }

    void GenerateTile(Vector3 localPos, Quaternion rotation, int x, int y, int totalRows, string faceName)
    {
        bool isGroundFloor = (y == 0);
        Sprite selectedSprite = null;
        bool isWindow = false;

        if (isGroundFloor)
        {
            // Ground floor: doors or wall details
            if (doorSprites.Count > 0 && Random.value < doorChance)
            {
                selectedSprite = doorSprites[Random.Range(0, doorSprites.Count)];
            }
            else if (wallDetailSprites.Count > 0 && Random.value < wallDetailChance)
            {
                selectedSprite = wallDetailSprites[Random.Range(0, wallDetailSprites.Count)];
            }
        }
        else
        {
            // Upper floors: windows
            if (windowSprites.Count > 0 && Random.value < windowDensity)
            {
                selectedSprite = windowSprites[Random.Range(0, windowSprites.Count)];
                isWindow = true;
            }
        }

        if (selectedSprite != null)
        {
            // Push tiles slightly off the wall along the face normal to avoid z-fighting
            Vector3 offsetPos = localPos + (rotation * Vector3.forward) * surfaceOffset;

            GameObject obj = CreateSpriteObject($"Tile_{faceName}_{x}_{y}", offsetPos, selectedSprite);
            obj.transform.localRotation = rotation;

            // Add glow to windows
            if (isWindow && enableWindowGlow && Random.value < glowChance)
            {
                AddGlowEffect(obj);
            }
        }
    }

    GameObject CreateSpriteObject(string name, Vector3 localPos, Sprite sprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        spawnedObjects.Add(obj);
        return obj;
    }

    void AddGlowEffect(GameObject obj)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Material glowMaterial = new Material(Shader.Find("Sprites/Default"));
        glowMaterial.EnableKeyword("_EMISSION");
        glowMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);

        sr.material = glowMaterial;
        sr.color = Color.white;
    }

    public void Clear()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                DestroyImmediate(spawnedObjects[i]);
            }
        }
        spawnedObjects.Clear();
    }
}

// Custom Editor Button
#if UNITY_EDITOR
[CustomEditor(typeof(BuildingGen))]
public class BuildingGenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        BuildingGen script = (BuildingGen)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Generate Building", GUILayout.Height(40)))
        {
            script.Generate();
        }
        if (GUILayout.Button("Clear"))
        {
            script.Clear();
        }
    }
}
#endif
