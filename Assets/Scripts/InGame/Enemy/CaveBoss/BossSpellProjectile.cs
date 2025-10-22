using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the spawned spell object that deals damage to player/NPC
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class BossSpellProjectile : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private int damage = 30;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private bool destroyOnHit = true;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject impactVFX;
    [SerializeField] private AudioClip impactSound;
    
    [Header("Warning Phase")]
    [SerializeField] private float warningDuration = 0.5f; // Time before spell becomes active
    [SerializeField] private SpriteRenderer warningIndicator; // Visual warning (circle on ground)
    
    private CircleCollider2D spellCollider;
    private bool hasHit = false;
    private bool isActive = false;
    private AudioSource audioSource;

    void Awake()
    {
        spellCollider = GetComponent<CircleCollider2D>();
        if (spellCollider != null)
        {
            spellCollider.isTrigger = true;
        }
        
        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        StartCoroutine(SpellLifecycle());
    }

    /// <summary>
    /// Initialize spell with custom values
    /// </summary>
    public void Initialize(int spellDamage, float spellLifetime)
    {
        damage = spellDamage;
        lifetime = spellLifetime;
    }

    private IEnumerator SpellLifecycle()
    {
        // Phase 1: Warning (spell is visible but doesn't deal damage yet)
        if (warningDuration > 0)
        {
            if (warningIndicator != null)
            {
                warningIndicator.enabled = true;
                // Pulse warning indicator
                StartCoroutine(PulseWarning());
            }
            
            // Disable collider during warning
            if (spellCollider != null)
            {
                spellCollider.enabled = false;
            }
            
            yield return new WaitForSeconds(warningDuration);
            
            if (warningIndicator != null)
            {
                warningIndicator.enabled = false;
            }
        }
        
        // Phase 2: Active (spell can now deal damage)
        isActive = true;
        if (spellCollider != null)
        {
            spellCollider.enabled = true;
        }
        
        Debug.Log($"Boss spell activated at {transform.position} - ready to deal {damage} damage");
        
        // Phase 3: Wait for lifetime
        yield return new WaitForSeconds(lifetime - warningDuration);
        
        // Phase 4: Destroy
        DestroySpell();
    }

    private IEnumerator PulseWarning()
    {
        if (warningIndicator == null) yield break;
        
        float elapsed = 0f;
        Color originalColor = warningIndicator.color;
        
        while (elapsed < warningDuration)
        {
            float pulse = Mathf.PingPong(elapsed * 4f, 1f);
            Color newColor = originalColor;
            newColor.a = 0.3f + (pulse * 0.7f);
            warningIndicator.color = newColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || hasHit) return;
        
        Debug.Log($"Boss spell detected collision with: {other.name} (tag: {other.tag})");
        
        // Check if it hit player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive())
            {
                playerHealth.TakeDamage(damage);
                hasHit = true;
                
                Debug.Log($"Boss spell dealt {damage} damage to player!");
                
                PlayImpactEffects(other.transform.position);
                
                if (destroyOnHit)
                {
                    DestroySpell();
                }
            }
        }
        // Check if it hit NPC Arin
        else if (other.CompareTag("NPC") || other.name.Contains("Arin"))
        {
            // Arin doesn't have health, but we can still show effects
            hasHit = true;
            
            Debug.Log($"Boss spell hit NPC Arin!");
            
            PlayImpactEffects(other.transform.position);
            
            if (destroyOnHit)
            {
                DestroySpell();
            }
        }
    }

    private void PlayImpactEffects(Vector3 position)
    {
        // Spawn VFX
        if (impactVFX != null)
        {
            GameObject vfx = Instantiate(impactVFX, position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
        
        // Play sound
        if (impactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSound);
        }
    }

    private void DestroySpell()
    {
        Debug.Log($"Boss spell at {transform.position} destroyed");
        
        // Play destruction effects if not already played
        if (!hasHit)
        {
            PlayImpactEffects(transform.position);
        }
        
        Destroy(gameObject, 0.1f); // Small delay for audio to play
    }

    void OnDrawGizmos()
    {
        // Draw spell radius in editor
        Gizmos.color = isActive ? Color.red : Color.yellow;
        if (spellCollider != null)
        {
            Gizmos.DrawWireSphere(transform.position, spellCollider.radius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}