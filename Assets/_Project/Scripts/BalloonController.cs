using UnityEngine;

/// <summary>
/// Handles balloon tap detection, pop animation, and destruction.
/// Integrates with BalloonPhysics (deactivate on pop) and BalloonManager (shockwave).
/// Animal descent is handled naturally by the physics system (fewer balloons = less lift).
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
    [Tooltip("Enable pop VFX particle burst")]
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
        if (!isPopping)
        {
            Pop();
        }
    }

    public void Pop()
    {
        if (isPopping) return;
        isPopping = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayRandomPopSFX();
        }

        StartCoroutine(PopSequence());
    }

    private System.Collections.IEnumerator PopSequence()
    {
        // Phase 1: Scale punch (balloon expands)
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            float scale = Mathf.Lerp(1f, punchScale, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        // Deactivate physics
        BalloonPhysics bp = GetComponent<BalloonPhysics>();
        if (bp != null)
        {
            bp.Deactivate();
        }

        // Hide rope immediately
        RopeRenderer rope = GetComponent<RopeRenderer>();
        if (rope != null)
        {
            rope.SetRopeVisible(false);
        }

        // Pop VFX
        if (enablePopVFX)
        {
            Color balloonColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            PopVFXController.SpawnPopVFX(transform.position, balloonColor);
        }

        // Shockwave to nearby balloons
        BalloonManager bm = FindObjectOfType<BalloonManager>();
        if (bm != null)
        {
            bm.TriggerShockwave(transform.position);
        }

        // Phase 2: Shrink + fade out
        elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            float scale = Mathf.Lerp(punchScale, 0f, t);
            transform.localScale = originalScale * scale;

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
