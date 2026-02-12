using UnityEngine;

/// <summary>
/// Renders a visual rope/string connection between a balloon and the animal.
/// Uses LineRenderer for simple, efficient rope visualization.
/// Part of M1/A3.1: Ropes and Float Animation
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RopeRenderer : MonoBehaviour
{
    [Header("Rope Connection")]
    [Tooltip("The animal this rope connects to")]
    [SerializeField] private Transform animalTransform;

    [Tooltip("Offset from balloon position (adjust for visual attachment point)")]
    [SerializeField] private Vector3 balloonOffset = new Vector3(0, -0.5f, 0);

    [Tooltip("Offset from animal position (adjust for visual attachment point)")]
    [SerializeField] private Vector3 animalOffset = new Vector3(0, 0.5f, 0);

    [Header("Rope Appearance")]
    [Tooltip("Width of the rope line")]
    [SerializeField] private float ropeWidth = 0.05f;

    [Tooltip("Color of the rope")]
    [SerializeField] private Color ropeColor = new Color(0.6f, 0.4f, 0.2f, 0.8f); // Brown-ish

    [Tooltip("Number of segments for rope curve (higher = smoother, 2 = straight line)")]
    [SerializeField] private int ropeSegments = 5;

    [Tooltip("Amount of rope sag/curve in the middle (0 = straight, higher = more sag)")]
    [SerializeField] private float ropeSag = 0.3f;

    private LineRenderer lineRenderer;
    private Transform balloonTransform;

    private void Awake()
    {
        balloonTransform = transform;
        SetupLineRenderer();
    }

    private void Start()
    {
        // If no animal assigned, try to find it
        if (animalTransform == null)
        {
            GameObject animal = GameObject.Find("Animal");
            if (animal != null)
            {
                animalTransform = animal.transform;
                Debug.Log($"RopeRenderer: Auto-found animal '{animal.name}'");
            }
            else
            {
                Debug.LogWarning("RopeRenderer: No animal found! Rope will not render.");
            }
        }
    }

    private void LateUpdate()
    {
        // Update rope position every frame to follow balloon and animal
        if (animalTransform != null && balloonTransform != null)
        {
            UpdateRopeLine();
        }
    }

    /// <summary>
    /// Sets up the LineRenderer component with initial settings
    /// </summary>
    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // Basic settings
        lineRenderer.positionCount = ropeSegments;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
        lineRenderer.startColor = ropeColor;
        lineRenderer.endColor = ropeColor;

        // Material (use default sprite material for 2D)
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material.color = ropeColor;

        // Sorting
        lineRenderer.sortingOrder = 5; // Behind balloons (10) but above background

        // Disable shadows and other 3D features
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Use world space
        lineRenderer.useWorldSpace = true;

        // Smooth corners
        lineRenderer.numCornerVertices = 3;
        lineRenderer.numCapVertices = 3;
    }

    /// <summary>
    /// Updates the rope line positions to follow balloon and animal
    /// Updated for M1/A3.2: Uses physics-based attachment points
    /// </summary>
    private void UpdateRopeLine()
    {
        // Try to get attachment points from physics components
        Vector3 startPos;
        Vector3 endPos;

        // Get balloon attachment point (bottom center)
        BalloonPhysics balloonPhysics = balloonTransform.GetComponent<BalloonPhysics>();
        if (balloonPhysics != null)
        {
            startPos = balloonPhysics.GetRopeAttachmentPoint();
        }
        else
        {
            // Fallback to offset method (for backward compatibility)
            startPos = balloonTransform.position + balloonOffset;
        }

        // Get animal attachment point (center)
        AnimalPhysics animalPhysics = animalTransform.GetComponent<AnimalPhysics>();
        if (animalPhysics != null)
        {
            endPos = animalPhysics.GetRopeAttachmentPoint();
        }
        else
        {
            // Fallback to offset method (for backward compatibility)
            endPos = animalTransform.position + animalOffset;
        }

        // Calculate rope curve with sag
        for (int i = 0; i < ropeSegments; i++)
        {
            float t = i / (float)(ropeSegments - 1);

            // Linear interpolation
            Vector3 position = Vector3.Lerp(startPos, endPos, t);

            // Add sag (parabolic curve in the middle)
            float sagAmount = CalculateSag(t);
            position.x += sagAmount * ropeSag; // Sag to the side (can change to .y for downward sag)

            lineRenderer.SetPosition(i, position);
        }
    }

    /// <summary>
    /// Calculates the sag amount for a given position along the rope
    /// Uses a parabolic curve (peaks at t=0.5)
    /// </summary>
    private float CalculateSag(float t)
    {
        // Parabola: -4(t - 0.5)^2 + 1
        // Peaks at t=0.5 with value 1, goes to 0 at t=0 and t=1
        return -4f * (t - 0.5f) * (t - 0.5f) + 1f;
    }

    /// <summary>
    /// Sets the animal transform reference
    /// </summary>
    public void SetAnimal(Transform animal)
    {
        animalTransform = animal;
    }

    /// <summary>
    /// Enables or disables the rope rendering
    /// </summary>
    public void SetRopeVisible(bool visible)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
    }

    /// <summary>
    /// Called when balloon is destroyed - disable rope
    /// </summary>
    private void OnDestroy()
    {
        // Clean up material to prevent memory leak
        if (lineRenderer != null && lineRenderer.material != null)
        {
            Destroy(lineRenderer.material);
        }
    }
}
