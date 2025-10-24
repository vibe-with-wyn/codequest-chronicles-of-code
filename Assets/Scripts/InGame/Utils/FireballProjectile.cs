using UnityEngine;
using System.Collections;

public class FireballProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float explosionAnimationDuration = 0.5f;
    
    [Header("Visual Settings")]
    [SerializeField] private bool useScaleFlipping = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private int damage;
    private Vector2 direction;
    private Vector2 startPosition;
    private Rigidbody2D rb;
    private CircleCollider2D projectileCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool hasHit = false;
    private bool hasExploded = false;
    private bool isInitialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<CircleCollider2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (animator == null)
        {
            Debug.LogError("Animator not found in Fireball or its children!");
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning("SpriteRenderer not found in Fireball or its children! Visual flipping may not work.");
        }

        if (projectileCollider != null)
        {
            projectileCollider.isTrigger = true;
        }

        if (enableDebugLogs)
            Debug.Log("Fireball Awake completed");
    }

    void Start()
    {
        if (!isInitialized)
        {
            Debug.LogError("Fireball Start() called but Initialize() was never called!");
            Destroy(gameObject);
            return;
        }

        startPosition = transform.position;
        
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
            if (enableDebugLogs)
                Debug.Log($"Fireball velocity set to: {direction * speed}");
        }
        
        Destroy(gameObject, lifeTime);
        
        if (enableDebugLogs)
            Debug.Log($"Fireball projectile launched with damage: {damage}, direction: {direction}, position: {transform.position}");
    }

    void Update()
    {
        if (!isInitialized || hasExploded) return;

        float distanceTraveled = Vector2.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            if (enableDebugLogs)
                Debug.Log($"Fireball reached max distance ({distanceTraveled:F2}/{maxDistance}) - exploding");
            ExplodeFireball();
        }
    }

    public void Initialize(int projectileDamage, Vector2 projectileDirection, float playerFacingDirection = 1f)
    {
        damage = projectileDamage;
        direction = projectileDirection.normalized;
        hasHit = false;
        hasExploded = false;
        isInitialized = true;
        
        SetVisualOrientation(playerFacingDirection);
        gameObject.SetActive(true);
        
        if (enableDebugLogs)
            Debug.Log($"Fireball initialized: Damage={damage}, Direction={direction}, FacingDirection={playerFacingDirection}, Position={transform.position}");
    }

    private void SetVisualOrientation(float facingDirection)
    {
        if (useScaleFlipping)
        {
            Vector3 currentScale = transform.localScale;
            if (facingDirection < 0)
            {
                currentScale.x = Mathf.Abs(currentScale.x) * -1f;
            }
            else
            {
                currentScale.x = Mathf.Abs(currentScale.x);
            }
            transform.localScale = currentScale;
            
            if (enableDebugLogs)
                Debug.Log($"Fireball scale set to: {currentScale} (facing: {facingDirection})");
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingDirection < 0;
            
            if (enableDebugLogs)
                Debug.Log($"Fireball sprite flipX set to: {spriteRenderer.flipX} (facing: {facingDirection})");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("Cannot set fireball orientation - no SpriteRenderer found and scale flipping disabled");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || hasExploded || !isInitialized) return;

        if (enableDebugLogs)
            Debug.Log($"Fireball collision detected with: {other.name} (tag: {other.tag}) on GameObject: {other.gameObject.name}");

        // NEW: Hit Cave Boss
        if (other.CompareTag("CaveBoss"))
        {
            if (IsValidBossBodyHit(other))
            {
                CaveBossAI bossAI = other.GetComponent<CaveBossAI>();
                if (bossAI != null)
                {
                    bossAI.TakeDamage(damage);
                    hasHit = true;
                    if (enableDebugLogs)
                        Debug.Log($"Fireball dealt {damage} damage to Cave Boss {other.name}");
                    ExplodeFireball();
                    return;
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"Cave Boss {other.name} has no CaveBossAI component!");
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"Fireball hit boss detection zone, not body - continuing flight");
                return;
            }
        }
        // Hit regular enemy
        else if (other.CompareTag("Enemy"))
        {
            if (IsValidEnemyBodyHit(other))
            {
                EnemyAI enemyAI = other.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.TakeDamage(damage);
                    hasHit = true;
                    if (enableDebugLogs)
                        Debug.Log($"Fireball dealt {damage} damage to enemy {other.name}");
                    ExplodeFireball();
                    return;
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"Enemy {other.name} has no EnemyAI component!");
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"Fireball hit enemy detection zone, not body - continuing flight");
                return;
            }
        }
        // Hit ground or obstacles
        else if (other.CompareTag("Ground") || other.CompareTag("Wall") || other.CompareTag("Platform"))
        {
            if (enableDebugLogs)
                Debug.Log($"Fireball hit {other.tag} ({other.name}) - exploding");
            ExplodeFireball();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"Fireball hit {other.tag} ({other.name}) - ignoring collision");
        }
    }

    // NEW: Validate boss body hit
    private bool IsValidBossBodyHit(Collider2D hitCollider)
    {
        CaveBossAI bossAI = hitCollider.GetComponent<CaveBossAI>();
        if (bossAI != null)
        {
            bool isBodyHit = bossAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = bossAI.IsDetectionCollider(hitCollider);
            
            if (isBodyHit)
            {
                if (enableDebugLogs)
                    Debug.Log("✓ Fireball hit confirmed: Boss body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                if (enableDebugLogs)
                    Debug.Log("✗ Fireball hit rejected: Boss detection collider");
                return false;
            }
        }

        // Fallback logic
        if (hitCollider is CapsuleCollider2D && !hitCollider.isTrigger)
        {
            if (enableDebugLogs)
                Debug.Log("✓ Fireball hit confirmed: CapsuleCollider2D non-trigger (assumed boss body)");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            if (enableDebugLogs)
                Debug.Log("✗ Fireball hit rejected: CircleCollider2D trigger (assumed detection zone)");
            return false;
        }

        // Default allow hit
        if (enableDebugLogs)
            Debug.Log("✓ Fireball hit allowed: Unknown collider type, defaulting to allow");
        return true;
    }

    private bool IsValidEnemyBodyHit(Collider2D hitCollider)
    {
        EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            bool isBodyHit = enemyAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = enemyAI.IsDetectionCollider(hitCollider);
            
            if (isBodyHit)
            {
                if (enableDebugLogs)
                    Debug.Log("✓ Fireball hit confirmed: Enemy body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                if (enableDebugLogs)
                    Debug.Log("✗ Fireball hit rejected: Enemy detection collider");
                return false;
            }
        }

        if (hitCollider is CapsuleCollider2D)
        {
            if (enableDebugLogs)
                Debug.Log("✓ Fireball hit confirmed: CapsuleCollider2D (assumed enemy body)");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            if (enableDebugLogs)
                Debug.Log("✗ Fireball hit rejected: CircleCollider2D trigger (assumed detection zone)");
            return false;
        }

        if (enableDebugLogs)
            Debug.Log("✓ Fireball hit allowed: Unknown collider type, defaulting to allow");
        return true;
    }

    private void ExplodeFireball()
    {
        if (hasExploded) return;
        
        hasExploded = true;
        
        if (enableDebugLogs)
            Debug.Log("Fireball exploding...");
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
        }
        
        if (animator != null && HasAnimatorParameter("Hit", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hit");
            if (enableDebugLogs)
                Debug.Log("Fireball explosion animation triggered");
            
            StartCoroutine(DestroyAfterExplosion());
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("Hit trigger not found in fireball animator - destroying immediately");
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterExplosion()
    {
        yield return new WaitForSeconds(explosionAnimationDuration);
        
        if (enableDebugLogs)
            Debug.Log("Fireball destroyed after explosion animation");
        
        Destroy(gameObject);
    }

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

    void OnDestroy()
    {
        if (enableDebugLogs)
            Debug.Log($"Fireball projectile destroyed at position: {transform.position}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
        
        if (isInitialized)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }
}
