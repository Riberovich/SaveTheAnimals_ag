using UnityEngine;

/// <summary>
/// Sprite-based straight rope connecting a balloon to the animal.
/// Rope is always straight — elasticity is represented ONLY by Y-scale.
/// No bending, no curves, no physics joints, no LineRenderer.
/// Renders BEHIND both balloons and the animal.
/// </summary>
public class RopeRenderer : MonoBehaviour
{
    [Header("Rope Appearance")]
    [Tooltip("Width of the rope in world units")]
    [SerializeField] private float ropeWidth = 0.04f;

    [Tooltip("Color of the rope")]
    [SerializeField] private Color ropeColor = new Color(0.55f, 0.35f, 0.2f, 0.9f);

    [Tooltip("Max stretch multiplier (visual cap, e.g., 1.3 = 30% longer than rest)")]
    [SerializeField] private float maxRopeStretch = 1.3f;

    [Tooltip("How smoothly the rope scale adjusts (lower = snappier)")]
    [SerializeField] private float ropeElasticity = 0.08f;

    [Header("References")]
    [Tooltip("The animal this rope connects to")]
    [SerializeField] private Transform animalTransform;

    // Runtime
    private GameObject ropeObject;
    private SpriteRenderer ropeSR;
    private Transform ropeTransform;
    private float currentScaleY;
    private float scaleYVelocity;
    private float restLength;
    private bool initialized = false;

    // Static cached sprite (shared across all ropes)
    private static Sprite cachedRopeSprite;

    private void Start()
    {
        if (animalTransform == null)
        {
            GameObject animal = GameObject.Find("Animal");
            if (animal != null)
            {
                animalTransform = animal.transform;
            }
        }

        CreateRopeObject();
        initialized = true;
    }

    private void CreateRopeObject()
    {
        // Create a standalone rope object (NOT parented to balloon — avoids inheriting pop scale)
        ropeObject = new GameObject("Rope_" + gameObject.name);
        ropeTransform = ropeObject.transform;

        ropeSR = ropeObject.AddComponent<SpriteRenderer>();
        ropeSR.sprite = GetRopeSprite();
        ropeSR.color = ropeColor;

        // Render behind everything
        ropeSR.sortingOrder = -1;

        // Initial rest length = current distance
        if (animalTransform != null)
        {
            restLength = Vector3.Distance(GetBalloonAttachPoint(), GetAnimalAttachPoint());
        }
        else
        {
            restLength = 2.5f;
        }

        currentScaleY = restLength;
    }

    private void LateUpdate()
    {
        if (!initialized || ropeObject == null || animalTransform == null) return;

        Vector3 balloonAttach = GetBalloonAttachPoint();
        Vector3 animalAttach = GetAnimalAttachPoint();
        Vector3 dir = animalAttach - balloonAttach;
        float distance = dir.magnitude;

        if (distance < 0.001f) return;

        // 1. Position: at balloon's rope attachment point
        ropeTransform.position = balloonAttach;

        // 2. Rotation: point local -Y toward animal (rope hangs down from balloon)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
        ropeTransform.rotation = Quaternion.Euler(0, 0, angle);

        // 3. Scale: X = width, Y = distance (with stretch cap + smooth)
        float targetScaleY = Mathf.Min(distance, restLength * maxRopeStretch);
        currentScaleY = Mathf.SmoothDamp(currentScaleY, targetScaleY, ref scaleYVelocity, ropeElasticity);

        ropeTransform.localScale = new Vector3(ropeWidth, currentScaleY, 1f);
    }

    private Vector3 GetBalloonAttachPoint()
    {
        // Use BalloonPhysics attach point if available (accounts for rotation)
        BalloonPhysics bp = GetComponent<BalloonPhysics>();
        if (bp != null)
        {
            return bp.GetRopeAttachmentPoint();
        }

        // Fallback: bottom of balloon
        return transform.position + Vector3.down * (transform.localScale.x * 0.5f);
    }

    private Vector3 GetAnimalAttachPoint()
    {
        // Use AnimalPhysics attach point if available
        AnimalPhysics ap = animalTransform.GetComponent<AnimalPhysics>();
        if (ap != null)
        {
            return ap.GetRopeAttachmentPoint();
        }

        // Fallback: center of animal
        return animalTransform.position;
    }

    /// <summary>
    /// Creates or returns cached 1x1 white sprite with pivot at top center.
    /// PPU = 1 so scale directly maps to world units.
    /// </summary>
    private static Sprite GetRopeSprite()
    {
        if (cachedRopeSprite != null) return cachedRopeSprite;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        // Pivot at (0.5, 1.0) = top center — sprite extends downward from anchor
        cachedRopeSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 1f), 1f);

        return cachedRopeSprite;
    }

    public void SetAnimal(Transform animal)
    {
        animalTransform = animal;
    }

    public void SetRopeVisible(bool visible)
    {
        if (ropeSR != null)
        {
            ropeSR.enabled = visible;
        }
    }

    private void OnDestroy()
    {
        // Clean up the standalone rope object
        if (ropeObject != null)
        {
            Destroy(ropeObject);
        }
    }
}
