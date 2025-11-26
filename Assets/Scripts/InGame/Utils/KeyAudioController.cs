using UnityEngine;
using System.Collections;

/// <summary>
/// Handles audio playback for key spawning and collection.
/// Supports proximity-based audio to mimic real-life sound propagation.
/// Sounds: Key spawn (pop-up) sound and key collection sound
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class KeyAudioController : MonoBehaviour
{
    [Header("Key Sounds (One-Shot)")]
    [Tooltip("Sound played when key pops up from chest")]
    [SerializeField] private AudioClip keySpawnSound;

    [Tooltip("Sound played when key is collected by player")]
    [SerializeField] private AudioClip keyCollectSound;

    [Header("Key Sound Timing")]
    [Tooltip("Delay before playing key spawn sound (seconds)")]
    [SerializeField] private float keySpawnSoundDelay = 0f;

    [Tooltip("Delay before playing key collect sound (seconds)")]
    [SerializeField] private float keyCollectSoundDelay = 0f;

    [Header("Audio Settings")]
    [SerializeField] private float volume = 1.0f;
    [SerializeField] private float pitch = 1.0f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Proximity Settings")]
    [SerializeField] private float maxAudibleDistance = 15f;
    [SerializeField] private float minAudibleDistance = 3f;
    [SerializeField] private bool useProximitySystem = true;
    [SerializeField] private AnimationCurve volumeFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // Core Components
    private AudioSource audioSource;
    private Transform player;

    void Start()
    {
        InitializeComponents();
        ConfigureAudioSource();
        ValidateAudioClips();
    }

    #region Initialization
    private void InitializeComponents()
    {
        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            LogDebug("AudioSource component added to Key");
        }

        // Find player reference for proximity
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            LogDebug("Player reference found for proximity audio");
        }
        else
        {
            Debug.LogWarning("Player not found - proximity audio disabled");
            useProximitySystem = false;
        }

        LogDebug("KeyAudioController initialized");
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null) return;

        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = useProximitySystem ? 1.0f : 0.0f;
        audioSource.loop = false;
        audioSource.playOnAwake = false;

        if (useProximitySystem)
        {
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.maxDistance = maxAudibleDistance;
            audioSource.minDistance = minAudibleDistance;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, volumeFalloffCurve);
        }

        LogDebug($"AudioSource configured: Volume={volume}, Pitch={pitch}, Spatial={useProximitySystem}");
    }

    private void ValidateAudioClips()
    {
        LogDebug("=== Key Audio Clips Validation ===");
        ValidateClip(keySpawnSound, "Key Spawn Sound");
        ValidateClip(keyCollectSound, "Key Collect Sound");
    }

    private void ValidateClip(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"Key - {clipName} NOT ASSIGNED!");
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"Key - {clipName} NOT LOADED! LoadState: {clip.loadState}");
            return;
        }

        LogDebug($"✓ {clipName}: '{clip.name}' ({clip.length:F2}s)");
    }
    #endregion

    #region Proximity System
    /// <summary>
    /// Check if player is close enough to hear the sound
    /// </summary>
    private bool IsPlayerInRange()
    {
        if (!useProximitySystem || player == null) return true;

        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= maxAudibleDistance;
    }

    /// <summary>
    /// Get volume multiplier based on player distance
    /// </summary>
    private float GetProximityVolumeMultiplier()
    {
        if (!useProximitySystem || player == null) return 1f;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance >= maxAudibleDistance) return 0f;
        if (distance <= minAudibleDistance) return 1f;

        float normalizedDistance = (distance - minAudibleDistance) / (maxAudibleDistance - minAudibleDistance);
        return volumeFalloffCurve.Evaluate(1f - normalizedDistance);
    }
    #endregion

    #region Public Audio Trigger Methods
    /// <summary>
    /// Play key spawn (pop-up) sound with optional delay
    /// Called by StringExerciseChestTrigger when key spawns from chest
    /// </summary>
    public void PlayKeySpawnSound()
    {
        if (keySpawnSound == null)
        {
            LogDebug("Key spawn sound not assigned - skipping");
            return;
        }

        if (keySpawnSoundDelay > 0)
        {
            StartCoroutine(PlayDelayedKeySpawnSound());
        }
        else
        {
            PlayOneTimeSoundEffect(keySpawnSound);
            LogDebug("Playing key spawn sound (no delay)");
        }
    }

    /// <summary>
    /// Play key collection sound with optional delay
    /// Called by KeyCollectionTrigger when player collects the key
    /// </summary>
    public void PlayKeyCollectSound()
    {
        if (keyCollectSound == null)
        {
            LogDebug("Key collect sound not assigned - skipping");
            return;
        }

        if (keyCollectSoundDelay > 0)
        {
            StartCoroutine(PlayDelayedKeyCollectSound());
        }
        else
        {
            PlayOneTimeSoundEffect(keyCollectSound);
            LogDebug("Playing key collect sound (no delay)");
        }
    }

    /// <summary>
    /// Coroutine to play key spawn sound after delay
    /// </summary>
    private IEnumerator PlayDelayedKeySpawnSound()
    {
        LogDebug($"Key spawn sound scheduled - will play after {keySpawnSoundDelay}s delay");

        yield return new WaitForSeconds(keySpawnSoundDelay);

        PlayOneTimeSoundEffect(keySpawnSound);
        LogDebug($"Playing key spawn sound (after {keySpawnSoundDelay}s delay)");
    }

    /// <summary>
    /// Coroutine to play key collect sound after delay
    /// </summary>
    private IEnumerator PlayDelayedKeyCollectSound()
    {
        LogDebug($"Key collect sound scheduled - will play after {keyCollectSoundDelay}s delay");

        yield return new WaitForSeconds(keyCollectSoundDelay);

        PlayOneTimeSoundEffect(keyCollectSound);
        LogDebug($"Playing key collect sound (after {keyCollectSoundDelay}s delay)");
    }

    /// <summary>
    /// Force stop all sounds
    /// </summary>
    public void StopAllSounds()
    {
        StopAllCoroutines(); // Stop any delayed sounds

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        LogDebug("All key sounds stopped");
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Plays a one-time sound effect
    /// </summary>
    private void PlayOneTimeSoundEffect(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        // Only play if player is in range
        if (!IsPlayerInRange())
        {
            LogDebug("Player out of range - sound effect muted");
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[KeyAudio] {message}");
        }
    }
    #endregion

    #region Public Getters
    public bool IsPlayingSound() => audioSource != null && audioSource.isPlaying;
    #endregion

    void OnDestroy()
    {
        StopAllSounds();
    }
}