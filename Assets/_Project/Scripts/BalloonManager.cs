using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all active balloons, facilitates collision detection, and handles shockwave effects.
/// Part of M1/A3.2: Pseudo-Physical Balloon System
/// </summary>
public class BalloonManager : MonoBehaviour
{
    [Header("Shockwave Settings")]
    [Tooltip("Base strength of shockwave when balloon pops")]
    [SerializeField] private float shockwaveStrength = 3.0f;

    [Tooltip("Radius of shockwave effect")]
    [SerializeField] private float shockwaveRadius = 2.0f;

    [Tooltip("How quickly shockwave strength falls off with distance")]
    [SerializeField] private float shockwaveFalloff = 2.0f;

    // Active balloons list
    private List<BalloonPhysics> activeBalloons = new List<BalloonPhysics>();

    // Singleton instance
    public static BalloonManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Registers a balloon with the manager
    /// </summary>
    public void RegisterBalloon(BalloonPhysics balloon)
    {
        if (!activeBalloons.Contains(balloon))
        {
            activeBalloons.Add(balloon);
            Debug.Log($"BalloonManager: Registered balloon. Total: {activeBalloons.Count}");
        }
    }

    /// <summary>
    /// Unregisters a balloon from the manager
    /// </summary>
    public void UnregisterBalloon(BalloonPhysics balloon)
    {
        if (activeBalloons.Contains(balloon))
        {
            activeBalloons.Remove(balloon);
            Debug.Log($"BalloonManager: Unregistered balloon. Remaining: {activeBalloons.Count}");
        }
    }

    /// <summary>
    /// Gets all active balloons (for collision detection)
    /// </summary>
    public List<BalloonPhysics> GetAllBalloons()
    {
        // Clean up any null references
        activeBalloons.RemoveAll(b => b == null);
        return activeBalloons;
    }

    /// <summary>
    /// Triggers a shockwave from a balloon pop position
    /// </summary>
    public void TriggerShockwave(Vector3 origin)
    {
        Debug.Log($"BalloonManager: Shockwave at {origin}, affecting {activeBalloons.Count} balloons");

        foreach (BalloonPhysics balloon in activeBalloons)
        {
            if (balloon == null) continue;

            float distance = Vector3.Distance(balloon.transform.position, origin);

            // Only affect balloons within radius
            if (distance <= shockwaveRadius && distance > 0.01f)
            {
                // Calculate falloff (closer = stronger)
                float falloff = 1f - Mathf.Pow(distance / shockwaveRadius, shockwaveFalloff);
                float strength = shockwaveStrength * falloff;

                balloon.ApplyShockwave(origin, strength);
            }
        }
    }

    /// <summary>
    /// Gets the count of active balloons
    /// </summary>
    public int GetBalloonCount()
    {
        activeBalloons.RemoveAll(b => b == null);
        return activeBalloons.Count;
    }

    /// <summary>
    /// Debug visualization
    /// </summary>
    private void OnDrawGizmos()
    {
        // Show active balloon count in scene
        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            // Could draw connection lines between balloons here if needed
        }
    }
}
