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
        // Hit NPC Arin (Arin doesn't have health, but you can add logic later)
        else if (other.CompareTag("NPC") || other.name.Contains("Arin"))
        {
            hasHit = true;
            Debug.Log($"Boss attacked NPC Arin!");
            // TODO: Add Arin damage handling if needed
        }
    }

    void OnDisable()
    {
        Debug.Log("Boss attack collider disabled");
    }
}