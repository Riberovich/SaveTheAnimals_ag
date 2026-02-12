using UnityEngine;

/// <summary>
/// Manages pop VFX effects for balloon pops.
/// Creates a simple particle burst that works in both World Space and Canvas/UI space.
/// Part of Milestone M1/A2: Pop VFX (Canvas-friendly)
/// </summary>
public class PopVFXController : MonoBehaviour
{
    [Header("Particle Settings")]
    [Tooltip("Number of particles to emit on pop")]
    [SerializeField] private int particleCount = 12;

    [Tooltip("Minimum particle size")]
    [SerializeField] private float minSize = 0.1f;

    [Tooltip("Maximum particle size")]
    [SerializeField] private float maxSize = 0.3f;

    [Tooltip("Speed of particle burst")]
    [SerializeField] private float burstSpeed = 3f;

    [Tooltip("Lifetime of particles in seconds")]
    [SerializeField] private float particleLifetime = 0.8f;

    [Header("Visual Settings")]
    [Tooltip("Start color of particles")]
    [SerializeField] private Color startColor = Color.yellow;

    [Tooltip("End color of particles (with fade)")]
    [SerializeField] private Color endColor = new Color(1f, 1f, 1f, 0f);

    // Static cache to avoid creating new resources every pop (M1/A2.1 optimization)
    private static Texture2D cachedParticleTexture;
    private static Material cachedParticleMaterial;

    private ParticleSystem particleSystem;
    private ParticleSystemRenderer particleRenderer;

    private void Awake()
    {
        SetupParticleSystem();
    }

    /// <summary>
    /// Sets up the particle system with mobile-friendly settings
    /// </summary>
    private void SetupParticleSystem()
    {
        // Add ParticleSystem component
        particleSystem = gameObject.AddComponent<ParticleSystem>();
        particleRenderer = GetComponent<ParticleSystemRenderer>();

        // IMPORTANT: make sure it's fully stopped before editing main module (prevents "duration while playing")
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystem.Clear(true);

        // Main module
        var main = particleSystem.main;
        main.duration = 0.2f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = particleLifetime;
        main.startSpeed = burstSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = startColor;
        main.gravityModifier = 0f; // No physics
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = particleCount;
        main.stopAction = ParticleSystemStopAction.Destroy; // Auto-destroy when done

        // Emission module
        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, particleCount)
        });

        // Shape module (sphere burst)
        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Color over lifetime (fade out)
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(startColor, 0.0f),
                new GradientColorKey(endColor, 1.0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Size over lifetime (shrink)
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer settings
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingOrder = 100; // Render on top
        particleRenderer.material = CreateParticleMaterial();

        // Performance: disable unnecessary modules
        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = false;

        var collision = particleSystem.collision;
        collision.enabled = false;

        var trails = particleSystem.trails;
        trails.enabled = false;
    }

    /// <summary>
    /// Creates a simple particle material with a circular texture.
    /// Uses static cache to avoid creating new resources on every pop (M1/A2.1).
    /// </summary>
    private Material CreateParticleMaterial()
    {
        // Return cached material if it exists
        if (cachedParticleMaterial != null)
        {
            return cachedParticleMaterial;
        }

        // Create material only once
        Material material = new Material(Shader.Find("Particles/Standard Unlit"));

        // Create a simple circular texture for the particles (cached)
        Texture2D particleTexture = CreateCircleTexture(64);
        material.mainTexture = particleTexture;

        // Set blend mode for proper transparency
        material.SetFloat("_Mode", 2); // Fade mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;

        // Cache for future use
        cachedParticleMaterial = material;

        return material;
    }

    /// <summary>
    /// Creates a procedural circular texture for particles.
    /// Uses static cache to avoid recreation on every pop (M1/A2.1).
    /// </summary>
    private Texture2D CreateCircleTexture(int resolution)
    {
        // Return cached texture if it exists
        if (cachedParticleTexture != null)
        {
            return cachedParticleTexture;
        }

        // Create texture only once
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];

        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float maxRadius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                float normalizedDistance = distance / maxRadius;

                if (normalizedDistance <= 1f)
                {
                    // Smooth falloff from center to edge
                    float alpha = 1f - normalizedDistance;
                    alpha = alpha * alpha; // Square for smoother falloff
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        // Cache for future use
        cachedParticleTexture = texture;

        return texture;
    }

    /// <summary>
    /// Plays the pop VFX effect
    /// </summary>
    public void Play()
    {
        if (particleSystem != null)
        {
            particleSystem.Play();
        }
    }

    /// <summary>
    /// Static helper to spawn a pop VFX at a specific position
    /// </summary>
    public static void SpawnPopVFX(Vector3 position, Color? color = null)
    {
        GameObject vfxObject = new GameObject("PopVFX");
        vfxObject.transform.position = position;

        PopVFXController vfx = vfxObject.AddComponent<PopVFXController>();

        // Apply custom color if provided
        if (color.HasValue)
        {
            vfx.startColor = color.Value;
            Color endCol = color.Value;
            endCol.a = 0f;
            vfx.endColor = endCol;
        }

        vfx.Play();

        // Auto-destroy after particles finish
        Destroy(vfxObject, vfx.particleLifetime + 0.5f);
    }

    /// <summary>
    /// Canvas-friendly version: Spawns pop VFX as a child of a Canvas element
    /// </summary>
    public static void SpawnPopVFXOnCanvas(Transform canvasParent, Vector3 localPosition, Color? color = null)
    {
        GameObject vfxObject = new GameObject("PopVFX_Canvas");
        vfxObject.transform.SetParent(canvasParent, false);
        vfxObject.transform.localPosition = localPosition;

        PopVFXController vfx = vfxObject.AddComponent<PopVFXController>();

        // Set simulation space to Local for Canvas
        var main = vfx.particleSystem.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        if (color.HasValue)
        {
            vfx.startColor = color.Value;
            Color endCol = color.Value;
            endCol.a = 0f;
            vfx.endColor = endCol;
        }

        vfx.Play();

        Destroy(vfxObject, vfx.particleLifetime + 0.5f);
    }
}
