using UnityEngine;
using System.Collections;

/// <summary>
/// Handles audio playback for Evil Wizard states and actions.
/// Supports proximity-based audio to mimic real-life sound propagation.
/// States: Run, Hurt, Die, Attack1, Attack2
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EvilWizardAudioController : MonoBehaviour
{
    [Header("Movement Sounds (Looping)")]
    [SerializeField] private AudioClip runSound;
    [SerializeField] private float runSoundLoopInterval = 0.4f;

    [Header("Combat Sounds (One-Shot)")]
    [SerializeField] private AudioClip attack1Sound;
    [SerializeField] private AudioClip attack2Sound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip dieSound;

    [Header("Attack Sound Timing")]
    [Tooltip("Delay before playing attack sounds (seconds) - allows syncing with animation")]
    [SerializeField] private float attackSoundDelay = 0.3f;

    [Header("Audio Settings")]
    [SerializeField] private float volume = 1.0f;
    [SerializeField] private float pitch = 1.0f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Proximity Settings")]
    [SerializeField] private float maxAudibleDistance = 12f;  // Evil Wizard sounds travel medium distance
    [SerializeField] private float minAudibleDistance = 6f;
    [SerializeField] private bool useProximitySystem = true;
    [SerializeField] private AnimationCurve volumeFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // Core Components
    private AudioSource audioSource;
    private EvilWizardAI wizardAI;
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
            LogDebug("AudioSource component added to Evil Wizard");
        }

        // Get EvilWizardAI reference
        wizardAI = GetComponent<EvilWizardAI>();
        if (wizardAI == null)
        {
            Debug.LogError($"EvilWizardAudioController on {gameObject.name}: EvilWizardAI component not found!");
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

        LogDebug("EvilWizardAudioController initialized");
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
        LogDebug("=== Evil Wizard Audio Clips Validation ===");
        ValidateClip(runSound, "Run Sound");
        ValidateClip(attack1Sound, "Attack 1 Sound");
        ValidateClip(attack2Sound, "Attack 2 Sound");
        ValidateClip(hurtSound, "Hurt Sound");
        ValidateClip(dieSound, "Die Sound");
    }

    private void ValidateClip(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"Evil Wizard - {clipName} NOT ASSIGNED!");
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"Evil Wizard - {clipName} NOT LOADED! LoadState: {clip.loadState}");
            return;
        }

        LogDebug($"✓ {clipName}: '{clip.name}' ({clip.length:F2}s)");
    }
    #endregion

    #region Looping Sound System
    /// <summary>
    /// Handles looping run sounds based on wizard movement state
    /// </summary>
    private void UpdateLoopingSoundEffects()
    {
        if (wizardAI == null) return;

        soundLoopTimer -= Time.deltaTime;

        // Determine if wizard is running (check velocity)
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

    #region Public Audio Trigger Methods (Called by EvilWizardAI)
    /// <summary>
    /// Play attack 1 sound with configurable delay
    /// </summary>
    public void PlayAttack1Sound()
    {
        if (attack1Sound != null)
        {
            StopLoopingSounds();

            // Play attack sound with delay
            if (attackSoundDelay > 0)
            {
                StartCoroutine(PlayDelayedAttackSound(attack1Sound, "Attack1"));
            }
            else
            {
                PlayOneTimeSoundEffect(attack1Sound);
                LogDebug("Playing Evil Wizard attack 1 sound (no delay)");
            }
        }
        else
        {
            Debug.LogWarning("Evil Wizard - Attack 1 sound not assigned!");
        }
    }

    /// <summary>
    /// Play attack 2 sound with configurable delay
    /// </summary>
    public void PlayAttack2Sound()
    {
        if (attack2Sound != null)
        {
            StopLoopingSounds();

            // Play attack sound with delay
            if (attackSoundDelay > 0)
            {
                StartCoroutine(PlayDelayedAttackSound(attack2Sound, "Attack2"));
            }
            else
            {
                PlayOneTimeSoundEffect(attack2Sound);
                LogDebug("Playing Evil Wizard attack 2 sound (no delay)");
            }
        }
        else
        {
            Debug.LogWarning("Evil Wizard - Attack 2 sound not assigned!");
        }
    }

    /// <summary>
    /// Coroutine to play attack sound after delay
    /// </summary>
    private IEnumerator PlayDelayedAttackSound(AudioClip attackClip, string attackName)
    {
        LogDebug($"Evil Wizard {attackName} sound scheduled - will play after {attackSoundDelay}s delay");

        yield return new WaitForSeconds(attackSoundDelay);

        PlayOneTimeSoundEffect(attackClip);
        LogDebug($"Playing Evil Wizard {attackName} sound (after {attackSoundDelay}s delay)");
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
            LogDebug("Playing Evil Wizard hurt sound");
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
            LogDebug("Playing Evil Wizard death sound");
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
            Debug.Log($"[EvilWizardAudio] {message}");
        }
    }
    #endregion

    #region Public Getters
    public bool IsPlayingSound() => audioSource != null && audioSource.isPlaying;
    #endregion
}