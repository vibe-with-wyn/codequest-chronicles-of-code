using UnityEngine;

/// <summary>
/// Handles audio playback for Quest 3 elemental runes.
/// Supports proximity-based audio to mimic real-life sound propagation.
/// Each rune type (Fire, Air, Ice, Water) has its own looping ambient sound.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RuneAudioController : MonoBehaviour
{
    [Header("Rune Type")]
    [SerializeField] private RuneType runeType = RuneType.Fire;

    [Header("Ambient Sound (Looping)")]
    [Tooltip("Looping ambient sound for this rune (plays continuously until collected)")]
    [SerializeField] private AudioClip ambientSound;

    [Header("Audio Settings")]
    [SerializeField] private float volume = 0.5f;  // Runes are quieter than combat sounds
    [SerializeField] private float pitch = 1.0f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Proximity Settings")]
    [SerializeField] private float maxAudibleDistance = 15f;  // How far rune sounds travel
    [SerializeField] private float minAudibleDistance = 3f;
    [SerializeField] private bool useProximitySystem = true;
    [SerializeField] private AnimationCurve volumeFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // Core Components
    private AudioSource audioSource;
    private Transform player;

    // State tracking
    private bool isPlaying = false;

    // Rune type enumeration
    public enum RuneType
    {
        Fire,
        Air,
        Ice,
        Water
    }

    void Start()
    {
        InitializeComponents();
        ConfigureAudioSource();
        ValidateAudioClips();

        // Start playing ambient sound immediately
        PlayAmbientLoop();
    }

    #region Initialization
    private void InitializeComponents()
    {
        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            LogDebug("AudioSource component added to Rune");
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

        LogDebug($"RuneAudioController initialized for {runeType} Rune");
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
        LogDebug($"=== {runeType} Rune Audio Clips Validation ===");
        ValidateClip(ambientSound, "Ambient Sound");
    }

    private void ValidateClip(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"{runeType} Rune - {clipName} NOT ASSIGNED!");
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"{runeType} Rune - {clipName} NOT LOADED! LoadState: {clip.loadState}");
            return;
        }

        LogDebug($"✓ {clipName}: '{clip.name}' ({clip.length:F2}s)");
    }
    #endregion

    #region Ambient Sound System
    /// <summary>
    /// Start playing ambient loop (called automatically on Start)
    /// </summary>
    public void PlayAmbientLoop()
    {
        if (ambientSound == null || audioSource == null || isPlaying)
            return;

        // Only play if player is in range
        if (!IsPlayerInRange())
        {
            LogDebug($"Player out of range - ambient sound muted");
            return;
        }

        audioSource.clip = ambientSound;
        audioSource.Play();
        isPlaying = true;

        LogDebug($"Started {runeType} Rune ambient loop");
    }

    /// <summary>
    /// Stop ambient loop (called when rune is collected)
    /// </summary>
    public void StopAmbientLoop()
    {
        if (audioSource == null || !isPlaying)
            return;

        audioSource.Stop();
        audioSource.clip = null;
        isPlaying = false;

        LogDebug($"Stopped {runeType} Rune ambient loop");
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
        StopAmbientLoop();
        LogDebug("All rune sounds stopped");
    }
    #endregion

    #region Helper Methods
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{runeType}RuneAudio] {message}");
        }
    }
    #endregion

    #region Public Getters
    public bool IsPlayingSound() => audioSource != null && audioSource.isPlaying;
    public RuneType GetRuneType() => runeType;
    #endregion

    void OnDestroy()
    {
        StopAllSounds();
    }
}