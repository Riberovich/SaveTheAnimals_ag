using UnityEngine;

/// <summary>
/// Adds gentle floating/bobbing animation to give life to balloons and animals.
/// Creates subtle up/down and side-to-side movement using sine waves.
/// Part of M1/A3.1: Ropes and Float Animation
/// </summary>
public class FloatAnimator : MonoBehaviour
{
    [Header("Vertical Float (Bobbing)")]
    [Tooltip("Amplitude of up/down bobbing (how far it moves)")]
    [SerializeField] private float verticalAmplitude = 0.15f;

    [Tooltip("Speed of up/down bobbing (how fast it oscillates)")]
    [SerializeField] private float verticalSpeed = 1.0f;

    [Header("Horizontal Sway")]
    [Tooltip("Amplitude of left/right swaying")]
    [SerializeField] private float horizontalAmplitude = 0.08f;

    [Tooltip("Speed of left/right swaying")]
    [SerializeField] private float horizontalSpeed = 0.7f;

    [Header("Randomization")]
    [Tooltip("Add random phase offset to make each object move differently")]
    [SerializeField] private bool randomizePhase = true;

    [Tooltip("Add random speed variation (+/- this percentage)")]
    [SerializeField] [Range(0f, 0.5f)] private float speedVariation = 0.2f;

    private Vector3 startPosition;
    private float verticalPhaseOffset;
    private float horizontalPhaseOffset;
    private float verticalSpeedMultiplier;
    private float horizontalSpeedMultiplier;
    private bool isFloating = true;

    private void Start()
    {
        // Store the starting position as reference
        startPosition = transform.localPosition;

        // Randomize phase offsets and speed if enabled
        if (randomizePhase)
        {
            verticalPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
            horizontalPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
            verticalSpeedMultiplier = 1f + Random.Range(-speedVariation, speedVariation);
            horizontalSpeedMultiplier = 1f + Random.Range(-speedVariation, speedVariation);
        }
        else
        {
            verticalPhaseOffset = 0f;
            horizontalPhaseOffset = 0f;
            verticalSpeedMultiplier = 1f;
            horizontalSpeedMultiplier = 1f;
        }
    }

    private void Update()
    {
        if (!isFloating) return;

        // Calculate sine wave offsets
        float time = Time.time;

        float verticalOffset = Mathf.Sin((time * verticalSpeed * verticalSpeedMultiplier) + verticalPhaseOffset) * verticalAmplitude;
        float horizontalOffset = Mathf.Sin((time * horizontalSpeed * horizontalSpeedMultiplier) + horizontalPhaseOffset) * horizontalAmplitude;

        // Apply floating animation (relative to start position)
        Vector3 floatPosition = startPosition;
        floatPosition.y += verticalOffset;
        floatPosition.x += horizontalOffset;

        transform.localPosition = floatPosition;
    }

    /// <summary>
    /// Enables or disables the floating animation
    /// </summary>
    public void SetFloating(bool floating)
    {
        isFloating = floating;

        // Reset to start position if disabled
        if (!isFloating)
        {
            transform.localPosition = startPosition;
        }
    }

    /// <summary>
    /// Updates the start position reference (useful when object is moved programmatically)
    /// Call this after manually changing the object's position
    /// </summary>
    public void UpdateStartPosition()
    {
        startPosition = transform.localPosition;
    }

    /// <summary>
    /// Sets custom animation parameters
    /// </summary>
    public void SetFloatParameters(float vAmplitude, float vSpeed, float hAmplitude, float hSpeed)
    {
        verticalAmplitude = vAmplitude;
        verticalSpeed = vSpeed;
        horizontalAmplitude = hAmplitude;
        horizontalSpeed = hSpeed;
    }
}
