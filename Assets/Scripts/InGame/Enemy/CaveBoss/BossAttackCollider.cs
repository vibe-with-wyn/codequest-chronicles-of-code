using UnityEngine;

public class BossAttackCollider : MonoBehaviour
{
    private int damage;
    private bool hasHit = false;

    void OnEnable()
    {
        hasHit = false;
        Debug.Log($"Boss attack collider enabled with damage: {damage}");
    }

    public void SetDamage(int damageValue)
    {
        damage = damageValue;
        hasHit = false;
        Debug.Log($"Boss attack collider damage set to: {damage}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
        Debug.Log($"Boss attack collider detected: {other.name} with tag: {other.tag}");
        
        // Hit player - VALIDATE BODY COLLIDER!
        if (other.CompareTag("Player"))
        {
            // CRITICAL: Only damage if we hit the player's BODY, not detection zone!
            if (IsValidPlayerBodyHit(other))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                
                if (playerHealth != null && playerHealth.IsAlive())
                {
                    playerHealth.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Boss dealt {damage} damage to player - HIT CONFIRMED ON BODY!");
                }
            }
            else
            {
                Debug.Log($"Boss attack hit player detection zone - DAMAGE IGNORED");
            }
        }
        // Hit NPC Arin - VALIDATE BODY COLLIDER!
        else if (other.CompareTag("NPC") || other.name.Contains("Arin"))
        {
            // CRITICAL: Only damage if we hit Arin's BODY, not detection zone!
            if (IsValidNPCBodyHit(other))
            {
                ArinNPCAI arinAI = other.GetComponent<ArinNPCAI>();
                if (arinAI != null)
                {
                    arinAI.TakeHit();
                    hasHit = true;
                    Debug.Log($"Boss hit NPC Arin - hurt animation triggered - HIT CONFIRMED ON BODY!");
                }
                else
                {
                    Debug.LogWarning($"NPC {other.name} doesn't have ArinNPCAI component!");
                }
            }
            else
            {
                Debug.Log($"Boss attack hit NPC detection zone - DAMAGE IGNORED");
            }
        }
    }

    /// <summary>
    /// Validate that we hit the player's BODY collider, not their detection zones
    /// </summary>
    private bool IsValidPlayerBodyHit(Collider2D hitCollider)
    {
        // Method 1: Check by collider type
        // Player's body is typically a CapsuleCollider2D (solid)
        // Detection/UI zones are typically CircleCollider2D (triggers)
        if (hitCollider is CapsuleCollider2D && !hitCollider.isTrigger)
        {
            Debug.Log("✓ Boss hit confirmed: Player body collider (CapsuleCollider2D)");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            Debug.Log("✗ Boss hit rejected: Player detection/UI collider (CircleCollider2D trigger)");
            return false;
        }

        // Method 2: Check by GameObject name
        string colliderName = hitCollider.gameObject.name.ToLower();
        if (colliderName.Contains("detection") || colliderName.Contains("trigger") || colliderName.Contains("ui"))
        {
            Debug.Log("✗ Boss hit rejected: GameObject name suggests detection/trigger/UI zone");
            return false;
        }

        // Method 3: Check PlayerMovement component reference
        // The player's BODY collider should be on the same GameObject as PlayerMovement
        PlayerMovement playerMovement = hitCollider.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            Debug.Log("✓ Boss hit confirmed: Same GameObject as PlayerMovement (likely body)");
            return true;
        }

        // Default: allow hit but warn
        Debug.LogWarning($"Unable to determine if player collider {hitCollider.name} is body or detection - allowing hit as fallback");
        return true;
    }

    /// <summary>
    /// Validate that we hit Arin's BODY collider, not detection zones
    /// </summary>
    private bool IsValidNPCBodyHit(Collider2D hitCollider)
    {
        // Method 1: Check by collider type
        // Arin's body is typically a CapsuleCollider2D (solid)
        // Detection zones are CircleCollider2D (triggers)
        if (hitCollider is CapsuleCollider2D && !hitCollider.isTrigger)
        {
            Debug.Log("✓ Boss hit confirmed: NPC body collider (CapsuleCollider2D)");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            Debug.Log("✗ Boss hit rejected: NPC detection collider (CircleCollider2D trigger)");
            return false;
        }

        // Method 2: Check by GameObject name
        string colliderName = hitCollider.gameObject.name.ToLower();
        if (colliderName.Contains("detection") || colliderName.Contains("trigger"))
        {
            Debug.Log("✗ Boss hit rejected: GameObject name suggests detection/trigger");
            return false;
        }

        // Method 3: Check ArinNPCAI component reference
        // Arin's BODY collider should be on the same GameObject as ArinNPCAI
        ArinNPCAI arinAI = hitCollider.GetComponent<ArinNPCAI>();
        if (arinAI != null)
        {
            Debug.Log("✓ Boss hit confirmed: Same GameObject as ArinNPCAI (likely body)");
            return true;
        }

        // Default: allow hit but warn
        Debug.LogWarning($"Unable to determine if NPC collider {hitCollider.name} is body or detection - allowing hit as fallback");
        return true;
    }

    void OnDisable()
    {
        Debug.Log("Boss attack collider disabled");
    }
}