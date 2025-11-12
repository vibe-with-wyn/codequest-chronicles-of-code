using UnityEngine;

/// <summary>
/// Lightning Barrier - Kills player on contact if barrier is still active
/// Attached to the Lightning Barrier GameObject
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LightningBarrierDeath : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"LightningBarrierDeath on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (debugMode)
                Debug.Log("[Lightning Barrier] Player touched barrier - triggering death!");

            // Get PlayerHealth component and kill player
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Deal massive damage to kill player instantly
                playerHealth.TakeDamage(9999);
                
                if (debugMode)
                    Debug.Log("[Lightning Barrier] ✓ Player killed - will respawn at nearest checkpoint");
            }
            else
            {
                Debug.LogError("[Lightning Barrier] PlayerHealth component not found on player!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.red;

            if (col is BoxCollider2D boxCol)
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            else if (col is CircleCollider2D circleCol)
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
            $"⚡ LIGHTNING BARRIER ⚡\nDEATH ZONE\n\nKills player on contact\nPlayer respawns at nearest checkpoint",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red } });
#endif
    }
}