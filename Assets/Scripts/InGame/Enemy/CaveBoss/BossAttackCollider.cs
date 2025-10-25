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
        
        // Hit player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            
            if (playerHealth != null && playerHealth.IsAlive())
            {
                playerHealth.TakeDamage(damage);
                hasHit = true;
                Debug.Log($"Boss dealt {damage} damage to player!");
            }
        }
        // NEW: Hit NPC Arin (triggers hurt animation, no damage/health)
        else if (other.CompareTag("NPC") || other.name.Contains("Arin"))
        {
            ArinNPCAI arinAI = other.GetComponent<ArinNPCAI>();
            if (arinAI != null)
            {
                arinAI.TakeHit();
                hasHit = true;
                Debug.Log($"Boss hit NPC Arin - hurt animation triggered!");
            }
            else
            {
                Debug.LogWarning($"NPC {other.name} doesn't have ArinNPCAI component!");
            }
        }
    }

    void OnDisable()
    {
        Debug.Log("Boss attack collider disabled");
    }
}