using UnityEngine;

/// <summary>
/// Cartoon-style animal follower: the animal is PASSIVE, dragged by balloons.
/// Follows balloon cluster center with delay, inertia, overshoot, and pendulum swing.
/// Uses spring-damper math — no Rigidbody, no real physics.
/// Tilts slightly in the direction of movement for a "dragged" feel.
/// </summary>
public class AnimalPhysics : MonoBehaviour
{
    [Header("Mass & Follow")]
    [Tooltip("Animal mass — higher = more inertia, slower to follow")]
    [SerializeField] private float animalMass = 1.5f;

    [Tooltip("Follow lag — how long the animal takes to catch up (SmoothDamp time)")]
    [SerializeField] private float followLag = 0.5f;

    [Tooltip("Vertical offset below balloon cluster (simulates weight/gravity)")]
    [SerializeField] private float gravityOffset = 2.0f;

    [Header("Spring (Overshoot & Swing)")]
    [Tooltip("Spring stiffness — higher = snappier response")]
    [SerializeField] private float springStiffness = 8.0f;

    [Tooltip("Swing damping — lower = more swing (0.3–0.8 recommended)")]
    [SerializeField] [Range(0.1f, 1f)] private float swingDamping = 0.5f;

    [Tooltip("Max velocity to prevent extreme movement")]
    [SerializeField] private float maxVelocity = 6.0f;

    [Header("Tilt")]
    [Tooltip("How much the animal tilts when moving sideways")]
    [SerializeField] private float tiltSensitivity = 8.0f;

    [Tooltip("Max tilt angle in degrees")]
    [SerializeField] private float maxTilt = 12.0f;

    [Tooltip("Tilt smoothing (SmoothDamp time)")]
    [SerializeField] private float tiltSmoothing = 0.2f;

    [Header("Ground")]
    [Tooltip("Ground Y position")]
    [SerializeField] private float groundLevel = -4.0f;

    [Tooltip("Bounce factor when hitting ground")]
    [SerializeField] [Range(0f, 0.5f)] private float groundBounce = 0.15f;

    [Header("Attach Point")]
    [Tooltip("Local offset where ropes connect (center/back of animal)")]
    [SerializeField] private Vector2 ropeAttachOffset = Vector2.zero;

    // Runtime state
    private BalloonManager balloonManager;
    private Vector2 velocity = Vector2.zero;
    private bool isFrozen = false; // Physics live from spawn — animal follows balloons immediately
    private bool isGrounded = false;

    // Tilt state
    private float currentTilt;
    private float tiltVelocity;

    private void Start()
    {
        balloonManager = FindObjectOfType<BalloonManager>();
    }

    private void Update()
    {
        if (isFrozen) return;

        float dt = Time.deltaTime;
        if (dt < 0.0001f) return;

        // Compute target position from balloon cluster
        Vector3 targetPos = ComputeTargetPosition();

        // Spring-damper: displacement → force → acceleration → velocity → position
        Vector2 currentPos = transform.position;
        Vector2 target2D = targetPos;
        Vector2 displacement = target2D - currentPos;

        // Spring force (pulls toward target)
        Vector2 springForce = displacement * springStiffness;

        // Damping force (resists velocity → creates swing when underdamped)
        Vector2 dampingForce = -velocity * swingDamping * 2f * Mathf.Sqrt(springStiffness * animalMass);

        // Acceleration = force / mass
        Vector2 acceleration = (springForce + dampingForce) / animalMass;

        // Integrate
        velocity += acceleration * dt;

        // Clamp velocity
        if (velocity.magnitude > maxVelocity)
        {
            velocity = velocity.normalized * maxVelocity;
        }

        Vector2 newPos = currentPos + velocity * dt;

        // Ground collision
        if (newPos.y < groundLevel)
        {
            newPos.y = groundLevel;
            if (velocity.y < 0)
            {
                velocity.y = -velocity.y * groundBounce;
            }
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        transform.position = new Vector3(newPos.x, newPos.y, 0f);

        // Tilt in direction of movement
        UpdateTilt(dt);
    }

    private Vector3 ComputeTargetPosition()
    {
        if (balloonManager == null || balloonManager.GetBalloonCount() == 0)
        {
            // No balloons — fall to ground
            return new Vector3(transform.position.x, groundLevel, 0f);
        }

        Vector3 clusterCenter = balloonManager.GetClusterCenter();

        // Target = below cluster center by gravityOffset (simulates hanging weight)
        return clusterCenter + Vector3.down * gravityOffset;
    }

    private void UpdateTilt(float dt)
    {
        // Tilt based on horizontal velocity (dragged feeling)
        float targetTilt = Mathf.Clamp(-velocity.x * tiltSensitivity, -maxTilt, maxTilt);
        currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, tiltSmoothing);
        transform.rotation = Quaternion.Euler(0, 0, currentTilt);
    }

    /// <summary>
    /// Gets the world-space rope attachment point (center/back of animal)
    /// </summary>
    public Vector3 GetRopeAttachmentPoint()
    {
        return transform.position + (Vector3)ropeAttachOffset;
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (!frozen)
        {
            velocity = Vector2.zero;
        }
    }

    public bool IsFrozen()
    {
        return isFrozen;
    }

    public void SetMass(float newMass)
    {
        animalMass = Mathf.Max(0.1f, newMass);
    }

    public float GetMass()
    {
        return animalMass;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Ground level
        Gizmos.color = new Color(0.5f, 0.3f, 0.1f, 0.5f);
        Gizmos.DrawLine(new Vector3(-10, groundLevel, 0), new Vector3(10, groundLevel, 0));

        // Velocity vector
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)velocity * 0.3f);

        // Rope attach point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetRopeAttachmentPoint(), 0.08f);

        // Target position
        if (balloonManager != null && balloonManager.GetBalloonCount() > 0)
        {
            Vector3 target = ComputeTargetPosition();
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(target, 0.1f);
            Gizmos.DrawLine(transform.position, target);
        }
    }
}
