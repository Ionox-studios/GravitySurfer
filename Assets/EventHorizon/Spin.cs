    using UnityEngine;

    public class Rotator : MonoBehaviour
    {
        public float rotationSpeed = 20f; // Adjust this value to control rotation speed

        void Update()
        {

            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

            // You can also rotate around other axes:
            // transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime); // X-axis
            // transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime); // Z-axis
        }
    }
