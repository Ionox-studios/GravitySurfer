using UnityEngine;
using System.Collections.Generic;
using System; 

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

    [Header("Roof")]
    public Sprite roofSprite;
    public Material roofMaterial;

    [Header("Ground Floor")]
    public List<Sprite> doorSprites;
    public List<Sprite> wallDetailSprites;
    [Range(0f, 1f)] public float doorChance = 0.2f;
    [Range(0f, 1f)] public float wallDetailChance = 0.3f;

    [Header("Windows - Base")]
    public List<Sprite> baseWindowSprites;
    [Range(0f, 1f)] public float windowDensity = 0.9f; 

    [Header("Windows - Weird")]
    public List<Sprite> weirdWindowSprites;
    [Range(0f, 1f)] public float weirdWindowChance = 0.15f; 

    [Header("Window Glow")]
    public bool enableWindowGlow = true;
    [Range(0f, 1f)] public float glowChance = 0.7f;
    [ColorUsage(true, true)] public Color glowColor = new Color(1f, 0.9f, 0.6f, 1f);
    [Range(0f, 5f)] public float glowIntensity = 1.5f;

    [Header("Sprite Rendering")]
    public int sortingOrder = 0;
    public string sortingLayerName = "Default";

    // --- INTERNAL LISTS ---
    [HideInInspector] public List<GameObject> spawnedObjects = new List<GameObject>();
    [HideInInspector] public List<GameObject> bakedObjects = new List<GameObject>();

    // Shared material reference for glowing windows
    private Material sharedGlowMat; 

    public void Generate()
    {
        Clear(); 
        sharedGlowMat = null; 
        
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

        // Calculate inverse scales to keep sprites 1:1
        Vector3 invScaleFront = new Vector3(1f / Mathf.Abs(parentScale.x), 1f / Mathf.Abs(parentScale.y), 1f);
        Vector3 invScaleSide  = new Vector3(1f / Mathf.Abs(parentScale.z), 1f / Mathf.Abs(parentScale.y), 1f);
        Vector3 invScaleTop   = new Vector3(1f / Mathf.Abs(parentScale.x), 1f / Mathf.Abs(parentScale.z), 1f);

        // ROOF
        GenerateRoof(bounds.size.x, bounds.size.z, parentScale.x, parentScale.z,
            new Vector3(center.x, center.y + extents.y, center.z), Quaternion.Euler(90, 0, 0), invScaleTop);

        // FRONT
        GenerateFace(bounds.size.x, bounds.size.y, parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z + extents.z), Quaternion.identity, invScaleFront, "Front_Outer");
        GenerateFace(bounds.size.x, bounds.size.y, parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z + extents.z), Quaternion.Euler(0, 180, 0), invScaleFront, "Front_Inner");

        // BACK
        GenerateFace(bounds.size.x, bounds.size.y, parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z - extents.z), Quaternion.identity, invScaleFront, "Back_Outer");
        GenerateFace(bounds.size.x, bounds.size.y, parentScale.x, parentScale.y,
            new Vector3(center.x, center.y, center.z - extents.z), Quaternion.Euler(0, 180, 0), invScaleFront, "Back_Inner");

        // RIGHT
        GenerateFace(bounds.size.z, bounds.size.y, parentScale.z, parentScale.y,
            new Vector3(center.x + extents.x, center.y, center.z), Quaternion.Euler(0, -90, 0), invScaleSide, "Right_Outer");
        GenerateFace(bounds.size.z, bounds.size.y, parentScale.z, parentScale.y,
            new Vector3(center.x + extents.x, center.y, center.z), Quaternion.Euler(0, 90, 0), invScaleSide, "Right_Inner");

        // LEFT
        GenerateFace(bounds.size.z, bounds.size.y, parentScale.z, parentScale.y,
            new Vector3(center.x - extents.x, center.y, center.z), Quaternion.Euler(0, 90, 0), invScaleSide, "Left_Outer");
        GenerateFace(bounds.size.z, bounds.size.y, parentScale.z, parentScale.y,
            new Vector3(center.x - extents.x, center.y, center.z), Quaternion.Euler(0, -90, 0), invScaleSide, "Left_Inner");
    }

    void GenerateRoof(float localWidth, float localHeight, float scaleX, float scaleY, Vector3 faceCenterLocal, Quaternion rotation, Vector3 childInverseScale)
    {
        float worldWidth = localWidth * Mathf.Abs(scaleX);
        float worldHeight = localHeight * Mathf.Abs(scaleY);

        if (roofSprite != null)
        {
            GameObject roofObj = CreateSpriteObject("Roof", faceCenterLocal, roofSprite);
            roofObj.transform.localRotation = rotation;
            roofObj.transform.localScale = childInverseScale;

            SpriteRenderer sr = roofObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(worldWidth, worldHeight);
                sr.sortingOrder = sortingOrder - 10;
                if (roofMaterial != null) sr.material = roofMaterial;
            }
        }
    }

    void GenerateFace(float localWidth, float localHeight, float scaleX, float scaleY, Vector3 faceCenterLocal, Quaternion rotation, Vector3 childInverseScale, string faceName)
    {
        float worldWidth = localWidth * Mathf.Abs(scaleX);
        float worldHeight = localHeight * Mathf.Abs(scaleY);

        if (wallSprite != null)
        {
            GameObject wallObj = CreateSpriteObject($"Wall_{faceName}", faceCenterLocal, wallSprite);
            wallObj.transform.localRotation = rotation;
            wallObj.transform.localScale = childInverseScale; 

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
            if (doorSprites.Count > 0 && UnityEngine.Random.value < doorChance)
                selectedSprite = doorSprites[UnityEngine.Random.Range(0, doorSprites.Count)];
            else if (wallDetailSprites.Count > 0 && UnityEngine.Random.value < wallDetailChance)
                selectedSprite = wallDetailSprites[UnityEngine.Random.Range(0, wallDetailSprites.Count)];
        }
        else
        {
            if (UnityEngine.Random.value < windowDensity)
            {
                isWindow = true;
                bool isWeird = (weirdWindowSprites.Count > 0) && (UnityEngine.Random.value < weirdWindowChance);

                if (isWeird) selectedSprite = weirdWindowSprites[UnityEngine.Random.Range(0, weirdWindowSprites.Count)];
                else if (baseWindowSprites.Count > 0) selectedSprite = baseWindowSprites[UnityEngine.Random.Range(0, baseWindowSprites.Count)];
            }
        }

        if (selectedSprite != null)
        {
            Vector3 offsetPos = localPos + (rotation * Vector3.forward) * surfaceOffset;
            GameObject obj = CreateSpriteObject($"Tile_{faceName}_{x}_{y}", offsetPos, selectedSprite);
            obj.transform.localRotation = rotation;
            obj.transform.localScale = inverseScale; 

            if (isWindow && enableWindowGlow && UnityEngine.Random.value < glowChance)
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

        if (sharedGlowMat == null)
        {
            sharedGlowMat = new Material(Shader.Find("Sprites/Default"));
            sharedGlowMat.EnableKeyword("_EMISSION");
            sharedGlowMat.SetColor("_EmissionColor", glowColor * glowIntensity);
            sharedGlowMat.name = "Shared_Window_Glow";
        }

        sr.material = sharedGlowMat;
        sr.color = Color.white;
    }

    // ==========================================
    //              FIXED BAKING LOGIC
    // ==========================================
    public void Bake()
    {
        if (spawnedObjects.Count == 0)
        {
            Debug.LogWarning("Nothing to bake! Generate first.");
            return;
        }

        // Clean up old baked objects
        for (int i = bakedObjects.Count - 1; i >= 0; i--)
        {
            if (bakedObjects[i] != null) DestroyImmediate(bakedObjects[i]);
        }
        bakedObjects.Clear();

        // --- KEY FIX 1: Group by Texture AND Material ---
        // We use a Tuple key: (Texture, Material) to ensure unique batches
        Dictionary<Tuple<Texture, Material>, List<SpriteRenderer>> groups = 
            new Dictionary<Tuple<Texture, Material>, List<SpriteRenderer>>();

        foreach (GameObject obj in spawnedObjects)
        {
            if (obj == null) continue;
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            
            if (sr != null && sr.sprite != null && sr.sprite.texture != null && sr.sharedMaterial != null)
            {
                // Create a unique key based on the specific Texture and the Material used
                var key = new Tuple<Texture, Material>(sr.sprite.texture, sr.sharedMaterial);

                if (!groups.ContainsKey(key))
                {
                    groups[key] = new List<SpriteRenderer>();
                }
                groups[key].Add(sr);
            }
        }

        // Process each group into a single mesh
        foreach (var kvp in groups)
        {
            Texture texture = kvp.Key.Item1;
            Material originalMat = kvp.Key.Item2;
            List<SpriteRenderer> renderers = kvp.Value;

            CombineInstance[] combine = new CombineInstance[renderers.Count];

            for (int i = 0; i < renderers.Count; i++)
            {
                SpriteRenderer sr = renderers[i];
                Mesh tempMesh = new Mesh();

                // Handle Tiled Sprites (Fix for "poofing" walls)
                if (sr.drawMode == SpriteDrawMode.Tiled && sr.sprite != null)
                {
                    List<Vector3> newVerts = new List<Vector3>();
                    List<int> newTris = new List<int>();
                    List<Vector2> newUVs = new List<Vector2>();
                    List<Vector3> newNormals = new List<Vector3>();

                    Vector2 size = sr.size;
                    Vector2 spriteSize = sr.sprite.bounds.size;
                    Vector2 startPos = -size / 2f;

                    // Calculate UV bounds
                    Vector2[] uvs = sr.sprite.uv;
                    float uMin = float.MaxValue, uMax = float.MinValue;
                    float vMin = float.MaxValue, vMax = float.MinValue;
                    foreach (var uv in uvs)
                    {
                        if (uv.x < uMin) uMin = uv.x;
                        if (uv.x > uMax) uMax = uv.x;
                        if (uv.y < vMin) vMin = uv.y;
                        if (uv.y > vMax) vMax = uv.y;
                    }
                    float uWidth = uMax - uMin;
                    float vHeight = vMax - vMin;

                    int vIndex = 0;
                    for (float y = 0; y < size.y; y += spriteSize.y)
                    {
                        for (float x = 0; x < size.x; x += spriteSize.x)
                        {
                            float w = Mathf.Min(spriteSize.x, size.x - x);
                            float h = Mathf.Min(spriteSize.y, size.y - y);

                            float x0 = startPos.x + x;
                            float y0 = startPos.y + y;
                            float x1 = x0 + w;
                            float y1 = y0 + h;

                            newVerts.Add(new Vector3(x0, y0, 0));
                            newVerts.Add(new Vector3(x1, y0, 0));
                            newVerts.Add(new Vector3(x0, y1, 0));
                            newVerts.Add(new Vector3(x1, y1, 0));

                            // Add Normals (Fix for dimmed lighting)
                            newNormals.Add(new Vector3(0, 0, -1));
                            newNormals.Add(new Vector3(0, 0, -1));
                            newNormals.Add(new Vector3(0, 0, -1));
                            newNormals.Add(new Vector3(0, 0, -1));

                            newTris.Add(vIndex + 0);
                            newTris.Add(vIndex + 2);
                            newTris.Add(vIndex + 1);
                            newTris.Add(vIndex + 2);
                            newTris.Add(vIndex + 3);
                            newTris.Add(vIndex + 1);

                            float uRatio = w / spriteSize.x;
                            float vRatio = h / spriteSize.y;

                            newUVs.Add(new Vector2(uMin, vMin));
                            newUVs.Add(new Vector2(uMin + uWidth * uRatio, vMin));
                            newUVs.Add(new Vector2(uMin, vMin + vHeight * vRatio));
                            newUVs.Add(new Vector2(uMin + uWidth * uRatio, vMin + vHeight * vRatio));

                            vIndex += 4;
                        }
                    }
                    tempMesh.SetVertices(newVerts);
                    tempMesh.SetTriangles(newTris, 0);
                    tempMesh.SetUVs(0, newUVs);
                    tempMesh.SetNormals(newNormals);
                }
                else
                {
                    // Standard Simple Sprite
                    Vector3[] spriteVerts = Array.ConvertAll(sr.sprite.vertices, v => (Vector3)v);
                    int[] spriteTris = Array.ConvertAll(sr.sprite.triangles, t => (int)t);
                    Vector2[] spriteUVs = sr.sprite.uv;

                    // Handle Flip
                    if (sr.flipX) for (int k = 0; k < spriteVerts.Length; k++) spriteVerts[k].x *= -1;
                    if (sr.flipY) for (int k = 0; k < spriteVerts.Length; k++) spriteVerts[k].y *= -1;

                    // Generate Normals (Fix for dimmed lighting)
                    Vector3[] spriteNormals = new Vector3[spriteVerts.Length];
                    for (int k = 0; k < spriteNormals.Length; k++) spriteNormals[k] = new Vector3(0, 0, -1);

                    tempMesh.vertices = spriteVerts;
                    tempMesh.triangles = spriteTris;
                    tempMesh.uv = spriteUVs;
                    tempMesh.normals = spriteNormals;
                }

                // --- KEY FIX 2: Bake Colors ---
                Vector3[] finalVerts = tempMesh.vertices;
                Color32[] spriteColors = new Color32[finalVerts.Length];
                Color32 tint = sr.color;
                for (int k = 0; k < spriteColors.Length; k++) spriteColors[k] = tint;
                tempMesh.colors32 = spriteColors;

                combine[i].mesh = tempMesh;
                combine[i].transform = transform.worldToLocalMatrix * sr.transform.localToWorldMatrix;
            }

            // Create the GameObject container
            GameObject bakedObj = new GameObject($"Baked_{originalMat.name}_{texture.name}");
            bakedObj.transform.SetParent(transform);
            bakedObj.transform.localPosition = Vector3.zero;
            bakedObj.transform.localRotation = Quaternion.identity;
            bakedObj.transform.localScale = Vector3.one;

            MeshFilter mf = bakedObj.AddComponent<MeshFilter>();
            MeshRenderer mr = bakedObj.AddComponent<MeshRenderer>();

            Mesh finalMesh = new Mesh();
            finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 
            finalMesh.CombineMeshes(combine, true, true);
            
            mf.sharedMesh = finalMesh;

            // --- KEY FIX 3: Material Clone ---
            // We clone the material and force the texture onto it
            Material bakedMat = new Material(originalMat);
            bakedMat.mainTexture = texture; // Crucial: Tell the shader which image to use!
            bakedMat.name = $"{originalMat.name}_Baked";
            
            // Ensure shader keywords for emission/alpha are set if needed
            if (originalMat.IsKeywordEnabled("_EMISSION")) bakedMat.EnableKeyword("_EMISSION");
            
            mr.sharedMaterial = bakedMat;

            bakedObjects.Add(bakedObj);
        }

        ClearSpawnedOnly();
    }

    public void Clear()
    {
        ClearSpawnedOnly();
        for (int i = bakedObjects.Count - 1; i >= 0; i--)
        {
            if (bakedObjects[i] != null) DestroyImmediate(bakedObjects[i]);
        }
        bakedObjects.Clear();
    }

    void ClearSpawnedOnly()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null) DestroyImmediate(spawnedObjects[i]);
        }
        spawnedObjects.Clear();

        // Fallback cleanup
        var children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (!bakedObjects.Contains(child.gameObject))
                children.Add(child.gameObject);
        }
        children.ForEach(child => DestroyImmediate(child));
    }
}

// --- EDITOR SCRIPT ---
#if UNITY_EDITOR
[CustomEditor(typeof(BuildingGen))]
public class BuildingGenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        BuildingGen script = (BuildingGen)target;
        GUILayout.Space(10);
        
        GUILayout.Label("Actions", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1. Generate", GUILayout.Height(30)))
        {
            script.Generate();
        }
        if (GUILayout.Button("2. Bake (Fix)", GUILayout.Height(30)))
        {
            script.Bake();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear All"))
        {
            script.Clear();
        }
    }
}
#endif