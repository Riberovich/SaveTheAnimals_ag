using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pseudo-physical simulation for balloons with upward force, collision, and rope constraint.
/// Part of M1/A3.2: Pseudo-Physical Balloon System
/// </summary>
public class BalloonPhysics : MonoBehaviour
{
    [Header("Forces")]
    [Tooltip("Upward lift force this balloon provides (how much it wants to fly up)")]
    [SerializeField] private float liftForce = 5.0f;

    [Tooltip("Damping to prevent jittery movement (0-1, higher = more damping)")]
    [SerializeField] [Range(0f, 1f)] private float damping = 0.85f;

    [Header("Collision")]
    [Tooltip("Collision radius (percentage of balloon visual size, e.g., 0.75 = 75%)")]
    [SerializeField] [Range(0.1f, 1f)] private float collisionRadiusMultiplier = 0.75f;

    [Tooltip("How hard balloons push each other away on collision")]
    [SerializeField] private float collisionRepulsion = 2.0f;

    [Header("Rope Constraint")]
    [Tooltip("Maximum distance from animal (rope length)")]
    [SerializeField] private float maxRopeLength = 3.0f;

    [Tooltip("How strongly the rope pulls the balloon toward the animal")]
    [SerializeField] private float ropeStiffness = 8.0f;

    [Header("Center Attraction")]
    [Tooltip("Force pulling balloons toward center position above animal")]
    [SerializeField] private float centerAttractionForce = 1.5f;

    // Runtime state
    private Vector2 velocity = Vector2.zero;
    private Transform animalTransform;
    private Vector3 desiredCenterPosition;
    private float balloonRadius;
    private bool isActive = true;

    // Reference to manager for collision detection
    private BalloonManager balloonManager;

    private void Start()
    {
        // Calculate balloon radius from scale
        balloonRadius = transform.localScale.x * 0.5f;

        // Find balloon manager
        balloonManager = FindObjectOfType<BalloonManager>();
        if (balloonManager != null)
        {
            balloonManager.RegisterBalloon(this);
        }

        // Find animal
        GameObject animal = GameObject.Find("Animal");
        if (animal != null)
        {
            animalTransform = animal.transform;
        }
    }

    private void FixedUpdate()
    {
        if (!isActive || animalTransform == null) return;

        // Update desired center position (directly above animal)
        UpdateDesiredCenterPosition();

        // Apply forces
        ApplyForces();

        // Apply collision with other balloons
        ApplyBalloonCollisions();

        // Apply rope constraint
        ApplyRopeConstraint();

        // Apply damping
        velocity *= damping;

        // Update position
        Vector3 newPosition = transform.position + (Vector3)velocity * Time.fixedDeltaTime;
        transform.position = newPosition;
    }

    /// <summary>
    /// Updates the desired center position (where balloons want to gather)
    /// </summary>
    private void UpdateDesiredCenterPosition()
    {
        if (animalTransform != null)
        {
            // Center position is directly above the animal at rope length distance
            desiredCenterPosition = animalTransform.position + Vector3.up * (maxRopeLength * 0.7f);
        }
    }

    /// <summary>
    /// Applies upward lift force and center attraction
    /// </summary>
    private void ApplyForces()
    {
        // Upward lift (balloons want to fly up)
        Vector2 lift = Vector2.up * liftForce;

        // Attraction toward center position
        Vector2 toCenter = (desiredCenterPosition - transform.position);
        Vector2 centerForce = toCenter.normalized * centerAttractionForce;

        // Combine forces
        velocity += (lift + centerForce) * Time.fixedDeltaTime;
    }

    /// <summary>
    /// Detects and resolves collisions with other balloons
    /// </summary>
    private void ApplyBalloonCollisions()
    {
        if (balloonManager == null) return;

        List<BalloonPhysics> otherBalloons = balloonManager.GetAllBalloons();
        float collisionRadius = balloonRadius * collisionRadiusMultiplier;

        foreach (BalloonPhysics other in otherBalloons)
        {
            if (other == this || !other.isActive) continue;

            Vector2 toOther = other.transform.position - transform.position;
            float distance = toOther.magnitude;
            float minDistance = collisionRadius + (other.balloonRadius * other.collisionRadiusMultiplier);

            // Check collision
            if (distance < minDistance && distance > 0.01f)
            {
                // Collision detected - push balloons apart
                Vector2 pushDirection = -toOther.normalized;
                float overlap = minDistance - distance;
                Vector2 repulsionForce = pushDirection * overlap * collisionRepulsion;

                velocity += repulsionForce * Time.fixedDeltaTime;
            }
        }
    }

    /// <summary>
    /// Constrains balloon to stay within rope length from animal
    /// </summary>
    private void ApplyRopeConstraint()
    {
        if (animalTransform == null) return;

        Vector2 toAnimal = animalTransform.position - transform.position;
        float distance = toAnimal.magnitude;

        // If beyond rope length, pull back toward animal
        if (distance > maxRopeLength)
        {
            Vector2 pullForce = toAnimal.normalized * (distance - maxRopeLength) * ropeStiffness;
            velocity += pullForce * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Applies a shockwave impulse to this balloon (called when nearby balloon pops)
    /// </summary>
    public void ApplyShockwave(Vector3 shockwaveOrigin, float shockwaveStrength)
    {
        if (!isActive) return;

        Vector2 awayFromOrigin = (transform.position - shockwaveOrigin).normalized;
        velocity += awayFromOrigin * shockwaveStrength;
    }

    /// <summary>
    /// Gets the balloon's current lift force contribution
    /// </summary>
    public float GetLiftForce()
    {
        return liftForce;
    }

    /// <summary>
    /// Gets the balloon's collision radius
    /// </summary>
    public float GetCollisionRadius()
    {
        return balloonRadius * collisionRadiusMultiplier;
    }

    /// <summary>
    /// Gets the rope attachment point (bottom center of balloon)
    /// </summary>
    public Vector3 GetRopeAttachmentPoint()
    {
        return transform.position + Vector3.down * balloonRadius;
    }

    /// <summary>
    /// Sets the animal reference (called by spawner)
    /// </summary>
    public void SetAnimal(Transform animal)
    {
        animalTransform = animal;
    }

    /// <summary>
    /// Deactivates physics (called when balloon is popping)
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
    }

    private void OnDestroy()
    {
        // Unregister from manager
        if (balloonManager != null)
        {
            balloonManager.UnregisterBalloon(this);
        }
    }

    /// <summary>
    /// Debug visualization in Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw collision radius
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, balloonRadius * collisionRadiusMultiplier);

        // Draw rope length constraint
        if (animalTransform != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(animalTransform.position, maxRopeLength);

            // Draw velocity vector
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)velocity);
        }

        // Draw desired center position
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(desiredCenterPosition, 0.2f);
    }
}
