using UnityEngine;

/// <summary>
/// Handles audio playback for the Lightning Barrier.
/// Supports proximity-based audio to mimic real-life sound propagation.
/// Plays continuous looping electric crackling/humming sound while barrier is active.
/// Sound stops when barrier is destroyed.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LightningBarrierAudioController : MonoBehaviour
{
    [Header("Barrier Sounds (Looping)")]
    [Tooltip("Looping electric crackling/humming sound for active barrier")]
    [SerializeField] private AudioClip barrierAmbientSound;

    [Header("Audio Settings")]
    [SerializeField] private float volume = 0.7f;  // Slightly louder than runes but quieter than combat
    [SerializeField] private float pitch = 1.0f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Proximity Settings")]
    [SerializeField] private float maxAudibleDistance = 25f;  // Barrier sounds travel very far (dangerous warning)
    [SerializeField] private float minAudibleDistance = 15f;
    [SerializeField] private bool useProximitySystem = true;
    [SerializeField] private AnimationCurve volumeFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // Core Components
    private AudioSource audioSource;
    private Transform player;

    // State tracking
    private bool isPlaying = false;

    void Start()
    {
        InitializeComponents();
        ConfigureAudioSource();
        ValidateAudioClips();

        // Start playing ambient sound immediately
        PlayBarrierAmbient();
    }

    #region Initialization
    private void InitializeComponents()
    {
        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            LogDebug("AudioSource component added to Lightning Barrier");
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

        LogDebug("LightningBarrierAudioController initialized");
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null) return;

        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = useProximitySystem ? 1.0f : 0.0f; // 3D sound for proximity
        audioSource.loop = true;  // Continuous loop
        audioSource.playOnAwake = false;

        // Configure 3D sound settings
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
        LogDebug("=== Lightning Barrier Audio Clips Validation ===");
        ValidateClip(barrierAmbientSound, "Barrier Ambient Sound");
    }

    private void ValidateClip(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"Lightning Barrier - {clipName} NOT ASSIGNED!");
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"Lightning Barrier - {clipName} NOT LOADED! LoadState: {clip.loadState}");
            return;
        }

        LogDebug($"✓ {clipName}: '{clip.name}' ({clip.length:F2}s)");
    }
    #endregion

    #region Ambient Sound System
    /// <summary>
    /// Start playing barrier ambient sound (called automatically on Start)
    /// </summary>
    public void PlayBarrierAmbient()
    {
        if (barrierAmbientSound == null || audioSource == null || isPlaying)
            return;

        // Only play if player is in range
        if (!IsPlayerInRange())
        {
            LogDebug($"Player out of range - ambient sound muted");
            return;
        }

        audioSource.clip = barrierAmbientSound;
        audioSource.Play();
        isPlaying = true;

        LogDebug("Started Lightning Barrier ambient loop (electric crackling)");
    }

    /// <summary>
    /// Stop barrier ambient sound (called when barrier is destroyed)
    /// </summary>
    public void StopBarrierAmbient()
    {
        if (audioSource == null || !isPlaying)
            return;

        audioSource.Stop();
        audioSource.clip = null;
        isPlaying = false;

        LogDebug("Stopped Lightning Barrier ambient loop");
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

        // Smooth falloff between min and max distance
        float normalizedDistance = (distance - minAudibleDistance) / (maxAudibleDistance - minAudibleDistance);
        return volumeFalloffCurve.Evaluate(1f - normalizedDistance);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Force stop all sounds
    /// </summary>
    public void StopAllSounds()
    {
        StopBarrierAmbient();
        LogDebug("All barrier sounds stopped");
    }
    #endregion

    #region Helper Methods
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[LightningBarrierAudio] {message}");
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