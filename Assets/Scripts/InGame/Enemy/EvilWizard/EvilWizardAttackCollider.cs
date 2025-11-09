using UnityEngine;

/// <summary>
/// Evil Wizard's attack collider - damages player when active
/// Attached to the WizardAttackCollider child GameObject
/// </summary>
public class EvilWizardAttackCollider : MonoBehaviour
{
    private int damage;
    private bool hasHit = false;

    void OnEnable()
    {
        hasHit = false;
        Debug.Log("[EvilWizardAttack] Attack collider enabled - ready to hit player");
    }

    public void SetDamage(int damageValue)
    {
        damage = damageValue;
        Debug.Log($"[EvilWizardAttack] Damage set to {damage}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
        {
            Debug.Log("[EvilWizardAttack] Already hit this attack cycle - ignoring");
            return;
        }

        if (other.CompareTag("Player"))
        {
            Debug.Log($"[EvilWizardAttack] Collided with: {other.gameObject.name} (Tag: {other.tag})");

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                hasHit = true;
                playerHealth.TakeDamage(damage);
                Debug.Log($"[EvilWizardAttack] ✓ HIT! Dealt {damage} damage to player");

                // Trigger hurt animation on player
                PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    playerMovement.TriggerHurt();
                }
            }
            else
            {
                Debug.LogWarning($"[EvilWizardAttack] Player has no PlayerHealth component!");
            }
        }
    }

    void OnDisable()
    {
        Debug.Log("[EvilWizardAttack] Attack collider disabled");
    }
}