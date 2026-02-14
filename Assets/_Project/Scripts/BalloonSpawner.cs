using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns the animal + balloon composition off-screen right.
/// Physics is LIVE from frame 1 — no frozen state, no rigid coroutine animation.
/// Balloons get fly-in targets and SmoothDamp toward them (arriving first).
/// Animal follows naturally via spring-damper with inertia (arriving second, with swing).
/// </summary>
public class BalloonSpawner : MonoBehaviour
{
    [Header("Balloon Setup")]
    [Tooltip("Sprite for balloons (leave empty for placeholder)")]
    [SerializeField] private Sprite balloonSprite;

    [Tooltip("Size of each balloon")]
    [SerializeField] private float balloonSize = 2f;

    [Tooltip("Number of balloons")]
    [SerializeField] private int balloonCount = 3;

    [Header("Balloon Colors")]
    [SerializeField] private Color[] balloonColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),
        new Color(0.3f, 0.7f, 1f),
        new Color(1f, 0.9f, 0.3f),
        new Color(0.5f, 1f, 0.5f),
        new Color(1f, 0.5f, 0.9f),
    };

    [Header("Animal Setup")]
    [Tooltip("Sprite for animal (leave empty for placeholder)")]
    [SerializeField] private Sprite animalSprite;

    [Tooltip("Size of the animal")]
    [SerializeField] private float animalSize = 1.5f;

    [Header("Composition Layout")]
    [Tooltip("Where the composition ends up (animal position)")]
    [SerializeField] private Vector3 targetPosition = new Vector3(0, 0, 0);

    [Tooltip("Height of balloons above animal")]
    [SerializeField] private float balloonHeightAboveAnimal = 2.5f;

    [Tooltip("Horizontal spread of balloons")]
    [SerializeField] private float balloonSpread = 1.2f;

    [Header("Fly-In")]
    [Tooltip("How far off-screen right the composition starts")]
    [SerializeField] private float flyInOffsetX = 12f;

    [Header("Balloon Physics")]
    [Tooltip("Upward pull force per balloon")]
    [SerializeField] private float balloonPullForce = 3.0f;

    [Tooltip("SmoothDamp time for balloon movement")]
    [SerializeField] private float balloonSmoothTime = 0.35f;

    [Tooltip("Max rope length")]
    [SerializeField] private float maxRopeLength = 3.0f;

    [Tooltip("Rotation smoothness (lower = faster)")]
    [SerializeField] private float rotationSmoothness = 0.15f;

    [Header("Animal Physics")]
    [Tooltip("Animal mass (higher = more inertia, more fly-in delay)")]
    [SerializeField] private float animalMass = 1.5f;

    [Tooltip("Gravity offset below balloon cluster")]
    [SerializeField] private float gravityOffset = 2.0f;

    [Tooltip("Spring stiffness for animal follow")]
    [SerializeField] private float springStiffness = 8.0f;

    [Tooltip("Swing damping (lower = more swing on arrival)")]
    [SerializeField] [Range(0.1f, 1f)] private float swingDamping = 0.5f;

    [Header("Rope Appearance")]
    [Tooltip("Rope width in world units")]
    [SerializeField] private float ropeWidth = 0.04f;

    [Tooltip("Max visual stretch multiplier")]
    [SerializeField] private float maxRopeStretch = 1.3f;

    [Tooltip("Rope visual elasticity smoothing")]
    [SerializeField] private float ropeElasticity = 0.08f;

    // Runtime
    private Transform animalTransform;

    private void Start()
    {
        // Create BalloonManager singleton
        GameObject managerObj = new GameObject("BalloonManager");
        managerObj.AddComponent<BalloonManager>();

        // Spawn position: off-screen right
        Vector3 spawnOrigin = targetPosition + Vector3.right * flyInOffsetX;

        // Spawn animal at start position — physics is live immediately,
        // so it will start following the balloon cluster as soon as balloons move
        SpawnAnimal(spawnOrigin);

        // Spawn balloons at start position and give each a fly-in target.
        // Balloons SmoothDamp toward their targets (arriving first).
        // Animal follows via spring-damper with inertia (arriving second).
        SpawnAllBalloons(spawnOrigin);

        Debug.Log($"Composition spawned at x={spawnOrigin.x}. " +
                  $"Balloons fly to x={targetPosition.x}, animal follows with inertia.");
    }

    private void SpawnAnimal(Vector3 position)
    {
        GameObject animal = new GameObject("Animal");
        animal.transform.position = position;
        animal.transform.localScale = Vector3.one * animalSize;

        SpriteRenderer sr = animal.AddComponent<SpriteRenderer>();
        sr.sprite = animalSprite != null ? animalSprite : CreateSquareSprite();
        sr.sortingOrder = 5;

        // Animal follower — NOT frozen, spring-damper active from frame 1
        AnimalPhysics ap = animal.AddComponent<AnimalPhysics>();
        ap.SetMass(animalMass);

        // Set spring params via reflection
        SetField(ap, "gravityOffset", gravityOffset);
        SetField(ap, "springStiffness", springStiffness);
        SetField(ap, "swingDamping", swingDamping);

        animalTransform = animal.transform;
    }

    private void SpawnAllBalloons(Vector3 animalPos)
    {
        for (int i = 0; i < balloonCount; i++)
        {
            float xOffset = 0f;
            if (balloonCount > 1)
            {
                float t = (float)i / (balloonCount - 1);
                xOffset = Mathf.Lerp(-balloonSpread, balloonSpread, t);
            }

            float yVariation = Random.Range(-0.3f, 0.3f);

            // Start position: clustered above the animal's spawn point
            Vector3 startPos = animalPos + new Vector3(xOffset, balloonHeightAboveAnimal + yVariation, 0);

            // Fly-in target: where this balloon should end up (above the target position)
            Vector3 flyInTarget = targetPosition + new Vector3(xOffset, balloonHeightAboveAnimal + yVariation, 0);

            Color color = balloonColors[i % balloonColors.Length];

            SpawnBalloon(startPos, color, flyInTarget);
        }
    }

    private void SpawnBalloon(Vector3 position, Color color, Vector3 flyInTarget)
    {
        GameObject balloon = new GameObject("Balloon");
        balloon.transform.position = position;
        balloon.transform.localScale = Vector3.one * balloonSize;

        // Sprite
        SpriteRenderer sr = balloon.AddComponent<SpriteRenderer>();
        sr.sprite = balloonSprite != null ? balloonSprite : CreateCircleSprite();
        sr.color = color;
        sr.sortingOrder = 10;

        // Collider
        CircleCollider2D col = balloon.AddComponent<CircleCollider2D>();
        col.radius = 4f;

        // Pop controller
        balloon.AddComponent<BalloonController>();

        // Balloon physics — NOT frozen, fly-in target set
        BalloonPhysics bp = balloon.AddComponent<BalloonPhysics>();
        bp.SetAnimal(animalTransform);

        // Configure via reflection
        SetField(bp, "balloonPullForce", balloonPullForce);
        SetField(bp, "moveSmoothTime", balloonSmoothTime);
        SetField(bp, "maxRopeLength", maxRopeLength);
        SetField(bp, "rotationSmoothness", rotationSmoothness);

        // Set fly-in target — balloon will SmoothDamp toward it,
        // rope constraint disabled until arrival
        bp.SetFlyInTarget(flyInTarget);

        // Rope (sprite-based)
        RopeRenderer rope = balloon.AddComponent<RopeRenderer>();
        rope.SetAnimal(animalTransform);

        SetField(rope, "ropeWidth", ropeWidth);
        SetField(rope, "maxRopeStretch", maxRopeStretch);
        SetField(rope, "ropeElasticity", ropeElasticity);
    }

    private void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private Sprite CreateCircleSprite()
    {
        int res = 64;
        Texture2D tex = new Texture2D(res, res);
        Color[] px = new Color[res * res];
        Vector2 center = new Vector2(res / 2f, res / 2f);
        float radius = res / 2f - 2;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                px[y * res + x] = Vector2.Distance(new Vector2(x, y), center) <= radius ? Color.white : Color.clear;

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);
    }

    private Sprite CreateSquareSprite()
    {
        int res = 64;
        Texture2D tex = new Texture2D(res, res);
        Color[] px = new Color[res * res];
        int pad = 8;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                px[y * res + x] = (x >= pad && x < res - pad && y >= pad && y < res - pad) ? Color.white : Color.clear;

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);
    }
}
