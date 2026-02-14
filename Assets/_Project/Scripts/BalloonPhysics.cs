using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Cartoon-style balloon behavior: balloons are LEADERS that actively move.
/// Float upward, cluster together, repel each other, rotate bottom toward animal.
/// Supports fly-in: when a fly-in target is set, balloon SmoothDamps toward it
/// (rope constraint is disabled so balloon can fly ahead of the animal).
/// All movement via Lerp/SmoothDamp — no Rigidbody, no real physics.
/// </summary>
public class BalloonPhysics : MonoBehaviour
{
    [Header("Pull Force")]
    [Tooltip("How strongly this balloon wants to float upward")]
    [SerializeField] private float balloonPullForce = 3.0f;

    [Tooltip("SmoothDamp time for position (lower = snappier, higher = floatier)")]
    [SerializeField] private float moveSmoothTime = 0.35f;

    [Header("Cluster")]
    [Tooltip("How strongly balloons attract toward cluster center")]
    [SerializeField] private float centerAttraction = 2.0f;

    [Header("Collision")]
    [Tooltip("Collision radius as fraction of visual size (0.75 = 75%)")]
    [SerializeField] [Range(0.1f, 1f)] private float collisionRadiusMultiplier = 0.75f;

    [Tooltip("How hard balloons push each other apart")]
    [SerializeField] private float repulsionStrength = 3.0f;

    [Header("Rope Constraint")]
    [Tooltip("Maximum distance from animal (rope length)")]
    [SerializeField] private float maxRopeLength = 3.0f;

    [Header("Rotation")]
    [Tooltip("How smoothly balloon rotates to face animal (lower = faster)")]
    [SerializeField] private float rotationSmoothness = 0.15f;

    [Tooltip("Max tilt angle in degrees")]
    [SerializeField] private float maxTiltAngle = 25f;

    [Header("Random Drift")]
    [Tooltip("Strength of gentle random sideways drift")]
    [SerializeField] private float driftStrength = 0.4f;

    [Tooltip("Speed of drift oscillation")]
    [SerializeField] private float driftSpeed = 0.6f;

    [Header("Attach Point")]
    [Tooltip("Local offset for rope attachment (bottom of balloon)")]
    [SerializeField] private Vector2 ropeAttachOffset = new Vector2(0, -0.5f);

    // Runtime state
    private Transform animalTransform;
    private BalloonManager balloonManager;
    private float balloonRadius;
    private bool isActive = true;
    private bool isFrozen = false; // No longer starts frozen — physics live from spawn

    // SmoothDamp state
    private Vector2 currentVelocity;

    // Rotation state
    private float currentAngle;
    private float angularVelocity;

    // Drift
    private float driftPhase;

    // Shockwave
    private Vector3 shockwaveOffset;

    // Fly-in target: when set, balloon SmoothDamps toward this position
    // and rope constraint is disabled so it can fly ahead of the animal
    private bool hasFlyInTarget = false;
    private Vector3 flyInTarget;
    private float flyInArrivalThreshold = 0.5f;

    private void Start()
    {
        balloonRadius = transform.localScale.x * 0.5f;
        driftPhase = Random.Range(0f, Mathf.PI * 2f);

        balloonManager = FindObjectOfType<BalloonManager>();
        if (balloonManager != null)
        {
            balloonManager.RegisterBalloon(this);
        }

        if (animalTransform == null)
        {
            GameObject animal = GameObject.Find("Animal");
            if (animal != null) animalTransform = animal.transform;
        }
    }

    private void Update()
    {
        if (!isActive || isFrozen || animalTransform == null) return;

        float dt = Time.deltaTime;
        if (dt < 0.0001f) return;

        // 1. Compute ideal target position (with fly-in blend)
        Vector3 idealPos = ComputeIdealPosition();

        // 2. Add shockwave offset (decays over time)
        idealPos += shockwaveOffset;
        shockwaveOffset = Vector3.Lerp(shockwaveOffset, Vector3.zero, dt * 4f);

        // 3. SmoothDamp toward ideal position
        Vector2 pos2D = transform.position;
        Vector2 ideal2D = idealPos;
        pos2D = Vector2.SmoothDamp(pos2D, ideal2D, ref currentVelocity, moveSmoothTime);

        // 4. Apply collision repulsion (direct offset)
        pos2D += ComputeRepulsion();

        // 5. Rope constraint — ONLY when NOT flying in
        //    During fly-in, balloons must be free to fly ahead of the animal
        if (!hasFlyInTarget)
        {
            pos2D = ApplyRopeConstraint(pos2D);
        }

        transform.position = new Vector3(pos2D.x, pos2D.y, 0f);

        // 6. Rotate bottom toward animal
        UpdateRotation(dt);

        // 7. Check fly-in arrival
        if (hasFlyInTarget)
        {
            float distToTarget = Vector2.Distance(pos2D, (Vector2)flyInTarget);
            if (distToTarget < flyInArrivalThreshold)
            {
                hasFlyInTarget = false;
            }
        }
    }

    private Vector3 ComputeIdealPosition()
    {
        Vector3 animalPos = animalTransform.position;

        // Normal target: above animal at ideal height
        float idealHeight = maxRopeLength * 0.75f;
        Vector3 normalTarget = animalPos + Vector3.up * idealHeight;

        // Center attraction: pull toward cluster center
        if (balloonManager != null)
        {
            Vector3 clusterCenter = balloonManager.GetClusterCenter();
            if (clusterCenter != Vector3.zero)
            {
                Vector3 toCenter = clusterCenter - transform.position;
                normalTarget += toCenter * centerAttraction * Time.deltaTime;
            }
        }

        // Random drift
        float driftX = Mathf.Sin(Time.time * driftSpeed + driftPhase) * driftStrength;
        float driftY = Mathf.Sin(Time.time * driftSpeed * 0.7f + driftPhase + 2f) * driftStrength * 0.3f;
        normalTarget.x += driftX * Time.deltaTime;
        normalTarget.y += driftY * Time.deltaTime;

        // Upward pull bias
        normalTarget.y += balloonPullForce * Time.deltaTime;

        // Fly-in blend: when far from target, use fly-in position;
        // as balloon approaches, smoothly transition to normal behavior
        if (hasFlyInTarget)
        {
            float distToTarget = Vector3.Distance(transform.position, flyInTarget);
            float blendRadius = 2.0f; // start blending within this distance
            float blend = Mathf.Clamp01(distToTarget / blendRadius);
            // blend = 1 when far (use fly-in), 0 when close (use normal)
            return Vector3.Lerp(normalTarget, flyInTarget, blend);
        }

        return normalTarget;
    }

    private Vector2 ComputeRepulsion()
    {
        if (balloonManager == null) return Vector2.zero;

        Vector2 totalRepulsion = Vector2.zero;
        List<BalloonPhysics> others = balloonManager.GetAllBalloons();
        float myRadius = balloonRadius * collisionRadiusMultiplier;

        foreach (BalloonPhysics other in others)
        {
            if (other == this || !other.isActive) continue;

            Vector2 toOther = (Vector2)(other.transform.position - transform.position);
            float dist = toOther.magnitude;
            float minDist = myRadius + other.balloonRadius * other.collisionRadiusMultiplier;

            if (dist < minDist && dist > 0.001f)
            {
                float overlap = minDist - dist;
                Vector2 pushDir = -toOther.normalized;
                totalRepulsion += pushDir * overlap * repulsionStrength * Time.deltaTime;
            }
        }

        return totalRepulsion;
    }

    private Vector2 ApplyRopeConstraint(Vector2 pos)
    {
        if (animalTransform == null) return pos;

        Vector2 animalPos = animalTransform.position;
        Vector2 toPos = pos - animalPos;
        float dist = toPos.magnitude;

        if (dist > maxRopeLength)
        {
            pos = animalPos + toPos.normalized * maxRopeLength;
        }

        return pos;
    }

    private void UpdateRotation(float dt)
    {
        if (animalTransform == null) return;

        Vector2 toAnimal = (Vector2)animalTransform.position - (Vector2)transform.position;
        if (toAnimal.sqrMagnitude < 0.001f) return;

        float targetAngle = Mathf.Atan2(toAnimal.y, toAnimal.x) * Mathf.Rad2Deg + 90f;
        targetAngle = Mathf.Clamp(targetAngle, -maxTiltAngle, maxTiltAngle);

        currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angularVelocity, rotationSmoothness);
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    // --- Public API ---

    /// <summary>
    /// Sets a fly-in destination. The balloon will SmoothDamp toward this position,
    /// with rope constraint disabled so it can fly ahead of the animal.
    /// Auto-clears when the balloon arrives within threshold distance.
    /// </summary>
    public void SetFlyInTarget(Vector3 target)
    {
        flyInTarget = target;
        hasFlyInTarget = true;
    }

    public bool HasFlyInTarget()
    {
        return hasFlyInTarget;
    }

    public Vector3 GetRopeAttachmentPoint()
    {
        return transform.position + transform.TransformDirection(ropeAttachOffset * transform.localScale.x);
    }

    public float GetLiftForce()
    {
        return balloonPullForce;
    }

    public float GetCollisionRadius()
    {
        return balloonRadius * collisionRadiusMultiplier;
    }

    public void SetAnimal(Transform animal)
    {
        animalTransform = animal;
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (!frozen)
        {
            currentVelocity = Vector2.zero;
        }
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public bool IsActive()
    {
        return isActive;
    }

    public void ApplyShockwave(Vector3 origin, float strength)
    {
        if (!isActive) return;
        Vector3 away = (transform.position - origin).normalized;
        shockwaveOffset += away * strength;
    }

    private void OnDestroy()
    {
        if (balloonManager != null)
        {
            balloonManager.UnregisterBalloon(this);
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, balloonRadius * collisionRadiusMultiplier);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetRopeAttachmentPoint(), 0.05f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)currentVelocity * 0.3f);

        // Draw fly-in target if active
        if (hasFlyInTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(flyInTarget, 0.15f);
            Gizmos.DrawLine(transform.position, flyInTarget);
        }
    }
}
