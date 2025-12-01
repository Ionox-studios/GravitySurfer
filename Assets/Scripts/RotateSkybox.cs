using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    public float rotationSpeed = 0.5f; // Adjust this value to control rotation speed

    void Update()
    {
        // Check if a skybox material is assigned in RenderSettings
        if (RenderSettings.skybox != null)
        {
            // Rotate the skybox around the Y-axis
            RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationSpeed);
        }
    }
}