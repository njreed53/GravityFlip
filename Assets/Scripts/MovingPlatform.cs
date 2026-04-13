using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector3 pointA;
    public Vector3 pointB;
    public float speed = 2f;

    private Vector3 target;

    void Start()
    {
        target = pointB; // Start at pointB
    }

    void Update()
    {
        // Move platform towards the target point
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        // Check if platform is close enough to the target point to switch
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            target = target == pointA ? pointB : pointA; // Switch target
        }
    }
}