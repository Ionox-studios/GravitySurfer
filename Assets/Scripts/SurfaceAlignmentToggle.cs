using UnityEngine;

/// <summary>
/// Helper component to easily toggle between standard hover and surface-sticking modes
/// Add this to your player vehicle for easy testing
/// </summary>
public class SurfaceAlignmentToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BarycentricAlignment barycentricAlignment;
    [SerializeField] private HoverVehicleController hoverController;
    
    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private bool startWithAlignmentEnabled = false;
    
    [Header("UI (Optional)")]
    [SerializeField] private TMPro.TextMeshProUGUI statusText; // Optional UI text
    
    private bool isAlignmentMode = false;
    
    void Start()
    {
        // Auto-find components if not assigned
        if (barycentricAlignment == null)
            barycentricAlignment = GetComponent<BarycentricAlignment>();
        
        if (hoverController == null)
            hoverController = GetComponent<HoverVehicleController>();
        
        // Set initial mode
        SetAlignmentMode(startWithAlignmentEnabled);
    }
    
    void Update()
    {
        // Toggle with key press
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMode();
        }
        
        UpdateUI();
    }
    
    public void ToggleMode()
    {
        SetAlignmentMode(!isAlignmentMode);
    }
    
    public void SetAlignmentMode(bool enabled)
    {
        isAlignmentMode = enabled;
        
        if (barycentricAlignment != null)
        {
            barycentricAlignment.SetAlignmentEnabled(enabled);
        }
        
        string mode = enabled ? "Surface Sticking Mode" : "Standard Hover Mode";
        Debug.Log($"Switched to {mode}");
    }
    
    void UpdateUI()
    {
        if (statusText == null) return;
        
        string mode = isAlignmentMode ? "SURFACE MODE" : "HOVER MODE";
        string stickStatus = "";
        
        if (isAlignmentMode && barycentricAlignment != null)
        {
            //stickStatus = barycentricAlignment.IsStuckToSurface ? " [STUCK]" : " [FREE]";
        }
        
        statusText.text = $"{mode}{stickStatus}\nPress {toggleKey} to toggle";
    }
    
    // Public method to check current mode
    public bool IsInSurfaceMode => isAlignmentMode;
}
