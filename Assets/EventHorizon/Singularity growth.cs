using UnityEngine;

public class Singularitygrowth : MonoBehaviour
{
    public Vector3 targetScale = new Vector3(2000f, 2000f, 2000f);
    public float speed = 0.5f;

    private Vector3 startScale;
    private float elapsedTime = 0f;

    void Start()
    {
        startScale = transform.localScale;   // ← Use whatever scale is in the Inspector
    }

    void Update()
    {
        elapsedTime += Time.deltaTime * speed;
        transform.localScale = Vector3.Lerp(
            startScale,
            targetScale,
            elapsedTime
        );
    }
}