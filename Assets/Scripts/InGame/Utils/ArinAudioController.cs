using UnityEngine;
using System.Collections;

/// <summary>
/// Handles audio playback for NPC Arin's states and actions.
/// Supports proximity-based audio to mimic real-life sound propagation.
/// States: Walk, Hurt, and 4 attack sounds (Attack1-4)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ArinAudioController : MonoBehaviour
{
    [Header("Movement Sounds (Looping)")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private float walkSoundLoopInterval = 0.5f;

    [Header("Combat Sounds (One-Shot)")]
    [SerializeField] private AudioClip attack1Sound;  // Water Blast
    [SerializeField] private AudioClip attack2Sound;  // Ice Shard
    [SerializeField] private AudioClip attack3Sound;  // Tidal Wave
    [SerializeField] private AudioClip attack4Sound;  // Frost Nova
    [SerializeField] private AudioClip hurtSound;

    [Header("Attack Sound Timing")]
    [Tooltip("Delay before playing attack sound (seconds) - allows syncing with animation")]
    [SerializeField] private float attackSoundDelay = 0.2f;

    [Header("Audio Settings")]
    [SerializeField] private float volume = 1.0f;
    [SerializeField] private float pitch = 1.0f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Proximity Settings")]
    [SerializeField] private float maxAudibleDistance = 15f;
    [SerializeField] private float minAudibleDistance = 8f;
    [SerializeField] private bool useProximitySystem = true;
    [SerializeField] private AnimationCurve volumeFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // Core Components
    private AudioSource audioSource;
    private ArinNPCAI arinAI;
    private Transform player;

    // Sound state tracking
    private float soundLoopTimer = 0f;
    private AudioClip lastLoopingSound = null;
    private bool wasMoving = false;

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
            LogDebug("AudioSource component added to Arin");
        }

        // Get ArinNPCAI reference
        arinAI = GetComponent<ArinNPCAI>();
        if (arinAI == null)
        {
            Debug.LogError($"ArinAudioController on {gameObject.name}: ArinNPCAI component not found!");
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

        LogDebug("ArinAudioController initialized");
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
        LogDebug("=== Arin Audio Clips Validation ===");
        ValidateClip(walkSound, "Walk Sound");
        ValidateClip(attack1Sound, "Attack 1 Sound (Water Blast)");
        ValidateClip(attack2Sound, "Attack 2 Sound (Ice Shard)");
        ValidateClip(attack3Sound, "Attack 3 Sound (Tidal Wave)");
        ValidateClip(attack4Sound, "Attack 4 Sound (Frost Nova)");
        ValidateClip(hurtSound, "Hurt Sound");
    }

    private void ValidateClip(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"Arin - {clipName} NOT ASSIGNED!");
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"Arin - {clipName} NOT LOADED! LoadState: {clip.loadState}");
            return;
        }

        LogDebug($"✓ {clipName}: '{clip.name}' ({clip.length:F2}s)");
    }
    #endregion

    #region Looping Sound System
    /// <summary>
    /// Handles looping walk sounds based on Arin's movement state
    /// </summary>
    private void UpdateLoopingSoundEffects()
    {
        if (arinAI == null) return;

        soundLoopTimer -= Time.deltaTime;

        // Determine if Arin is moving (check velocity)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        bool isCurrentlyMoving = rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f;

        // Stop looping sound if no longer moving
        if (lastLoopingSound != null && !isCurrentlyMoving)
        {
            StopLoopingSounds();
            return;
        }

        // Play walk sound if moving
        if (isCurrentlyMoving && soundLoopTimer <= 0 && walkSound != null)
        {
            if (lastLoopingSound != walkSound)
            {
                PlayLoopingSound(walkSound);
                LogDebug("Started looping walk sound");
            }
        }

        wasMoving = isCurrentlyMoving;
    }

    /// <summary>
    /// Plays a looping sound effect (walk)
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
        soundLoopTimer = walkSoundLoopInterval;
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
            LogDebug("Stopped looping walk sound");
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

    #region Public Audio Trigger Methods (Called by ArinNPCAI)
    /// <summary>
    /// Play attack sound based on attack index (1-4) with configurable delay
    /// </summary>
    public void PlayAttackSound(int attackIndex)
    {
        AudioClip attackSound = attackIndex switch
        {
            1 => attack1Sound,
            2 => attack2Sound,
            3 => attack3Sound,
            4 => attack4Sound,
            _ => null
        };

        if (attackSound != null)
        {
            StopLoopingSounds();
            
            // Play attack sound with delay
            if (attackSoundDelay > 0)
            {
                StartCoroutine(PlayDelayedAttackSound(attackSound, attackIndex));
            }
            else
            {
                PlayOneTimeSoundEffect(attackSound);
                LogDebug($"Playing Arin Attack {attackIndex} sound (no delay)");
            }
        }
        else
        {
            Debug.LogWarning($"Arin - Attack {attackIndex} sound not assigned!");
        }
    }

    /// <summary>
    /// Coroutine to play attack sound after delay
    /// </summary>
    private IEnumerator PlayDelayedAttackSound(AudioClip attackSound, int attackIndex)
    {
        LogDebug($"Arin Attack {attackIndex} sound scheduled - will play after {attackSoundDelay}s delay");
        
        yield return new WaitForSeconds(attackSoundDelay);
        
        PlayOneTimeSoundEffect(attackSound);
        LogDebug($"Playing Arin Attack {attackIndex} sound (after {attackSoundDelay}s delay)");
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
            LogDebug("Playing Arin hurt sound");
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
    /// Plays a one-time sound effect (attack, hurt)
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
            Debug.Log($"[ArinAudio] {message}");
        }
    }
    #endregion

    #region Public Getters
    public bool IsPlayingSound() => audioSource != null && audioSource.isPlaying;
    #endregion
}