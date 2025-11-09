using UnityEngine;

/// <summary>
/// Fireball projectile that damages enemies
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class FireballProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f;
    
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    private int damage;
    private Vector2 direction;
    private Rigidbody2D rb;
    private CircleCollider2D fireballCollider;
    private bool hasHit = false;
    private float facingDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        fireballCollider = GetComponent<CircleCollider2D>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Configure rigidbody
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Ensure collider is trigger
        fireballCollider.isTrigger = true;
    }

    /// <summary>
    /// Initialize fireball with damage, direction, and facing direction
    /// </summary>
    public void Initialize(int damageValue, Vector2 moveDirection, float playerFacingDirection)
    {
        damage = damageValue;
        direction = moveDirection.normalized;
        facingDirection = playerFacingDirection;
        
        // Flip sprite based on direction
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingDirection < 0;
        }
        
        // Set velocity
        rb.linearVelocity = direction * speed;
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
        
        Debug.Log($"Fireball initialized: Damage={damage}, Direction={direction}, Speed={speed}, FacingDirection={facingDirection}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        Debug.Log($"Fireball hit: {other.name} with tag: {other.tag}");

        // NEW: Check for Evil Wizard
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
                    DestroyFireball();
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
                    DestroyFireball();
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
                    DestroyFireball();
                    return;
                }
            }
        }
        // Hit ground/walls - destroy fireball
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            Debug.Log($"Fireball hit {other.tag} - destroying");
            hasHit = true;
            DestroyFireball();
        }
    }

    // NEW: Validate Evil Wizard body hit
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

    private void DestroyFireball()
    {
        // Optional: Add explosion effect here
        Destroy(gameObject);
    }
}
