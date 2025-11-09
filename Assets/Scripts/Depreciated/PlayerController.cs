using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController _characterController;
    private Transform _transform;
    public float MoveSpeed = 5f;
    public float RotateSpeed = 100f;
    private float rotationY;

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        // Cache the transform to avoid repeated property access
        _transform = transform;
    }

    public void Move(Vector2 movementVector)
    {
        // Use cached transform and correct variable name
        Vector3 move = _transform.right * movementVector.x + _transform.forward * movementVector.y;
        move = move * MoveSpeed * Time.deltaTime;
        if (_characterController != null)
            _characterController.Move(move);
    }

    public void Rotate(Vector2 rotationVector)
    {
        // Fix rotation variable usage and apply to cached transform
        rotationY += rotationVector.x * RotateSpeed * Time.deltaTime;
        _transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
    }
}
