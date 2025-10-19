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
        
        Debug.Log($"Arin attack collider detected: {other.name} with tag: {other.tag}");
        
        // Check if it's the cave boss or enemy
        if (other.CompareTag("CaveBoss") || other.CompareTag("Enemy"))
        {
            // Try to get EnemyHealth component
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                hasHit = true;
                Debug.Log($"Arin dealt {damage} damage to {other.name}");
            }
            else
            {
                Debug.LogWarning($"No EnemyHealth component found on {other.name}");
            }
        }
    }

    void OnDisable()
    {
        Debug.Log("Arin attack collider disabled");
    }
}