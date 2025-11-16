using UnityEngine;

public class HaloRotate : MonoBehaviour
{
    [Header("Rotation Speed (degrees per second)")]
    public float rotationSpeed = 45f;

    void Update()
    {
        // Rotate around the Z-axis only
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}