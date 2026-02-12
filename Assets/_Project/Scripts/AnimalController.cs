using UnityEngine;

/// <summary>
/// Controls animal behavior including descent animation when balloons pop.
/// Works with both Sprite (Transform) and UI (RectTransform) animals.
/// Part of Milestone M1/A3: Animal descends per pop
/// </summary>
public class AnimalController : MonoBehaviour
{
    [Header("Descent Settings")]
    [Tooltip("Distance to descend per balloon pop (world units or UI pixels)")]
    [SerializeField] private float descendStep = 1.0f;

    [Tooltip("Duration of descent animation in seconds")]
    [SerializeField] private float descendDuration = 0.3f;

    [Tooltip("Easing curve for descent (0=linear, 1=smooth)")]
    [SerializeField] [Range(0f, 1f)] private float descendEasing = 0.5f;

    // Internal state
    private RectTransform rectTransform;
    private Transform cachedTransform;
    private bool isDescending = false;
    private Vector3 descendStartPosition;
    private Vector3 descendTargetPosition;
    private float descendElapsed = 0f;
    private bool isUIElement = false;

    private void Awake()
    {
        cachedTransform = transform;
        rectTransform = GetComponent<RectTransform>();
        isUIElement = rectTransform != null;

        if (isUIElement)
        {
            Debug.Log($"AnimalController: UI mode (RectTransform) detected on {gameObject.name}");
        }
        else
        {
            Debug.Log($"AnimalController: World space mode (Transform) detected on {gameObject.name}");
        }
    }

    private void Update()
    {
        // Handle descent animation if active
        if (isDescending)
        {
            UpdateDescentAnimation();
        }
    }

    /// <summary>
    /// Triggers the animal to descend by one step with smooth animation.
    /// Can be called from BalloonController when a balloon pops.
    /// </summary>
    public void DescendOneStep()
    {
        // If already descending, queue the next descent (or skip for simplicity)
        if (isDescending)
        {
            Debug.Log("AnimalController: Already descending, skipping this step.");
            return;
        }

        // Start descent animation
        StartDescend();
    }

    /// <summary>
    /// Starts the descent animation
    /// </summary>
    private void StartDescend()
    {
        isDescending = true;
        descendElapsed = 0f;

        // Temporarily disable FloatAnimator during descent (M1/A3.1)
        FloatAnimator floatAnimator = GetComponent<FloatAnimator>();
        if (floatAnimator != null)
        {
            floatAnimator.SetFloating(false);
        }

        // Get current position (works for both Transform and RectTransform)
        if (isUIElement)
        {
            descendStartPosition = rectTransform.anchoredPosition;
            // For UI, descend means moving down in Y (negative)
            descendTargetPosition = descendStartPosition + new Vector3(0, -descendStep, 0);
        }
        else
        {
            descendStartPosition = cachedTransform.position;
            // For world space, descend means moving down in Y (negative)
            descendTargetPosition = descendStartPosition + new Vector3(0, -descendStep, 0);
        }

        Debug.Log($"AnimalController: Starting descent from {descendStartPosition} to {descendTargetPosition}");
    }

    /// <summary>
    /// Updates the descent animation each frame
    /// </summary>
    private void UpdateDescentAnimation()
    {
        descendElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(descendElapsed / descendDuration);

        // Apply easing (simple smooth step)
        float easedT = ApplyEasing(t, descendEasing);

        // Interpolate position
        Vector3 newPosition = Vector3.Lerp(descendStartPosition, descendTargetPosition, easedT);

        // Apply to appropriate transform type
        if (isUIElement)
        {
            rectTransform.anchoredPosition = newPosition;
        }
        else
        {
            cachedTransform.position = newPosition;
        }

        // Check if animation complete
        if (t >= 1f)
        {
            isDescending = false;

            // Re-enable FloatAnimator and update its start position (M1/A3.1)
            FloatAnimator floatAnimator = GetComponent<FloatAnimator>();
            if (floatAnimator != null)
            {
                floatAnimator.UpdateStartPosition();
                floatAnimator.SetFloating(true);
            }

            Debug.Log("AnimalController: Descent complete");
        }
    }

    /// <summary>
    /// Simple easing function for smooth animation.
    /// strength: 0 = linear, 1 = smooth (smoothstep-like)
    /// </summary>
    private float ApplyEasing(float t, float strength)
    {
        if (strength <= 0f)
        {
            return t; // Linear
        }

        // Smoothstep interpolation
        float smoothT = t * t * (3f - 2f * t);
        return Mathf.Lerp(t, smoothT, strength);
    }

    /// <summary>
    /// Resets the animal to a specific position (useful for testing)
    /// </summary>
    public void ResetPosition(Vector3 position)
    {
        isDescending = false;

        if (isUIElement)
        {
            rectTransform.anchoredPosition = position;
        }
        else
        {
            cachedTransform.position = position;
        }

        Debug.Log($"AnimalController: Position reset to {position}");
    }

    /// <summary>
    /// Returns current descent state
    /// </summary>
    public bool IsDescending()
    {
        return isDescending;
    }
}
