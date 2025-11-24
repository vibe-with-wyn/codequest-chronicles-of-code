using UnityEngine;

/// <summary>
/// Handles audio playback for enemy AI states and actions.
/// Works seamlessly with EnemyAI without modifying core logic.
/// Supports different enemy types (Skeleton, Golem, etc.)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EnemyAudioController : MonoBehaviour
{
    [Header("Enemy Type")]
    [SerializeField] private EnemyType enemyType = EnemyType.Skeleton;

    [Header("Movement Sounds (Looping)")]
    [SerializeField] private AudioClip walkSound;          // Used for patrol & chase
    [SerializeField] private float walkSoundLoopInterval = 0.5f;

    [Header("Combat Sounds (One-Shot)")]
    [SerializeField] private AudioClip attack1Sound;
    [SerializeField] private AudioClip attack2Sound;       // Only for Skeleton
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip dieSound;

    [Header("Audio Settings")]
    [SerializeField] private float volume = 1.0f;
    [SerializeField] private float pitch = 1.0f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Proximity Settings")]
    [SerializeField] private float maxAudibleDistance = 15f;  // Max distance player can hear enemy
    [SerializeField] private float minAudibleDistance = 8f;   // Distance for full volume
    [SerializeField] private bool useProximitySystem = true;
    [SerializeField] private AnimationCurve volumeFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // Core Components
    private AudioSource audioSource;
    private EnemyAI enemyAI;

    // Sound state tracking
    private float soundLoopTimer = 0f;
    private AudioClip lastLoopingSound = null;
    private bool wasWalking = false;
    private bool wasChasing = false;

    // Enemy type enumeration
    public enum EnemyType
    {
        Skeleton,
        Golem
    }

    private Transform player;

    void Start()
    {
        InitializeComponents();
        ConfigureAudioSource();
        ValidateAudioClips();
    }

    void Update()
    {
        if (enemyAI == null || enemyAI.IsDead()) return;

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
            LogDebug("AudioSource component added to enemy");
        }

        // Get EnemyAI reference
        enemyAI = GetComponent<EnemyAI>();
        if (enemyAI == null)
        {
            Debug.LogError($"EnemyAudioController on {gameObject.name}: EnemyAI component not found!");
        }

        // Find player reference
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

        LogDebug($"EnemyAudioController initialized for {enemyType}");
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
        LogDebug("=== Audio Clips Validation ===");
        ValidateClip(walkSound, "Walk Sound");
        ValidateClip(attack1Sound, "Attack 1 Sound");

        if (enemyType == EnemyType.Skeleton)
            ValidateClip(attack2Sound, "Attack 2 Sound (Skeleton only)");

        ValidateClip(hurtSound, "Hurt Sound");
        ValidateClip(dieSound, "Die Sound");
    }

    private void ValidateClip(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"{enemyType} - {clipName} NOT ASSIGNED!");
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError($"{enemyType} - {clipName} NOT LOADED! LoadState: {clip.loadState}");
            return;
        }

        LogDebug($"✓ {clipName}: '{clip.name}' ({clip.length:F2}s)");
    }
    #endregion

    #region Looping Sound System
    /// <summary>
    /// Handles looping walk/chase sounds based on enemy state
    /// </summary>
    private void UpdateLoopingSoundEffects()
    {
        if (enemyAI == null) return;

        soundLoopTimer -= Time.deltaTime;

        // Determine current movement state
        bool isCurrentlyWalking = enemyAI.IsPatrolling();
        bool isCurrentlyChasing = enemyAI.IsChasing();
        bool isMoving = isCurrentlyWalking || isCurrentlyChasing;

        // Stop looping sound if state changed to non-moving
        if (lastLoopingSound != null && !isMoving)
        {
            StopLoopingSounds();
            return;
        }

        // Play walk sound if moving (patrol or chase both use walk sound)
        if (isMoving && soundLoopTimer <= 0 && walkSound != null)
        {
            if (lastLoopingSound != walkSound)
            {
                PlayLoopingSound(walkSound);
                LogDebug($"Started looping walk sound (State: {(isCurrentlyChasing ? "Chasing" : "Patrolling")})");
            }
        }

        wasWalking = isCurrentlyWalking;
        wasChasing = isCurrentlyChasing;
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
            LogDebug($"Player out of range ({Vector3.Distance(transform.position, player.position):F1}m) - sound muted");
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

    #region Public Audio Trigger Methods (Called by EnemyAI)
    /// <summary>
    /// Play attack sound based on attack index
    /// </summary>
    public void PlayAttackSound(int attackIndex)
    {
        AudioClip attackSound = null;

        switch (enemyType)
        {
            case EnemyType.Skeleton:
                attackSound = attackIndex == 1 ? attack1Sound : attack2Sound;
                break;

            case EnemyType.Golem:
                attackSound = attack1Sound; // Golem only has one attack
                break;
        }

        if (attackSound != null)
        {
            StopLoopingSounds();
            PlayOneTimeSoundEffect(attackSound);
            LogDebug($"Playing {enemyType} Attack {attackIndex} sound");
        }
        else
        {
            Debug.LogWarning($"{enemyType} - Attack {attackIndex} sound not assigned!");
        }
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
            LogDebug($"Playing {enemyType} hurt sound");
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
            LogDebug($"Playing {enemyType} death sound");
        }
    }

    /// <summary>
    /// Force stop all sounds (useful for death/cleanup)
    /// </summary>
    public void StopAllSounds()
    {
        StopLoopingSounds();
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
            LogDebug($"Player out of range - sound effect muted");
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
    /// Get volume multiplier based on player distance (for additional control)
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

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[EnemyAudio - {enemyType}] {message}");
        }
    }
    #endregion

    #region Public Getters
    public EnemyType GetEnemyType() => enemyType;
    public bool IsPlayingSound() => audioSource != null && audioSource.isPlaying;
    #endregion
}