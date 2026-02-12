using UnityEngine;

/// <summary>
/// Pseudo-physical simulation for the animal affected by balloon lift forces and gravity.
/// The animal is pulled up by balloons but held down by its own weight.
/// Part of M1/A3.2: Pseudo-Physical Balloon System
/// </summary>
public class AnimalPhysics : MonoBehaviour
{
    [Header("Mass Settings")]
    [Tooltip("Mass of the animal (higher = harder for balloons to lift)")]
    [SerializeField] private float mass = 2.0f;

    [Tooltip("Gravity force pulling the animal down")]
    [SerializeField] private float gravity = 9.8f;

    [Header("Movement")]
    [Tooltip("Damping to smooth animal movement (0-1, higher = more damping)")]
    [SerializeField] [Range(0f, 1f)] private float damping = 0.9f;

    [Tooltip("Maximum vertical velocity (prevents extreme speeds)")]
    [SerializeField] private float maxVerticalVelocity = 5.0f;

    [Header("Ground")]
    [Tooltip("Ground Y position (animal can't go below this)")]
    [SerializeField] private float groundLevel = -4.0f;

    [Tooltip("Bounce factor when hitting ground (0 = no bounce, 1 = full bounce)")]
    [SerializeField] [Range(0f, 0.5f)] private float groundBounce = 0.2f;

    // Runtime state
    private Vector2 velocity = Vector2.zero;
    private BalloonManager balloonManager;
    private float currentLiftForce = 0f;
    private bool isGrounded = false;

    private void Start()
    {
        // Find balloon manager
        balloonManager = FindObjectOfType<BalloonManager>();
    }

    private void FixedUpdate()
    {
        // Calculate total lift from all active balloons
        CalculateLiftForce();

        // Apply forces
        ApplyForces();

        // Apply damping
        velocity *= damping;

        // Clamp vertical velocity
        velocity.y = Mathf.Clamp(velocity.y, -maxVerticalVelocity, maxVerticalVelocity);

        // Update position
        Vector3 newPosition = transform.position + (Vector3)velocity * Time.fixedDeltaTime;

        // Ground collision
        if (newPosition.y < groundLevel)
        {
            newPosition.y = groundLevel;

            // Bounce
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

        transform.position = newPosition;
    }

    /// <summary>
    /// Calculates total lift force from all connected balloons
    /// </summary>
    private void CalculateLiftForce()
    {
        currentLiftForce = 0f;

        if (balloonManager != null)
        {
            var balloons = balloonManager.GetAllBalloons();
            foreach (var balloon in balloons)
            {
                currentLiftForce += balloon.GetLiftForce();
            }
        }
    }

    /// <summary>
    /// Applies gravity and lift forces to the animal
    /// </summary>
    private void ApplyForces()
    {
        // Gravity pulls down (scaled by mass)
        float gravityForce = -gravity * mass;

        // Lift from balloons pushes up
        float netForce = gravityForce + currentLiftForce;

        // Apply net force (F = ma, so a = F/m)
        float acceleration = netForce / mass;
        velocity.y += acceleration * Time.fixedDeltaTime;
    }

    /// <summary>
    /// Gets the rope attachment point (center of animal)
    /// </summary>
    public Vector3 GetRopeAttachmentPoint()
    {
        return transform.position;
    }

    /// <summary>
    /// Sets the animal's mass
    /// </summary>
    public void SetMass(float newMass)
    {
        mass = Mathf.Max(0.1f, newMass); // Prevent zero or negative mass
    }

    /// <summary>
    /// Gets current mass
    /// </summary>
    public float GetMass()
    {
        return mass;
    }

    /// <summary>
    /// Gets current lift force from balloons
    /// </summary>
    public float GetCurrentLiftForce()
    {
        return currentLiftForce;
    }

    /// <summary>
    /// Gets the net force (positive = up, negative = down)
    /// </summary>
    public float GetNetForce()
    {
        return (-gravity * mass) + currentLiftForce;
    }

    /// <summary>
    /// Checks if animal is on the ground
    /// </summary>
    public bool IsGrounded()
    {
        return isGrounded;
    }

    /// <summary>
    /// Debug visualization
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw ground level
        Gizmos.color = new Color(0.5f, 0.3f, 0.1f, 0.5f);
        Gizmos.DrawLine(new Vector3(-10, groundLevel, 0), new Vector3(10, groundLevel, 0));

        // Draw velocity vector
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)velocity);

        // Draw force indicators
        Vector3 pos = transform.position;

        // Gravity force (red, downward)
        float gravityViz = (gravity * mass) * 0.1f;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, pos + Vector3.down * gravityViz);

        // Lift force (green, upward)
        float liftViz = currentLiftForce * 0.1f;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + Vector3.up * liftViz);

        // Net force (yellow)
        float netForceViz = GetNetForce() * 0.1f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos + Vector3.right * 0.3f, pos + Vector3.right * 0.3f + Vector3.up * netForceViz);
    }
}
