using UnityEngine;
using System.Collections;

/// <summary>
/// Handles audio playback for Cave Boss (NightBorne) states and actions.
/// Supports proximity-based audio to mimic real-life sound propagation.
/// States: Run, Attack, Hurt, Die
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CaveBossAudioController : MonoBehaviour
{
    [Header("Movement Sounds (Looping)")]
    [SerializeField] private AudioClip runSound;
    [SerializeField] private float runSoundLoopInterval = 0.4f;

    [Header("Combat Sounds (One-Shot)")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip dieSound;

    [Header("Attack Sound Timing")]
    [Tooltip("Delay before playing attack sound (seconds) - allows syncing with animation")]
    [SerializeField] private float attackSoundDelay = 0.3f;

    [Header("Audio Settings")]
    [SerializeField] private float volume = 1.0f;
    [SerializeField] private float pitch = 1.0f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Proximity Settings")]
    [SerializeField] private float maxAudibleDistance = 15f;  // Boss sounds travel farther
    [SerializeField] private float minAudibleDistance = 8f;
    [SerializeField] private bool useProximitySystem = true;
    [SerializeField] private AnimationCurve volumeFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // Core Components
    private AudioSource audioSource;
    private CaveBossAI bossAI;
    private Transform player;

    // Sound state tracking
    private float soundLoopTimer = 0f;
    private AudioClip lastLoopingSound = null;
    private bool wasRunning = false;

    void Start()
    {
        InitializeComponents();
        ConfigureAudioSource();
        ValidateAudioClips();
    }

    void Update()
    {
        UpdateLoopingSoundEffects();
    }

    #region Initialization
    private void InitializeComponents()
    {
        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            LogDebug("AudioSource component added to Cave Boss");
        }

        // Get CaveBossAI reference
        bossAI = GetComponent<CaveBossAI>();
        if (bossAI == null)
        {
            Debug.LogError($"CaveBossAudioController on {gameObject.name}: CaveBossAI component not found!");
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

        LogDebug("CaveBossAudioController initialized");
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null) return;

        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = useProximitySystem ? 1.0f : 0.0f; // 3D sound for proximity
        audioSource.loop = false;
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
        LogDebug("=== Cave Boss Audio Clips Validation ===");
        ValidateClip(runSound, "Run Sound");
        ValidateClip(attackSound, "Attack Sound");
        ValidateClip(hurtSound, "Hurt Sound");
        ValidateClip(dieSound, "Die Sound");
    }

    private void ValidateClip(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"Cave Boss - {clipName} NOT ASSIGNED!");
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"Cave Boss - {clipName} NOT LOADED! LoadState: {clip.loadState}");
            return;
        }

        LogDebug($"✓ {clipName}: '{clip.name}' ({clip.length:F2}s)");
    }
    #endregion

    #region Looping Sound System
    /// <summary>
    /// Handles looping run sounds based on boss movement state
    /// </summary>
    private void UpdateLoopingSoundEffects()
    {
        if (bossAI == null) return;

        soundLoopTimer -= Time.deltaTime;

        // Determine if boss is running (check velocity)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        bool isCurrentlyRunning = rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f;

        // Stop looping sound if no longer running
        if (lastLoopingSound != null && !isCurrentlyRunning)
        {
            StopLoopingSounds();
            return;
        }

        // Play run sound if running
        if (isCurrentlyRunning && soundLoopTimer <= 0 && runSound != null)
        {
            if (lastLoopingSound != runSound)
            {
                PlayLoopingSound(runSound);
                LogDebug("Started looping run sound");
            }
        }

        wasRunning = isCurrentlyRunning;
    }

    /// <summary>
    /// Plays a looping sound effect (run)
    /// </summary>
    private void PlayLoopingSound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        // Only play if player is in range
        if (!IsPlayerInRange())
        {
            LogDebug($"Player out of range - sound muted");
            return;
        }

        audioSource.PlayOneShot(clip);
        lastLoopingSound = clip;
        soundLoopTimer = runSoundLoopInterval;
    }

    /// <summary>
    /// Stops all looping sounds
    /// </summary>
    private void StopLoopingSounds()
    {
        if (audioSource != null && audioSource.isPlaying && lastLoopingSound != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
            lastLoopingSound = null;
            soundLoopTimer = 0f;
            LogDebug("Stopped looping run sound");
        }
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

    #region Public Audio Trigger Methods (Called by CaveBossAI)
    /// <summary>
    /// Play attack sound with configurable delay
    /// </summary>
    public void PlayAttackSound()
    {
        if (attackSound != null)
        {
            StopLoopingSounds();
            
            // Play attack sound with delay
            if (attackSoundDelay > 0)
            {
                StartCoroutine(PlayDelayedAttackSound());
            }
            else
            {
                PlayOneTimeSoundEffect(attackSound);
                LogDebug("Playing Cave Boss attack sound (no delay)");
            }
        }
        else
        {
            Debug.LogWarning("Cave Boss - Attack sound not assigned!");
        }
    }

    /// <summary>
    /// Coroutine to play attack sound after delay
    /// </summary>
    private IEnumerator PlayDelayedAttackSound()
    {
        LogDebug($"Cave Boss attack sound scheduled - will play after {attackSoundDelay}s delay");
        
        yield return new WaitForSeconds(attackSoundDelay);
        
        PlayOneTimeSoundEffect(attackSound);
        LogDebug($"Playing Cave Boss attack sound (after {attackSoundDelay}s delay)");
    }

    /// <summary>
    /// Play hurt sound effect
    /// </summary>
    public void PlayHurtSound()
    {
        if (hurtSound != null)
        {
            StopLoopingSounds();
            PlayOneTimeSoundEffect(hurtSound);
            LogDebug("Playing Cave Boss hurt sound");
        }
    }

    /// <summary>
    /// Play death sound effect
    /// </summary>
    public void PlayDeathSound()
    {
        if (dieSound != null)
        {
            StopLoopingSounds();
            PlayOneTimeSoundEffect(dieSound);
            LogDebug("Playing Cave Boss death sound");
        }
    }

    /// <summary>
    /// Force stop all sounds
    /// </summary>
    public void StopAllSounds()
    {
        StopLoopingSounds();
        StopAllCoroutines(); // Stop any delayed attack sounds
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
        LogDebug("All sounds stopped");
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Plays a one-time sound effect (attack, hurt, die)
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

        // Ensure no looping sound interferes
        if (audioSource.isPlaying && lastLoopingSound != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        audioSource.PlayOneShot(clip);
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CaveBossAudio] {message}");
        }
    }
    #endregion

    #region Public Getters
    public bool IsPlayingSound() => audioSource != null && audioSource.isPlaying;
    #endregion
}