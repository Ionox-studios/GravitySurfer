    using UnityEngine;

    public class OrbitScript : MonoBehaviour
    {
        public Transform targetPoint; // The point to rotate around
        public float rotationSpeed = 2f; // Speed of rotation
        public Vector3 rotationAxis = Vector3.right; // Axis of rotation (e.g., Vector3.up for Y-axis)

        void Update()
        {
            // Rotate around the target point
            transform.RotateAround(targetPoint.position, rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }