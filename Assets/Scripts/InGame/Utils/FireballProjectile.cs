using UnityEngine;

/// <summary>
/// Fireball projectile that damages enemies with range limit and explosion animation
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class FireballProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float maxRange = 10f; // Maximum travel distance before exploding
    
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    
    [Header("Explosion Settings")]
    [Tooltip("Duration of explosion animation before destruction")]
    [SerializeField] private float explosionDuration = 0.5f;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private float impactSoundVolume = 1.0f;
    
    private int damage;
    private Vector2 direction;
    private Rigidbody2D rb;
    private CircleCollider2D fireballCollider;
    private bool hasHit = false;
    private bool isExploding = false;
    private float facingDirection;
    private Vector3 startPosition;
    private float distanceTraveled = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        fireballCollider = GetComponent<CircleCollider2D>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Auto-find AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                ConfigureAudioSource();
            }
        }
        
        // Configure rigidbody
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Ensure collider is trigger
        fireballCollider.isTrigger = true;
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null) return;

        audioSource.volume = 1.0f;
        audioSource.pitch = 1.0f;
        audioSource.spatialBlend = 0.0f; // 2D sound
        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // Track distance traveled and explode if max range exceeded
        if (!isExploding && !hasHit)
        {
            float frameDistance = rb.linearVelocity.magnitude * Time.deltaTime;
            distanceTraveled += frameDistance;
            
            if (distanceTraveled >= maxRange)
            {
                Debug.Log($"Fireball reached max range ({maxRange}m) - exploding");
                TriggerExplosion();
            }
        }
    }

    /// <summary>
    /// Initialize fireball with damage, direction, and facing direction
    /// </summary>
    public void Initialize(int damageValue, Vector2 moveDirection, float playerFacingDirection)
    {
        damage = damageValue;
        direction = moveDirection.normalized;
        facingDirection = playerFacingDirection;
        startPosition = transform.position;
        
        // FIXED: Use ONLY transform.localScale for flipping (remove spriteRenderer.flipX to avoid conflicts)
        if (facingDirection < 0)
        {
            // Facing left - flip to negative scale
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x); // Force negative
            transform.localScale = scale;
        }
        else
        {
            // Facing right - keep positive scale
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x); // Force positive
            transform.localScale = scale;
        }
        
        // Set velocity
        rb.linearVelocity = direction * speed;
        
        Debug.Log($"Fireball initialized: Damage={damage}, Direction={direction}, Speed={speed}, " +
                 $"FacingDirection={facingDirection}, Scale={transform.localScale.x}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || isExploding) return;

        Debug.Log($"Fireball hit: {other.name} with tag: {other.tag}");

        // Check for Evil Wizard
        if (other.CompareTag("EvilWizard") || other.name.Contains("Evil Wizard"))
        {
            if (IsValidWizardBodyHit(other))
            {
                EvilWizardAI wizardAI = other.GetComponent<EvilWizardAI>();
                if (wizardAI != null)
                {
                    wizardAI.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Fireball dealt {damage} damage to Evil Wizard {other.name}");
                    TriggerExplosion();
                    return;
                }
            }
            else
            {
                Debug.Log($"Fireball hit Evil Wizard detection zone - ignoring");
            }
        }
        // Check for Cave Boss
        else if (other.CompareTag("CaveBoss"))
        {
            if (IsValidBossBodyHit(other))
            {
                CaveBossAI bossAI = other.GetComponent<CaveBossAI>();
                if (bossAI != null)
                {
                    bossAI.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Fireball dealt {damage} damage to Cave Boss");
                    TriggerExplosion();
                    return;
                }
            }
        }
        // Check for regular enemies
        else if (other.CompareTag("Enemy"))
        {
            if (IsValidEnemyBodyHit(other))
            {
                EnemyAI enemyAI = other.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Fireball dealt {damage} damage to enemy {other.name}");
                    TriggerExplosion();
                    return;
                }
            }
        }
        // CLEANED: Only check for Ground tag (removed Wall check as per requirements)
        else if (other.CompareTag("Ground"))
        {
            Debug.Log($"Fireball hit ground - exploding");
            hasHit = true;
            TriggerExplosion();
        }
    }

    private bool IsValidWizardBodyHit(Collider2D hitCollider)
    {
        EvilWizardAI wizardAI = hitCollider.GetComponent<EvilWizardAI>();
        if (wizardAI != null)
        {
            bool isBodyHit = wizardAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = wizardAI.IsDetectionCollider(hitCollider);

            if (isBodyHit)
            {
                Debug.Log("✓ Fireball hit confirmed: Wizard body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                Debug.Log("✗ Fireball hit rejected: Wizard detection collider");
                return false;
            }
        }

        if (hitCollider is CapsuleCollider2D && !hitCollider.isTrigger)
        {
            Debug.Log("✓ Fireball hit confirmed: CapsuleCollider2D non-trigger");
            return true;
        }

        return false;
    }

    private bool IsValidBossBodyHit(Collider2D hitCollider)
    {
        CaveBossAI bossAI = hitCollider.GetComponent<CaveBossAI>();
        if (bossAI != null)
        {
            return bossAI.IsBodyCollider(hitCollider);
        }

        return hitCollider is CapsuleCollider2D && !hitCollider.isTrigger;
    }

    private bool IsValidEnemyBodyHit(Collider2D hitCollider)
    {
        EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            return enemyAI.IsBodyCollider(hitCollider);
        }

        return hitCollider is CapsuleCollider2D;
    }

    /// <summary>
    /// FIXED: Trigger explosion animation and schedule destruction
    /// NEW: Play impact sound when explosion triggers
    /// </summary>
    private void TriggerExplosion()
    {
        if (isExploding) return;
        
        isExploding = true;
        
        // Stop movement
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Disable collider to prevent multiple hits
        if (fireballCollider != null)
            fireballCollider.enabled = false;
        
        // Play impact sound BEFORE triggering animator
        PlayImpactSound();
        
        // Trigger explosion animation if animator exists
        if (animator != null && HasAnimatorParameter("Explode", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Explode");
            Debug.Log("Fireball explosion animation triggered");
        }
        else if (animator != null && HasAnimatorParameter("Hit", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hit");
            Debug.Log("Fireball hit animation triggered");
        }
        else
        {
            Debug.LogWarning("Fireball animator doesn't have 'Explode' or 'Hit' trigger - destroying immediately");
        }
        
        // Destroy after explosion animation completes
        Destroy(gameObject, explosionDuration);
        
        Debug.Log($"Fireball exploding at position {transform.position} after traveling {distanceTraveled:F2}m");
    }

    /// <summary>
    /// NEW: Play impact sound effect when fireball hits
    /// </summary>
    private void PlayImpactSound()
    {
        if (audioSource == null || impactSound == null)
            return;

        audioSource.volume = impactSoundVolume;
        audioSource.PlayOneShot(impactSound);
        Debug.Log($"Fireball impact sound played: {impactSound.name}");
    }

    /// <summary>
    /// Check if animator has a specific parameter
    /// </summary>
    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType paramType)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == paramType)
                return true;
        }
        return false;
    }
}
