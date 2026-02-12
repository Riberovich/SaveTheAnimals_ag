using UnityEngine;

/// <summary>
/// Manages audio playback including randomized pop sound effects.
/// Singleton pattern for easy access from anywhere.
/// Part of Milestone M1/A1: Tap balloon → pop animation + randomized SFX
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pop Sound Effects")]
    [Tooltip("Array of pop SFX clips (8+ recommended for variety)")]
    [SerializeField] private AudioClip[] popSFXClips;

    [Header("Audio Source Settings")]
    [Tooltip("Volume for pop sound effects (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float popSFXVolume = 0.8f;

    [Tooltip("Pitch variation range for more variety (+/- this value)")]
    [SerializeField] [Range(0f, 0.3f)] private float pitchVariation = 0.1f;

    private AudioSource audioSource;
    private int lastPlayedIndex = -1; // Avoid repeating the same sound twice in a row

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Plays a random pop SFX from the library with pitch variation.
    /// Avoids playing the same sound twice in a row.
    /// </summary>
    public void PlayRandomPopSFX()
    {
        // Check if we have any clips
        if (popSFXClips == null || popSFXClips.Length == 0)
        {
            Debug.LogWarning("AudioManager: No pop SFX clips assigned. Add AudioClips in the Inspector.");
            return;
        }

        // Pick a random clip (avoid repeating the last one if we have multiple clips)
        int randomIndex;
        if (popSFXClips.Length == 1)
        {
            randomIndex = 0;
        }
        else
        {
            do
            {
                randomIndex = Random.Range(0, popSFXClips.Length);
            }
            while (randomIndex == lastPlayedIndex && popSFXClips.Length > 1);
        }

        lastPlayedIndex = randomIndex;

        // Apply random pitch variation
        float randomPitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.pitch = randomPitch;

        // Play the sound
        audioSource.PlayOneShot(popSFXClips[randomIndex], popSFXVolume);
    }

    /// <summary>
    /// Optional: Play a specific pop SFX by index
    /// </summary>
    public void PlayPopSFX(int index)
    {
        if (popSFXClips == null || popSFXClips.Length == 0)
        {
            Debug.LogWarning("AudioManager: No pop SFX clips assigned.");
            return;
        }

        if (index < 0 || index >= popSFXClips.Length)
        {
            Debug.LogWarning($"AudioManager: Invalid index {index}. Valid range: 0-{popSFXClips.Length - 1}");
            return;
        }

        audioSource.PlayOneShot(popSFXClips[index], popSFXVolume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
