using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float groundAcceleration = 45f;
    public float groundDeceleration = 55f;
    public float iceAcceleration = 6f;
    public float iceDeceleration = 2f;

    [Header("Gravity")]
    public float gravityForce = 80f;
    public float groundProbeDistance = 0.35f;
    public float groundProbeInset = 0.02f;
    public float snapSkin = 0.01f;
    public LayerMask groundLayer;

    [Header("Ground Stability")]
    public int requiredSupportHits = 2;
    public float sideProbeFraction = 0.18f;
    [Header("Visual")]
    public Transform visual;

    private Rigidbody rb;
    private Collider bodyCollider;

    private Vector3 spawnPoint;
    private bool gravityFlipped = false;
    private bool isGrounded = false;
    private bool canFlip = false;
    private bool onIce = false;

    private RaycastHit groundHit;
    private bool hasGroundHit = false;

    private Transform currentPlatform;
    private Vector3 lastPlatformPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

        bodyCollider = FindBodyCollider();

        if (visual == null)
            visual = transform.Find("Visual");
    }

    void Start()
    {
        spawnPoint = transform.position;
    }

    void Update()
    {
        RefreshGroundState();
        if (Input.GetKeyDown(KeyCode.Space) && canFlip)
        {
            FlipGravity();
        }
    }

    void FixedUpdate()
    {
        RefreshGroundState();

        FollowMovingPlatform();
        MoveSideways();
        ApplyGravity();
        SnapToSurface();
    }

    private void RefreshGroundState()
    {
        if (bodyCollider == null)
        {
            isGrounded = false;
            canFlip = false;
            onIce = false;
            hasGroundHit = false;
            currentPlatform = null;
            return;
        }

        Vector3 gravityDir = gravityFlipped ? Vector3.up : Vector3.down;
        Bounds b = bodyCollider.bounds;

        // Small radius based on the player's width, not height.
        float radius = Mathf.Max(0.05f, Mathf.Min(b.extents.x, b.extents.z) * 0.45f);

        // Start just outside the player on the opposite side of gravity.
        Vector3 castOrigin = b.center - gravityDir * (b.extents.y + 0.02f);

        // Cast far enough to reach the surface below/above the player.
        float castDistance = b.extents.y + groundProbeDistance + 0.05f;

        hasGroundHit = Physics.SphereCast(
            castOrigin,
            radius,
            gravityDir,
            out groundHit,
            castDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (hasGroundHit)
        {
            float surfaceFacing = Vector3.Dot(groundHit.normal, -gravityDir);

            isGrounded = surfaceFacing > 0.5f;
            canFlip = isGrounded;
            onIce = groundHit.collider.CompareTag("Ice");

            if (!isGrounded)
                hasGroundHit = false;
        }
        else
        {
            isGrounded = false;
            canFlip = false;
            onIce = false;
            currentPlatform = null;
        }
    }


    private void MoveSideways()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        bool hasInput = Mathf.Abs(moveInput) > 0.01f;

        float accel = onIce ? iceAcceleration : groundAcceleration;
        float decel = onIce ? iceDeceleration : groundDeceleration;

        Vector3 velocity = rb.velocity;
        float targetX = moveInput * moveSpeed;

        if (hasInput)
            velocity.x = Mathf.MoveTowards(velocity.x, targetX, accel * Time.fixedDeltaTime);
        else
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, decel * Time.fixedDeltaTime);

        velocity.z = 0f;
        rb.velocity = velocity;
    }

    private void ApplyGravity()
    {
        Vector3 gravityDir = gravityFlipped ? Vector3.up : Vector3.down;

        if (!isGrounded)
        {
            rb.velocity += gravityDir * gravityForce * Time.fixedDeltaTime;
        }
        else
        {
            Vector3 v = rb.velocity;
            float gravityComponent = Vector3.Dot(v, gravityDir);
            v -= gravityDir * gravityComponent;
            rb.velocity = v;
        }
    }

    private void SnapToSurface()
    {
        if (!isGrounded || !hasGroundHit || bodyCollider == null)
            return;

        Vector3 gravityDir = gravityFlipped ? Vector3.up : Vector3.down;
        float halfHeight = bodyCollider.bounds.extents.y;

        Vector3 snappedPosition = groundHit.point + groundHit.normal * (halfHeight + snapSkin);

        Vector3 pos = rb.position;
        pos.y = snappedPosition.y;
        rb.MovePosition(pos);

        Vector3 v = rb.velocity;
        float gravityComponent = Vector3.Dot(v, gravityDir);
        v -= gravityDir * gravityComponent;
        rb.velocity = v;
    }

    private void FollowMovingPlatform()
    {
        if (!isGrounded || !hasGroundHit)
        {
            currentPlatform = null;
            return;
        }

        if (!groundHit.collider.CompareTag("MovingPlatform"))
        {
            currentPlatform = null;
            return;
        }

        Transform platform = groundHit.collider.transform;

        if (currentPlatform != platform)
        {
            currentPlatform = platform;
            lastPlatformPosition = platform.position;
            return;
        }

        Vector3 delta = platform.position - lastPlatformPosition;

        if (delta.sqrMagnitude > 0f)
        {
            rb.MovePosition(rb.position + delta);
        }

        lastPlatformPosition = platform.position;
    }

    private void FlipGravity()
    {
        gravityFlipped = !gravityFlipped;

        Vector3 v = rb.velocity;
        v.y = 0f;
        rb.velocity = v;

        if (visual != null)
        {
            visual.localRotation = gravityFlipped
                ? Quaternion.Euler(0f, 0f, 180f)
                : Quaternion.identity;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spike"))
        {
            Die();
            return;
        }

        if (other.CompareTag("Checkpoint"))
        {
            SetCheckpoint(other.transform);
            return;
        }

        if (other.CompareTag("LevelExit"))
        {
            LoadNextLevel();
            return;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Spike"))
        {
            Die();
        }
    }
    private void Die()
    {
        transform.SetParent(null, true);
        currentPlatform = null;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = spawnPoint;

        gravityFlipped = false;

        if (visual != null)
            visual.localRotation = Quaternion.identity;

        RefreshGroundState();
    }

    private void SetCheckpoint(Transform checkpoint)
    {
        spawnPoint = checkpoint.position;

        Checkpoint cp = checkpoint.GetComponent<Checkpoint>();
        if (cp != null)
            cp.Activate();
    }

    private void LoadNextLevel()
    {
        int index = SceneManager.GetActiveScene().buildIndex;
        if (index + 1 < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(index + 1);
        }
    }

    private Collider FindBodyCollider()
    {
        Collider[] cols = GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++)
        {
            if (!cols[i].isTrigger)
                return cols[i];
        }
        return null;
    }
}