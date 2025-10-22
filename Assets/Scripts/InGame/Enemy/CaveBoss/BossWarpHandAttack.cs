using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the warp portal with hand animation that deals damage to player/NPC
/// Uses BoxCollider2D for the hand hitbox
/// The warp and hand are part of a single animation
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BossWarpHandAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int damage = 30;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Damage Timing")]
    [Tooltip("Delay before the hand collider becomes active (when hand extends from warp in animation)")]
    [SerializeField] private float damageActivationDelay = 0.5f;

    [Tooltip("How long the hand collider stays active and can deal damage")]
    [SerializeField] private float damageActiveDuration = 0.4f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject impactVFX;
    [SerializeField] private AudioClip impactSound;

    private BoxCollider2D handCollider;
    private bool hasHit = false;
    private bool isActive = false;
    private AudioSource audioSource;
    private Animator animator;

    void Awake()
    {
        handCollider = GetComponent<BoxCollider2D>();
        if (handCollider != null)
        {
            handCollider.isTrigger = true;
            handCollider.enabled = false; // Start disabled
        }

        animator = GetComponent<Animator>();

        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        StartCoroutine(WarpHandAttackSequence());
    }

    /// <summary>
    /// Initialize warp hand with custom values
    /// </summary>
    public void Initialize(int attackDamage, float attackLifetime)
    {
        damage = attackDamage;
        lifetime = attackLifetime;

        Debug.Log($"Warp hand initialized: Damage={damage}, Lifetime={lifetime}");
    }

    private IEnumerator WarpHandAttackSequence()
    {
        Debug.Log($"Warp hand attack started at {transform.position}");

        // Trigger animation (warp opens and hand extends in one animation)
        if (animator != null && HasAnimatorTrigger("Attack"))
        {
            animator.SetTrigger("Attack");
            Debug.Log("Warp hand animation triggered");
        }

        // Wait for the hand to extend (delay before damage becomes active)
        yield return new WaitForSeconds(damageActivationDelay);

        // Activate damage collider (hand is now extended and can hit)
        ActivateDamageCollider();

        // Keep collider active for the damage duration
        yield return new WaitForSeconds(damageActiveDuration);

        // Deactivate damage collider (hand retracts or disappears)
        DeactivateDamageCollider();

        // Wait for remaining lifetime
        float remainingLifetime = lifetime - damageActivationDelay - damageActiveDuration;
        if (remainingLifetime > 0)
        {
            yield return new WaitForSeconds(remainingLifetime);
        }

        // Destroy warp hand
        DestroyWarpHand();
    }

    private void ActivateDamageCollider()
    {
        isActive = true;
        if (handCollider != null)
        {
            handCollider.enabled = true;
        }

        Debug.Log($"Warp hand damage activated at {transform.position} - ready to deal {damage} damage");
    }

    private void DeactivateDamageCollider()
    {
        isActive = false;
        if (handCollider != null)
        {
            handCollider.enabled = false;
        }

        Debug.Log("Warp hand damage deactivated");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || hasHit) return;

        Debug.Log($"Warp hand detected collision with: {other.name} (tag: {other.tag})");

        // Check if it hit player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive())
            {
                playerHealth.TakeDamage(damage);
                hasHit = true;

                Debug.Log($"Warp hand dealt {damage} damage to player!");

                PlayImpactEffects(other.transform.position);

                if (destroyOnHit)
                {
                    DestroyWarpHand();
                }
            }
        }
        // Check if it hit NPC Arin
        else if (other.CompareTag("NPC") || other.name.Contains("Arin"))
        {
            // Arin doesn't have health, but we can still show effects
            hasHit = true;

            Debug.Log($"Warp hand hit NPC Arin!");

            PlayImpactEffects(other.transform.position);

            if (destroyOnHit)
            {
                DestroyWarpHand();
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

    private void DestroyWarpHand()
    {
        Debug.Log($"Warp hand at {transform.position} destroyed");

        // Play destruction effects if not already played
        if (!hasHit)
        {
            PlayImpactEffects(transform.position);
        }

        Destroy(gameObject, 0.1f); // Small delay for audio to play
    }

    private bool HasAnimatorTrigger(string triggerName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
                return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        // Draw warp hand box in editor
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = isActive ? Color.red : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }
    }
}