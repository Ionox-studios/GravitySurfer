using UnityEngine;

/// <summary>
/// Generates wave data for deforming road mesh
/// CPU-friendly using simple sine waves
/// </summary>
public class WaveGenerator : MonoBehaviour
{
    [Header("Wave Properties")]
    [SerializeField] private float waveHeight = 1f;
    [SerializeField] private float waveLength = 10f;
    [SerializeField] private float waveSpeed = 5f;
    [SerializeField] private Vector2 waveDirection = new Vector2(0, 1); // Direction waves travel
    
    [Header("Multiple Waves (for variety)")]
    [SerializeField] private bool useMultipleWaves = true;
    [SerializeField] private WaveData[] additionalWaves = new WaveData[]
    {
        new WaveData { amplitude = 0.5f, wavelength = 15f, speed = 3f, direction = new Vector2(0.3f, 1f) },
        new WaveData { amplitude = 0.3f, wavelength = 8f, speed = 7f, direction = new Vector2(-0.2f, 1f) }
    };
    
    private float time;
    
    void Update()
    {
        time += Time.deltaTime;
    }
    
    /// <summary>
    /// Calculate wave height at a given world position
    /// </summary>
    public float GetWaveHeight(Vector3 worldPos)
    {
        float height = 0f;
        
        // Main wave
        Vector2 pos2D = new Vector2(worldPos.x, worldPos.z);
        Vector2 normDir = waveDirection.normalized;
        float projection = Vector2.Dot(pos2D, normDir);
        float wave = Mathf.Sin((projection / waveLength + time * waveSpeed) * Mathf.PI * 2f);
        height += wave * waveHeight;
        
        // Additional waves for more organic feel
        if (useMultipleWaves)
        {
            foreach (var waveData in additionalWaves)
            {
                Vector2 dir = waveData.direction.normalized;
                float proj = Vector2.Dot(pos2D, dir);
                float w = Mathf.Sin((proj / waveData.wavelength + time * waveData.speed) * Mathf.PI * 2f);
                height += w * waveData.amplitude;
            }
        }
        
        return height;
    }
    
    /// <summary>
    /// Calculate normal vector at a position (for proper lighting/physics)
    /// Uses simple derivative approximation
    /// </summary>
    public Vector3 GetWaveNormal(Vector3 worldPos, float epsilon = 0.1f)
    {
        float h = GetWaveHeight(worldPos);
        float hx = GetWaveHeight(worldPos + Vector3.right * epsilon);
        float hz = GetWaveHeight(worldPos + Vector3.forward * epsilon);
        
        Vector3 tangentX = new Vector3(epsilon, hx - h, 0).normalized;
        Vector3 tangentZ = new Vector3(0, hz - h, epsilon).normalized;
        
        return Vector3.Cross(tangentZ, tangentX).normalized;
    }
    
    /// <summary>
    /// Get wave velocity at a position (for surfing physics)
    /// </summary>
    public Vector3 GetWaveVelocity(Vector3 worldPos)
    {
        // Simple approximation: derivative of height over time
        float currentHeight = GetWaveHeight(worldPos);
        
        // Calculate height at next frame
        float oldTime = time;
        time += 0.016f; // Approximate frame time
        float nextHeight = GetWaveHeight(worldPos);
        time = oldTime;
        
        float verticalVelocity = (nextHeight - currentHeight) / 0.016f;
        
        return new Vector3(
            waveDirection.normalized.x * waveSpeed,
            verticalVelocity,
            waveDirection.normalized.y * waveSpeed
        );
    }
    
    public void SetWaveDirection(Vector2 direction)
    {
        waveDirection = direction;
    }
    
    public void SetWaveHeight(float height)
    {
        waveHeight = height;
    }
    
    void OnDrawGizmos()
    {
        // Draw a preview of wave direction
        if (waveDirection.magnitude > 0.01f)
        {
            Vector3 pos = transform.position;
            Vector3 dir3D = new Vector3(waveDirection.x, 0, waveDirection.y).normalized;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(pos, dir3D * 5f);
            Gizmos.DrawSphere(pos + dir3D * 5f, 0.3f);
            
            // Draw wave peaks
            for (int i = 0; i < 5; i++)
            {
                Vector3 wavePos = pos + dir3D * i * waveLength;
                Gizmos.color = Color.Lerp(Color.cyan, Color.blue, i / 5f);
                Gizmos.DrawWireSphere(wavePos, 0.5f);
            }
        }
    }
}

[System.Serializable]
public class WaveData
{
    public float amplitude = 1f;
    public float wavelength = 10f;
    public float speed = 5f;
    public Vector2 direction = Vector2.up;
}
