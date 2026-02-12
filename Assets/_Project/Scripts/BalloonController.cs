using UnityEngine;

/// <summary>
/// Handles balloon tap detection, pop animation, and destruction.
/// Part of Milestone M1/A1: Tap balloon → pop animation + randomized SFX
/// </summary>
public class BalloonController : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Duration of the scale-punch animation before pop")]
    [SerializeField] private float punchDuration = 0.15f;

    [Tooltip("Scale multiplier for the punch effect")]
    [SerializeField] private float punchScale = 1.3f;

    [Tooltip("Duration of the pop shrink animation")]
    [SerializeField] private float popDuration = 0.2f;

    [Header("VFX Settings")]
    [Tooltip("Enable pop VFX particle burst (M1/A2)")]
    [SerializeField] private bool enablePopVFX = true;

    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private bool isPopping = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    private void OnMouseDown()
    {
        // Only process tap if not already popping
        if (!isPopping)
        {
            Pop();
        }
    }

    /// <summary>
    /// Triggers the balloon pop sequence: punch animation → SFX → pop animation → destroy
    /// </summary>
    public void Pop()
    {
        if (isPopping) return;

        isPopping = true;

        // Play randomized pop SFX
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayRandomPopSFX();
        }

        // Start pop animation sequence
        StartCoroutine(PopSequence());
    }

    private System.Collections.IEnumerator PopSequence()
    {
        // Phase 1: Scale punch (balloon expands quickly)
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            float scale = Mathf.Lerp(1f, punchScale, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        // Spawn pop VFX at peak of punch animation (M1/A2)
        if (enablePopVFX)
        {
            Color balloonColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            PopVFXController.SpawnPopVFX(transform.position, balloonColor);
        }

        // Phase 2: Pop shrink (balloon shrinks and disappears)
        elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            float scale = Mathf.Lerp(punchScale, 0f, t);
            transform.localScale = originalScale * scale;

            // Fade out sprite
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        // Destroy the balloon GameObject
        Destroy(gameObject);
    }
}
