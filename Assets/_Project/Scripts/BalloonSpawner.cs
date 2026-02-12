using UnityEngine;

/// <summary>
/// Simple helper script to spawn a test balloon for M1/A1.
/// This is a temporary setup script for testing the balloon pop functionality.
/// Updated for M1/A3 to include animal placeholder.
/// Updated for M1/A3.1 to include ropes and float animation.
/// </summary>
public class BalloonSpawner : MonoBehaviour
{
    [Header("Balloon Setup")]
    [Tooltip("Sprite to use for the balloon (assign any sprite, or leave empty for placeholder)")]
    [SerializeField] private Sprite balloonSprite;

    [Tooltip("Position to spawn the balloon")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0, 0, 0);

    [Tooltip("Size of the balloon")]
    [SerializeField] private float balloonSize = 2f;

    [Tooltip("Color of the balloon")]
    [SerializeField] private Color balloonColor = Color.red;

    [Header("Animal Setup (M1/A3)")]
    [Tooltip("Sprite to use for the animal (assign any sprite, or leave empty for placeholder)")]
    [SerializeField] private Sprite animalSprite;

    [Tooltip("Initial position for the animal (should be above balloons)")]
    [SerializeField] private Vector3 animalStartPosition = new Vector3(0, 4, 0);

    [Tooltip("Size of the animal")]
    [SerializeField] private float animalSize = 1.5f;

    [Header("Animation Settings (M1/A3.1)")]
    [Tooltip("Enable floating animation for balloons and animal")]
    [SerializeField] private bool enableFloatAnimation = true;

    [Tooltip("Enable rope rendering between balloons and animal")]
    [SerializeField] private bool enableRopes = true;

    [Header("Physics Settings (M1/A3.2)")]
    [Tooltip("Enable pseudo-physical balloon simulation (recommended)")]
    [SerializeField] private bool enablePhysicsSimulation = true;

    [Tooltip("Animal mass (higher = needs more balloons to lift)")]
    [SerializeField] private float animalMass = 2.0f;

    [Tooltip("Lift force per balloon")]
    [SerializeField] private float balloonLiftForce = 5.0f;

    private AnimalController animalController;
    private Transform animalTransform;
    private BalloonManager balloonManager;

    private void Start()
    {
        // Create BalloonManager for physics system (M1/A3.2)
        if (enablePhysicsSimulation)
        {
            GameObject managerObj = new GameObject("BalloonManager");
            balloonManager = managerObj.AddComponent<BalloonManager>();
        }

        // First, create the animal placeholder (M1/A3)
        SpawnAnimal();

        // Then spawn balloons with animal reference
        SpawnBalloon();
        // Test: spawn multiple colored balloons
        spawnPosition = new Vector3(-1, 1, 0);
        balloonColor = Color.white;
        SpawnBalloon();

        spawnPosition = new Vector3(1, 2, 0);
        balloonColor = Color.white;
        SpawnBalloon();
    }

    /// <summary>
    /// Spawns an animal placeholder for M1/A3 testing
    /// </summary>
    private void SpawnAnimal()
    {
        // Create animal GameObject
        GameObject animal = new GameObject("Animal");
        animal.transform.position = animalStartPosition;
        animal.transform.localScale = Vector3.one * animalSize;

        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = animal.AddComponent<SpriteRenderer>();

        if (animalSprite != null)
        {
            spriteRenderer.sprite = animalSprite;
        }
        else
        {
            // Create a simple square sprite as placeholder
            spriteRenderer.sprite = CreateSquareSprite();
        }

        spriteRenderer.sortingOrder = 10; // Render on top of balloons

        // Add AnimalController script
        animalController = animal.AddComponent<AnimalController>();
        animalTransform = animal.transform;

        // Choose animation system based on settings (M1/A3.2)
        if (enablePhysicsSimulation)
        {
            // Add physics simulation (realistic forces)
            AnimalPhysics animalPhysics = animal.AddComponent<AnimalPhysics>();
            animalPhysics.SetMass(animalMass);
            Debug.Log($"Animal spawned at {animalStartPosition} with mass {animalMass}. Physics enabled!");
        }
        else if (enableFloatAnimation)
        {
            // Add simple float animator (gentle bobbing)
            FloatAnimator floatAnimator = animal.AddComponent<FloatAnimator>();
            floatAnimator.SetFloatParameters(
                vAmplitude: 0.1f,
                vSpeed: 0.8f,
                hAmplitude: 0.05f,
                hSpeed: 0.6f
            );
            Debug.Log($"Animal spawned at {animalStartPosition}. Float animation enabled!");
        }
        else
        {
            Debug.Log($"Animal spawned at {animalStartPosition}. Static mode.");
        }
    }

    private void SpawnBalloon()
    {
        // Create balloon GameObject
        GameObject balloon = new GameObject("Balloon");
        balloon.transform.position = spawnPosition;
        balloon.transform.localScale = Vector3.one * balloonSize;

        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = balloon.AddComponent<SpriteRenderer>();

        if (balloonSprite != null)
        {
            spriteRenderer.sprite = balloonSprite;
        }
        else
        {
            // Create a simple circular sprite as placeholder
            spriteRenderer.sprite = CreateCircleSprite();
        }

        spriteRenderer.color = balloonColor;

        // Add CircleCollider2D for tap detection
        CircleCollider2D collider = balloon.AddComponent<CircleCollider2D>();
        collider.radius = 4f;

        // Add BalloonController script
        BalloonController balloonController = balloon.AddComponent<BalloonController>();

        // Assign animal reference (M1/A3)
        if (animalController != null)
        {
            // Use reflection to set the private field (since it's SerializeField)
            var field = typeof(BalloonController).GetField("animalController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(balloonController, animalController);
            }
        }

        // Choose animation/physics system (M1/A3.2)
        if (enablePhysicsSimulation)
        {
            // Add physics simulation (realistic forces and collision)
            BalloonPhysics balloonPhysics = balloon.AddComponent<BalloonPhysics>();
            balloonPhysics.SetAnimal(animalTransform);

            // Configure lift force via reflection (since it's SerializeField)
            var liftField = typeof(BalloonPhysics).GetField("liftForce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (liftField != null)
            {
                liftField.SetValue(balloonPhysics, balloonLiftForce);
            }
        }
        else if (enableFloatAnimation)
        {
            // Add simple float animator (gentle bobbing)
            FloatAnimator floatAnimator = balloon.AddComponent<FloatAnimator>();
            floatAnimator.SetFloatParameters(
                vAmplitude: 0.15f,
                vSpeed: 1.0f,
                hAmplitude: 0.08f,
                hSpeed: 0.7f
            );
        }

        // Add RopeRenderer to visually connect balloon to animal (M1/A3.1)
        if (enableRopes && animalTransform != null)
        {
            RopeRenderer ropeRenderer = balloon.AddComponent<RopeRenderer>();
            ropeRenderer.SetAnimal(animalTransform);
        }

        Debug.Log($"Balloon spawned at {spawnPosition}. Physics: {enablePhysicsSimulation}, Rope: {enableRopes}");
    }

    /// <summary>
    /// Creates a simple circle sprite as a placeholder
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];

        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f - 2;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                if (distance <= radius)
                {
                    // Inside the circle - white
                    pixels[y * resolution + x] = Color.white;
                }
                else
                {
                    // Outside the circle - transparent
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100);
    }

    /// <summary>
    /// Creates a simple square sprite as a placeholder for the animal (M1/A3)
    /// </summary>
    private Sprite CreateSquareSprite()
    {
        int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];

        // Create a simple square with rounded corners (animal placeholder)
        int padding = 8;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Simple square with small padding
                if (x >= padding && x < resolution - padding &&
                    y >= padding && y < resolution - padding)
                {
                    pixels[y * resolution + x] = Color.white;
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100);
    }
}
