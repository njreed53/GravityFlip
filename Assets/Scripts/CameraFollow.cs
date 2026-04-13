using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Only follow the target horizontally, keep y fixed, and use a fixed z value
        Vector3 targetPos = new Vector3(target.position.x, transform.position.y, transform.position.z);

        // Smoothly move the camera to the target position
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );
    }
}