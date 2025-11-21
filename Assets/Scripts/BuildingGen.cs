using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BuildingGen : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridWidth = 2f;
    public float gridHeight = 2f;
    public float surfaceOffset = 0.01f;

    [Header("Building Wall")]
    public Sprite wallSprite;
    public Material wallMaterial;

    [Header("Ground Floor")]
    public List<Sprite> doorSprites;
    public List<Sprite> wallDetailSprites;
    [Range(0f, 1f)] public float doorChance = 0.2f;
    [Range(0f, 1f)] public float wallDetailChance = 0.3f;

    [Header("Windows - Base")]
    public List<Sprite> baseWindowSprites;
    [Range(0f, 1f)] public float windowDensity = 0.9f; // Chance a tile has ANY window

    [Header("Windows - Weird")]
    public List<Sprite> weirdWindowSprites;
    [Range(0f, 1f)] public float weirdWindowChance = 0.15f; // If a window spawns, chance it is "weird"

    [Header("Window Glow")]
    public bool enableWindowGlow = true;
    [Range(0f, 1f)] public float glowChance = 0.7f;
    [ColorUsage(true, true)] public Color glowColor = new Color(1f, 0.9f, 0.6f, 1f);
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

        Bounds bounds = meshFilter.sharedMesh.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        Vector3 parentScale = transform.localScale;

        // Scale Correction Vectors
        Vector3 invScaleFront = new Vector3(1f / Mathf.Abs(parentScale.x), 1f / Mathf.Abs(parentScale.y), 1f);
        Vector3 invScaleSide  = new Vector3(1f / Mathf.Abs(parentScale.z), 1f / Mathf.Abs(parentScale.y), 1f);

        // --- FRONT (Z+) ---
        GenerateFace(bounds.size.x, bounds.size.y, parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z + extents.z), Quaternion.identity, invScaleFront, "Front_Outer");
        GenerateFace(bounds.size.x, bounds.size.y, parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z + extents.z), Quaternion.Euler(0, 180, 0), invScaleFront, "Front_Inner");

        // --- BACK (Z-) ---
        GenerateFace(bounds.size.x, bounds.size.y, parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z - extents.z), Quaternion.identity, invScaleFront, "Back_Outer");
        GenerateFace(bounds.size.x, bounds.size.y, parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z - extents.z), Quaternion.Euler(0, 180, 0), invScaleFront, "Back_Inner");

        // --- RIGHT (X+) ---
        GenerateFace(bounds.size.z, bounds.size.y, parentScale.z, parentScale.y,
            new Vector3(center.x + extents.x, center.y, center.z), Quaternion.Euler(0, -90, 0), invScaleSide, "Right_Outer");
        GenerateFace(bounds.size.z, bounds.size.y, parentScale.z, parentScale.y,
            new Vector3(center.x + extents.x, center.y, center.z), Quaternion.Euler(0, 90, 0), invScaleSide, "Right_Inner");

        // --- LEFT (X-) ---
        GenerateFace(bounds.size.z, bounds.size.y, parentScale.z, parentScale.y,
            new Vector3(center.x - extents.x, center.y, center.z), Quaternion.Euler(0, 90, 0), invScaleSide, "Left_Outer");
        GenerateFace(bounds.size.z, bounds.size.y, parentScale.z, parentScale.y,
            new Vector3(center.x - extents.x, center.y, center.z), Quaternion.Euler(0, -90, 0), invScaleSide, "Left_Inner");
    }

    void GenerateFace(float localWidth, float localHeight, float scaleX, float scaleY, Vector3 faceCenterLocal, Quaternion rotation, Vector3 childInverseScale, string faceName)
    {
        float worldWidth = localWidth * Mathf.Abs(scaleX);
        float worldHeight = localHeight * Mathf.Abs(scaleY);

        // Wall Background
        if (wallSprite != null)
        {
            GameObject wallObj = CreateSpriteObject($"Wall_{faceName}", faceCenterLocal, wallSprite);
            wallObj.transform.localRotation = rotation;
            wallObj.transform.localScale = childInverseScale; // Apply inverse scale

            SpriteRenderer sr = wallObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(worldWidth, worldHeight);
                sr.sortingOrder = sortingOrder - 10;
                if (wallMaterial != null) sr.material = wallMaterial;
            }
        }

        int columns = Mathf.FloorToInt(worldWidth / gridWidth);
        int rows = Mathf.FloorToInt(worldHeight / gridHeight);

        if (columns <= 0 || rows <= 0) return;

        float localStepX = gridWidth / scaleX;
        float localStepY = gridHeight / scaleY;
        
        float startX = -(columns * localStepX) / 2f + (localStepX / 2f);
        float startY = -(rows * localStepY) / 2f + (localStepY / 2f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 faceLocalPos = new Vector3(startX + (x * localStepX), startY + (y * localStepY), 0f);
                Vector3 localPos = faceCenterLocal + rotation * faceLocalPos;
                GenerateTile(localPos, rotation, childInverseScale, x, y, rows, faceName);
            }
        }
    }

    void GenerateTile(Vector3 localPos, Quaternion rotation, Vector3 inverseScale, int x, int y, int totalRows, string faceName)
    {
        bool isGroundFloor = (y == 0);
        Sprite selectedSprite = null;
        bool isWindow = false;

        if (isGroundFloor)
        {
            // Ground floor logic
            if (doorSprites.Count > 0 && Random.value < doorChance)
                selectedSprite = doorSprites[Random.Range(0, doorSprites.Count)];
            else if (wallDetailSprites.Count > 0 && Random.value < wallDetailChance)
                selectedSprite = wallDetailSprites[Random.Range(0, wallDetailSprites.Count)];
        }
        else
        {
            // Upper floors logic
            if (Random.value < windowDensity)
            {
                isWindow = true;
                
                // Logic: Weird vs Base
                bool isWeird = (weirdWindowSprites.Count > 0) && (Random.value < weirdWindowChance);

                if (isWeird)
                {
                    selectedSprite = weirdWindowSprites[Random.Range(0, weirdWindowSprites.Count)];
                }
                else if (baseWindowSprites.Count > 0)
                {
                    selectedSprite = baseWindowSprites[Random.Range(0, baseWindowSprites.Count)];
                }
            }
        }

        if (selectedSprite != null)
        {
            Vector3 offsetPos = localPos + (rotation * Vector3.forward) * surfaceOffset;
            GameObject obj = CreateSpriteObject($"Tile_{faceName}_{x}_{y}", offsetPos, selectedSprite);
            obj.transform.localRotation = rotation;
            obj.transform.localScale = inverseScale; // Apply inverse scale

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
    }

    public void Clear()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null) DestroyImmediate(spawnedObjects[i]);
        }
        spawnedObjects.Clear();
        
        // Fallback cleanup
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => DestroyImmediate(child));
    }
}

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