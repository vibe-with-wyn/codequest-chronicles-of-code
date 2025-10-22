using UnityEngine;

public class ArinAttackCollider : MonoBehaviour
{
    private int damage;
    private bool hasHit = false;

    void OnEnable()
    {
        // Reset hit flag when collider is enabled for new attack
        hasHit = false;
        Debug.Log($"Arin attack collider enabled with damage: {damage}");
    }

    public void SetDamage(int damageValue)
    {
        damage = damageValue;
        hasHit = false;
        Debug.Log($"Arin attack collider damage set to: {damage}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return; // Prevent multiple hits from same attack
        
        Debug.Log($"Arin attack collider detected: {other.name} with tag: {other.tag} on GameObject: {other.gameObject.name}");
        
        // Check if it's the cave boss
        if (other.CompareTag("CaveBoss"))
        {
            // CRITICAL: Validate we hit the boss's body, not detection colliders
            if (IsValidBossBodyHit(other))
            {
                // Try CaveBossAI first
                CaveBossAI bossAI = other.GetComponent<CaveBossAI>();
                if (bossAI != null)
                {
                    bossAI.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Arin dealt {damage} damage to Cave Boss via CaveBossAI - HIT CONFIRMED ON BODY");
                    return;
                }
                
                // Fallback: Try EnemyHealth directly
                EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Arin dealt {damage} damage to Cave Boss via EnemyHealth - HIT CONFIRMED ON BODY");
                    return;
                }
                
                Debug.LogError($"Cave Boss {other.name} has neither CaveBossAI nor EnemyHealth component!");
            }
            else
            {
                Debug.Log($"Arin attack hit Cave Boss but NOT on body collider (hit detection zone) - DAMAGE IGNORED");
            }
        }
        // Check if it's a regular enemy
        else if (other.CompareTag("Enemy"))
        {
            // CRITICAL: Validate we hit the enemy's body, not detection colliders
            if (IsValidEnemyBodyHit(other))
            {
                EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
                
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Arin dealt {damage} damage to enemy {other.name} - HIT CONFIRMED ON BODY");
                }
                else
                {
                    Debug.LogWarning($"No EnemyHealth component found on enemy {other.name}");
                }
            }
            else
            {
                Debug.Log($"Arin attack hit enemy but NOT on body collider (hit detection zone) - DAMAGE IGNORED");
            }
        }
    }

    /// <summary>
    /// Check if we hit the Cave Boss's body collider (not detection colliders)
    /// </summary>
    private bool IsValidBossBodyHit(Collider2D hitCollider)
    {
        // Method 1: Check through CaveBossAI component
        CaveBossAI bossAI = hitCollider.GetComponent<CaveBossAI>();
        if (bossAI != null)
        {
            bool isBodyHit = bossAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = bossAI.IsDetectionCollider(hitCollider);
            
            Debug.Log($"Boss collision analysis - Body: {isBodyHit}, Detection: {isDetectionHit}");
            
            if (isBodyHit)
            {
                Debug.Log("✓ Arin hit confirmed: Boss body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                Debug.Log("✗ Arin hit rejected: Boss detection collider");
                return false;
            }
        }

        // Method 2: Check by collider type (fallback)
        if (hitCollider is CapsuleCollider2D && !hitCollider.isTrigger)
        {
            Debug.Log("✓ Arin hit confirmed: CapsuleCollider2D non-trigger (likely boss body)");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            Debug.Log("✗ Arin hit rejected: CircleCollider2D trigger (likely detection zone)");
            return false;
        }

        // Method 3: Check by GameObject name
        string colliderName = hitCollider.gameObject.name.ToLower();
        if (colliderName.Contains("detection") || colliderName.Contains("trigger"))
        {
            Debug.Log("✗ Arin hit rejected: GameObject name suggests detection/trigger");
            return false;
        }

        // Default: allow hit but warn
        Debug.LogWarning($"Unable to determine collider type for boss {hitCollider.name}, allowing hit as fallback");
        return true;
    }

    /// <summary>
    /// Check if we hit a regular enemy's body collider (not detection colliders)
    /// </summary>
    private bool IsValidEnemyBodyHit(Collider2D hitCollider)
    {
        // Method 1: Check through EnemyAI component
        EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            bool isBodyHit = enemyAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = enemyAI.IsDetectionCollider(hitCollider);
            
            Debug.Log($"Enemy collision analysis - Body: {isBodyHit}, Detection: {isDetectionHit}");
            
            if (isBodyHit)
            {
                Debug.Log("✓ Arin hit confirmed: Enemy body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                Debug.Log("✗ Arin hit rejected: Enemy detection collider");
                return false;
            }
        }

        // Method 2: Check by collider type (fallback)
        if (hitCollider is CapsuleCollider2D)
        {
            Debug.Log("✓ Arin hit confirmed: CapsuleCollider2D (likely enemy body)");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            Debug.Log("✗ Arin hit rejected: CircleCollider2D trigger (likely detection zone)");
            return false;
        }

        // Default: allow hit
        Debug.LogWarning($"Unable to determine collider type for enemy {hitCollider.name}, allowing hit as fallback");
        return true;
    }

    void OnDisable()
    {
        Debug.Log("Arin attack collider disabled");
    }
}