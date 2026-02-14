using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all active balloons: tracking, cluster center, collision facilitation, shockwaves.
/// </summary>
public class BalloonManager : MonoBehaviour
{
    [Header("Shockwave Settings")]
    [Tooltip("Base strength of shockwave when balloon pops")]
    [SerializeField] private float shockwaveStrength = 3.0f;

    [Tooltip("Radius of shockwave effect")]
    [SerializeField] private float shockwaveRadius = 2.5f;

    [Tooltip("How quickly shockwave strength falls off with distance")]
    [SerializeField] private float shockwaveFalloff = 2.0f;

    // Active balloons list
    private List<BalloonPhysics> activeBalloons = new List<BalloonPhysics>();

    // Cached cluster center (updated each frame)
    private Vector3 clusterCenter;

    // Singleton instance
    public static BalloonManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        UpdateClusterCenter();
    }

    /// <summary>
    /// Computes the average position of all active balloons.
    /// </summary>
    private void UpdateClusterCenter()
    {
        activeBalloons.RemoveAll(b => b == null);

        if (activeBalloons.Count == 0)
        {
            clusterCenter = Vector3.zero;
            return;
        }

        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (BalloonPhysics b in activeBalloons)
        {
            if (b.IsActive())
            {
                sum += b.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            clusterCenter = sum / count;
        }
    }

    /// <summary>
    /// Returns the current average position of all active balloons.
    /// </summary>
    public Vector3 GetClusterCenter()
    {
        return clusterCenter;
    }

    public void RegisterBalloon(BalloonPhysics balloon)
    {
        if (!activeBalloons.Contains(balloon))
        {
            activeBalloons.Add(balloon);
        }
    }

    public void UnregisterBalloon(BalloonPhysics balloon)
    {
        activeBalloons.Remove(balloon);
    }

    public List<BalloonPhysics> GetAllBalloons()
    {
        activeBalloons.RemoveAll(b => b == null);
        return activeBalloons;
    }

    public void TriggerShockwave(Vector3 origin)
    {
        foreach (BalloonPhysics balloon in activeBalloons)
        {
            if (balloon == null) continue;

            float distance = Vector3.Distance(balloon.transform.position, origin);

            if (distance <= shockwaveRadius && distance > 0.01f)
            {
                float falloff = 1f - Mathf.Pow(distance / shockwaveRadius, shockwaveFalloff);
                float strength = shockwaveStrength * falloff;
                balloon.ApplyShockwave(origin, strength);
            }
        }
    }

    public int GetBalloonCount()
    {
        activeBalloons.RemoveAll(b => b == null);
        return activeBalloons.Count;
    }
}
