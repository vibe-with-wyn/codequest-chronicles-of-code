using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHP = 100;
    
    [Header("Collider References")] // NEW: Serializable collider validation
    [SerializeField] private CapsuleCollider2D bodyCollider; // Should match EnemyAI
    [SerializeField] private CircleCollider2D detectionCollider; // Should match EnemyAI
    
    private int currentHP;
    private bool isDead = false;

    void Start()
    {
        currentHP = maxHP;
        ValidateColliderReferences();
        Debug.Log($"EnemyHealth initialized on {gameObject.name}: {currentHP}/{maxHP} HP");
    }

    private void ValidateColliderReferences()
    {
        // Auto-find colliders if not assigned
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<CapsuleCollider2D>();
            if (bodyCollider != null)
                Debug.Log("EnemyHealth: Auto-found body collider");
        }

        if (detectionCollider == null)
        {
            detectionCollider = GetComponent<CircleCollider2D>();
            if (detectionCollider != null)
                Debug.Log("EnemyHealth: Auto-found detection collider");
        }

        // Cross-reference with EnemyAI
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            if (bodyCollider != enemyAI.GetBodyCollider())
            {
                Debug.LogWarning("EnemyHealth and EnemyAI body colliders don't match! This may cause issues.");
            }
            
            if (detectionCollider != enemyAI.GetDetectionCollider())
            {
                Debug.LogWarning("EnemyHealth and EnemyAI detection colliders don't match! This may cause issues.");
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        Debug.Log($"EnemyHealth.TakeDamage called - Current state: HP={currentHP}, Dead={isDead}");
        
        int previousHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - damage);
        
        Debug.Log($"Enemy {gameObject.name} took {damage} damage. HP: {previousHP} -> {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public int GetCurrentHealth() => currentHP;
    public int GetMaxHealth() => maxHP;
    public bool IsAlive() => !isDead && currentHP > 0;

    /// <summary>
    /// Validate that damage should be applied to this enemy based on the collider hit
    /// </summary>
    public bool ShouldTakeDamageFromCollider(Collider2D hitCollider)
    {
        if (bodyCollider != null && hitCollider == bodyCollider)
        {
            Debug.Log("Damage validated: Hit body collider");
            return true;
        }
        
        if (detectionCollider != null && hitCollider == detectionCollider)
        {
            Debug.Log("Damage rejected: Hit detection collider");
            return false;
        }
        
        Debug.LogWarning($"Unknown collider hit: {hitCollider.name}, defaulting to allow damage");
        return true;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"Enemy {gameObject.name} died! Triggering death sequence...");

        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.OnDeath();
        }
        else
        {
            Debug.Log("EnemyAI not found - using fallback death sequence");
            StartCoroutine(FallbackDeathSequence());
        }
    }
    
    private IEnumerator FallbackDeathSequence()
    {
        Transform animatorChild = transform.Find("Animator");
        if (animatorChild != null)
        {
            Animator childAnimator = animatorChild.GetComponent<Animator>();
            if (childAnimator != null)
            {
                try
                {
                    childAnimator.SetTrigger("Die");
                    Debug.Log("Death animation triggered on Animator child via fallback");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Die trigger not found in child animator: {e.Message}");
                }
            }
        }
        
        yield return new WaitForSeconds(2.5f);
        
        if (animatorChild != null)
        {
            animatorChild.gameObject.SetActive(false);
        }
        
        Destroy(gameObject);
        Debug.Log("Fallback death sequence completed");
    }

    void OnValidate()
    {
        if (maxHP <= 0)
            maxHP = 100;
    }
}