using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BuildingGen : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridWidth = 2f; // Set typically to 2 or 3 for standard Unity units
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

    [Header("Windows")]
    public List<Sprite> windowSprites;
    [Range(0f, 1f)] public float windowDensity = 0.9f;

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

        // 1. Get Local Bounds
        Bounds bounds = meshFilter.sharedMesh.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        // 2. Get The Parent Scale to calculate "Real World" size
        Vector3 parentScale = transform.localScale;

        // 3. Calculate Scale Factors for Children
        // To prevent children from stretching, we scale them by (1/ParentScale)
        // Note: We use absolute values to handle negative scaling
        Vector3 invScaleFront = new Vector3(1f / Mathf.Abs(parentScale.x), 1f / Mathf.Abs(parentScale.y), 1f);
        Vector3 invScaleSide  = new Vector3(1f / Mathf.Abs(parentScale.z), 1f / Mathf.Abs(parentScale.y), 1f);

        // --- FRONT (Z+) ---
        // Uses X (Width) and Y (Height)
        GenerateFace(
            bounds.size.x, bounds.size.y, 
            parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z + extents.z),
            Quaternion.identity,
            invScaleFront, 
            "Front_Outer"
        );

        GenerateFace(
            bounds.size.x, bounds.size.y,
            parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z + extents.z),
            Quaternion.Euler(0, 180, 0),
            invScaleFront,
            "Front_Inner"
        );

        // --- BACK (Z-) ---
        GenerateFace(
            bounds.size.x, bounds.size.y,
            parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z - extents.z),
            Quaternion.identity, // Usually back faces need 180 rotation relative to camera, but keeping your logic
            invScaleFront,
            "Back_Outer"
        );
        
        // Back Inner often unnecessary unless transparent, but keeping for consistency
        GenerateFace(
            bounds.size.x, bounds.size.y,
            parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z - extents.z),
            Quaternion.Euler(0, 180, 0),
            invScaleFront,
            "Back_Inner"
        );

        // --- RIGHT (X+) ---
        // Uses Z (Width) and Y (Height). 
        // Note: When rotated 90deg, the object's Local X aligns with World Z.
        GenerateFace(
            bounds.size.z, bounds.size.y,
            parentScale.z, parentScale.y,
            new Vector3(center.x + extents.x, center.y, center.z),
            Quaternion.Euler(0, -90, 0), // Rotated -90 so it faces out right
            invScaleSide,
            "Right_Outer"
        );

        GenerateFace(
            bounds.size.z, bounds.size.y,
            parentScale.z, parentScale.y,
            new Vector3(center.x + extents.x, center.y, center.z),
            Quaternion.Euler(0, 90, 0),
            invScaleSide,
            "Right_Inner"
        );

        // --- LEFT (X-) ---
        GenerateFace(
            bounds.size.z, bounds.size.y,
            parentScale.z, parentScale.y,
            new Vector3(center.x - extents.x, center.y, center.z),
            Quaternion.Euler(0, 90, 0),
            invScaleSide,
            "Left_Outer"
        );

        GenerateFace(
            bounds.size.z, bounds.size.y,
            parentScale.z, parentScale.y,
            new Vector3(center.x - extents.x, center.y, center.z),
            Quaternion.Euler(0, -90, 0),
            invScaleSide,
            "Left_Inner"
        );
    }

    void GenerateFace(
        float localWidth, float localHeight, 
        float scaleX, float scaleY, 
        Vector3 faceCenterLocal, 
        Quaternion rotation, 
        Vector3 childInverseScale,
        string faceName)
    {
        // Calculate "World" dimensions to determine grid count
        float worldWidth = localWidth * Mathf.Abs(scaleX);
        float worldHeight = localHeight * Mathf.Abs(scaleY);

        // --- WALL BACKGROUND ---
        if (wallSprite != null)
        {
            GameObject wallObj = CreateSpriteObject($"Wall_{faceName}", faceCenterLocal, wallSprite);
            wallObj.transform.localRotation = rotation;
            
            // Apply inverse scale so the "Transform" scale is neutralized
            wallObj.transform.localScale = childInverseScale;

            SpriteRenderer sr = wallObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                // Since we neutralized the scale, we set the SIZE to the World Dimensions
                // This ensures the tiling matches world units (1 tile = 1 unit)
                sr.size = new Vector2(worldWidth, worldHeight);
                sr.sortingOrder = sortingOrder - 10;
                if (wallMaterial != null) sr.material = wallMaterial;
            }
        }

        // --- GRID GENERATION ---
        int columns = Mathf.FloorToInt(worldWidth / gridWidth);
        int rows = Mathf.FloorToInt(worldHeight / gridHeight);

        if (columns <= 0 || rows <= 0) return;

        // We iterate using grid counts, but we must place them using LOCAL coordinates.
        // Step Size in Local Space = GridSize / Scale
        float localStepX = gridWidth / scaleX;
        float localStepY = gridHeight / scaleY;

        // Calculate start position (bottom-left of the face in local space)
        // We center the grid on the face
        float usedLocalWidth = columns * localStepX;
        float usedLocalHeight = rows * localStepY;
        
        float startX = -usedLocalWidth / 2f + (localStepX / 2f);
        float startY = -usedLocalHeight / 2f + (localStepY / 2f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                // Position in face-local space (XY plane)
                Vector3 faceLocalPos = new Vector3(
                    startX + (x * localStepX),
                    startY + (y * localStepY),
                    0f
                );

                // Convert to object-local space
                Vector3 localPos = faceCenterLocal + rotation * faceLocalPos;
                
                // Generate
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
            if (doorSprites.Count > 0 && Random.value < doorChance)
                selectedSprite = doorSprites[Random.Range(0, doorSprites.Count)];
            else if (wallDetailSprites.Count > 0 && Random.value < wallDetailChance)
                selectedSprite = wallDetailSprites[Random.Range(0, wallDetailSprites.Count)];
        }
        else
        {
            if (windowSprites.Count > 0 && Random.value < windowDensity)
            {
                selectedSprite = windowSprites[Random.Range(0, windowSprites.Count)];
                isWindow = true;
            }
        }

        if (selectedSprite != null)
        {
            Vector3 offsetPos = localPos + (rotation * Vector3.forward) * surfaceOffset;

            GameObject obj = CreateSpriteObject($"Tile_{faceName}_{x}_{y}", offsetPos, selectedSprite);
            obj.transform.localRotation = rotation;
            
            // CRITICAL FIX: Apply the inverse scale to the sprite object
            // This ensures a 1x1 sprite remains 1x1 in world space, regardless of parent scale
            obj.transform.localScale = inverseScale;

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
        // Loop backwards to safely remove
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
                DestroyImmediate(spawnedObjects[i]);
        }
        spawnedObjects.Clear();
        
        // Fallback clean up of children if list gets desynced
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