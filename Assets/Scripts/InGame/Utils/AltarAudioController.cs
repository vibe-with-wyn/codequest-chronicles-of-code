using UnityEngine;
using System.Collections;

/// <summary>
/// Handles audio playback for altar activation.
/// Supports proximity-based audio to mimic real-life sound propagation.
/// Plays a single activation sound when altar is activated.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AltarAudioController : MonoBehaviour
{
    [Header("Activation Sound (One-Shot)")]
    [Tooltip("Sound played when altar activation begins")]
    [SerializeField] private AudioClip activationSound;

    [Header("Activation Sound Timing")]
    [Tooltip("Delay before playing activation sound (seconds)")]
    [SerializeField] private float activationSoundDelay = 0f;

    [Header("Audio Settings")]
    [SerializeField] private float volume = 1.0f;
    [SerializeField] private float pitch = 1.0f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Proximity Settings")]
    [SerializeField] private float maxAudibleDistance = 20f;  // Altar sounds travel far
    [SerializeField] private float minAudibleDistance = 5f;
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
            LogDebug("AudioSource component added to Altar");
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

        LogDebug("AltarAudioController initialized");
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
        LogDebug("=== Altar Audio Clips Validation ===");
        ValidateClip(activationSound, "Activation Sound");
    }

    private void ValidateClip(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"Altar - {clipName} NOT ASSIGNED!");
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"Altar - {clipName} NOT LOADED! LoadState: {clip.loadState}");
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

    #region Public Audio Trigger Methods (Called by HelloWorldAltarTrigger)
    /// <summary>
    /// Play activation sound with optional delay
    /// </summary>
    public void PlayActivationSound()
    {
        if (activationSound == null)
        {
            LogDebug("Activation sound not assigned - skipping");
            return;
        }

        if (activationSoundDelay > 0)
        {
            StartCoroutine(PlayDelayedActivationSound());
        }
        else
        {
            PlayOneTimeSoundEffect(activationSound);
            LogDebug("Playing activation sound (no delay)");
        }
    }

    /// <summary>
    /// Coroutine to play activation sound after delay
    /// </summary>
    private IEnumerator PlayDelayedActivationSound()
    {
        LogDebug($"Activation sound scheduled - will play after {activationSoundDelay}s delay");
        
        yield return new WaitForSeconds(activationSoundDelay);
        
        PlayOneTimeSoundEffect(activationSound);
        LogDebug($"Playing activation sound (after {activationSoundDelay}s delay)");
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

        LogDebug("All altar sounds stopped");
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
            Debug.Log($"[AltarAudio] {message}");
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