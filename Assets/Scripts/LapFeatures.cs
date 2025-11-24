using UnityEngine;

/// <summary>
/// Container for game objects/features that should be active during a specific lap
/// Add this to your GameController's lap features array
/// </summary>
[System.Serializable]
public class LapFeatures : MonoBehaviour
{
    [Header("Features for this Lap")]
    [SerializeField] private GameObject[] objectsToActivate;
    [Tooltip("Game objects that will be enabled during this lap")]
    
    [SerializeField] private GameObject[] objectsToDeactivate;
    [Tooltip("Game objects that will be disabled during this lap")]

    /// <summary>
    /// Activate all features for this lap
    /// </summary>
    public void Activate()
    {
        // Enable objects
        if (objectsToActivate != null)
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"Activated: {obj.name}");
                }
            }
        }

        // Disable objects
        if (objectsToDeactivate != null)
        {
            foreach (var obj in objectsToDeactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"Deactivated: {obj.name}");
                }
            }
        }
    }

    /// <summary>
    /// Deactivate all features for this lap
    /// </summary>
    public void Deactivate()
    {
        // Reverse the activation
        if (objectsToActivate != null)
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        // Re-enable the objects that were supposed to be disabled
        if (objectsToDeactivate != null)
        {
            foreach (var obj in objectsToDeactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
    }
}
