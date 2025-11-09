using UnityEngine;

public class PlayerAttackCollider : MonoBehaviour
{
    private int damage;
    private bool hasHit = false;

    void OnEnable()
    {
        hasHit = false;
        Debug.Log($"Player attack collider enabled with damage: {damage}");
    }

    public void SetDamage(int damageValue)
    {
        damage = damageValue;
        hasHit = false;
        Debug.Log($"Player attack collider damage set to: {damage}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        Debug.Log($"Player attack collider detected: {other.name} with tag: {other.tag}");

        // Check for Cave Boss
        if (other.CompareTag("CaveBoss"))
        {
            if (IsValidBossBodyHit(other))
            {
                CaveBossAI bossAI = other.GetComponent<CaveBossAI>();
                if (bossAI != null)
                {
                    bossAI.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Player dealt {damage} damage to Cave Boss {other.name}");
                    ShowDamageEffect(other.transform.position);
                    return;
                }
            }
        }
        // NEW: Check for Evil Wizard
        else if (other.CompareTag("EvilWizard") || other.name.Contains("Evil Wizard"))
        {
            if (IsValidWizardBodyHit(other))
            {
                EvilWizardAI wizardAI = other.GetComponent<EvilWizardAI>();
                if (wizardAI != null)
                {
                    wizardAI.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Player dealt {damage} damage to Evil Wizard {other.name} - HIT CONFIRMED ON BODY");
                    ShowDamageEffect(other.transform.position);
                    return;
                }
                else
                {
                    Debug.LogWarning($"Evil Wizard {other.name} does not have EvilWizardAI component!");
                }
            }
            else
            {
                Debug.Log($"Player attack hit Evil Wizard but NOT on body collider (likely detection zone) - DAMAGE IGNORED");
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
                    Debug.Log($"Player dealt {damage} damage to enemy {other.name}");
                    ShowDamageEffect(other.transform.position);
                }
            }
        }
    }

    // NEW: Validate Evil Wizard body hit
    private bool IsValidWizardBodyHit(Collider2D hitCollider)
    {
        // Method 1: Check through EvilWizardAI component
        EvilWizardAI wizardAI = hitCollider.GetComponent<EvilWizardAI>();
        if (wizardAI != null)
        {
            bool isBodyHit = wizardAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = wizardAI.IsDetectionCollider(hitCollider);

            Debug.Log($"Wizard collision analysis - Body: {isBodyHit}, Detection: {isDetectionHit}");

            if (isBodyHit)
            {
                Debug.Log("✓ Player hit confirmed: Wizard body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                Debug.Log("✗ Player hit rejected: Wizard detection collider");
                return false;
            }
        }

        // Method 2: Check by collider type (fallback)
        if (hitCollider is CapsuleCollider2D && !hitCollider.isTrigger)
        {
            Debug.Log("✓ Player hit confirmed: CapsuleCollider2D non-trigger (likely wizard body)");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            Debug.Log("✗ Player hit rejected: CircleCollider2D trigger (likely detection zone)");
            return false;
        }

        // Method 3: Check by GameObject name
        string colliderName = hitCollider.gameObject.name.ToLower();
        if (colliderName.Contains("detection") || colliderName.Contains("trigger") || colliderName.Contains("attack"))
        {
            Debug.Log("✗ Player hit rejected: GameObject name suggests detection/trigger/attack collider");
            return false;
        }

        // Default: allow hit but warn
        Debug.LogWarning($"Unable to determine collider type for wizard {hitCollider.name}, allowing hit as fallback");
        return true;
    }

    private bool IsValidBossBodyHit(Collider2D hitCollider)
    {
        CaveBossAI bossAI = hitCollider.GetComponent<CaveBossAI>();
        if (bossAI != null)
        {
            bool isBodyHit = bossAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = bossAI.IsDetectionCollider(hitCollider);

            Debug.Log($"Boss collision analysis - Body: {isBodyHit}, Detection: {isDetectionHit}");

            if (isBodyHit)
            {
                Debug.Log("✓ Player hit confirmed: Boss body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                Debug.Log("✗ Player hit rejected: Boss detection collider");
                return false;
            }
        }

        if (hitCollider is CapsuleCollider2D && !hitCollider.isTrigger)
        {
            Debug.Log("✓ Player hit confirmed: CapsuleCollider2D non-trigger");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            Debug.Log("✗ Player hit rejected: CircleCollider2D trigger");
            return false;
        }

        string colliderName = hitCollider.gameObject.name.ToLower();
        if (colliderName.Contains("detection") || colliderName.Contains("trigger"))
        {
            Debug.Log("✗ Player hit rejected: GameObject name suggests detection/trigger");
            return false;
        }

        Debug.LogWarning($"Unable to determine collider type for boss {hitCollider.name}, allowing hit as fallback");
        return true;
    }

    private bool IsValidEnemyBodyHit(Collider2D hitCollider)
    {
        EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            bool isBodyHit = enemyAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = enemyAI.IsDetectionCollider(hitCollider);

            Debug.Log($"Collision analysis - Body: {isBodyHit}, Detection: {isDetectionHit}");

            if (isBodyHit)
            {
                Debug.Log("✓ Hit confirmed: Enemy body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                Debug.Log("✗ Hit rejected: Enemy detection collider");
                return false;
            }
        }

        if (hitCollider is CapsuleCollider2D)
        {
            Debug.Log("✓ Hit confirmed: CapsuleCollider2D");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            Debug.Log("✗ Hit rejected: CircleCollider2D trigger");
            return false;
        }

        if (hitCollider.gameObject.name.ToLower().Contains("detection") ||
            hitCollider.gameObject.name.ToLower().Contains("trigger"))
        {
            Debug.Log("✗ Hit rejected: GameObject name suggests detection/trigger");
            return false;
        }

        Debug.LogWarning($"Unable to determine collider type for {hitCollider.name}, allowing hit as fallback");
        return true;
    }

    void OnDisable()
    {
        Debug.Log("Player attack collider disabled");
    }

    private void ShowDamageEffect(Vector3 position)
    {
        Debug.Log($"Player damage effect at position: {position}");
    }
}