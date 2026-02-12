using UnityEngine;

/// <summary>
/// Simple helper script to spawn a test balloon for M1/A1.
/// This is a temporary setup script for testing the balloon pop functionality.
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

    private void Start()
    {
        SpawnBalloon();
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
        collider.radius = 0.5f;

        // Add BalloonController script
        balloon.AddComponent<BalloonController>();

        Debug.Log("Balloon spawned at " + spawnPosition + ". Tap it to see the pop animation!");
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
}
