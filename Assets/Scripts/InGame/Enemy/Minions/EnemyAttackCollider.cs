using UnityEngine;

public class EnemyAttackCollider : MonoBehaviour
{
    private int damage;
    private bool hasHit = false;

    void OnEnable()
    {
        // Reset hit flag when collider is enabled for new attack
        hasHit = false;
        Debug.Log($"Enemy attack collider enabled with damage: {damage}");
    }

    public void SetDamage(int damageValue)
    {
        damage = damageValue;
        hasHit = false; // Reset hit flag when damage is set
        Debug.Log($"Enemy attack collider damage set to: {damage}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return; // Prevent multiple hits from same attack
        
        Debug.Log($"Enemy attack collider detected: {other.name} with tag: {other.tag} on GameObject: {other.gameObject.name}");
        
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = FindPlayerHealthComponent(other);
            
            if (playerHealth != null)
            {
                // CRITICAL CHECK: Only attack if player is alive AND not dead
                if (playerHealth.IsAlive())
                {
                    playerHealth.TakeDamage(damage);
                    hasHit = true; // Mark as hit to prevent multiple damage
                    Debug.Log($"Enemy dealt {damage} damage to player via {other.name}");
                    
                    // Add visual feedback (optional)
                    ShowDamageEffect(other.transform.position);
                }
                else
                {
                    Debug.Log($"Attack blocked - Player is dead or not alive (playerHealth.IsAlive() = false)");
                }
            }
            else
            {
                Debug.LogError($"PlayerHealth component not found on Player tagged object '{other.name}' or its hierarchy! " +
                              $"GameObject path: {GetGameObjectPath(other.gameObject)}");
                
                // Debug: List the GameObject hierarchy and components
                DebugGameObjectHierarchy(other.gameObject);
            }
        }
    }

    // Comprehensive method to find PlayerHealth component
    private PlayerHealth FindPlayerHealthComponent(Collider2D collider)
    {
        GameObject currentObject = collider.gameObject;
        PlayerHealth playerHealth = null;
        
        // 1. Check the collider's GameObject
        playerHealth = currentObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            Debug.Log($"Found PlayerHealth on collider object: {currentObject.name}");
            return playerHealth;
        }
        
        // 2. Check parent objects (go up the hierarchy)
        Transform parent = currentObject.transform.parent;
        while (parent != null)
        {
            playerHealth = parent.GetComponent<PlayerHealth>();
            if (playerHealth != null) 
            {
                Debug.Log($"Found PlayerHealth on parent object: {parent.name}");
                return playerHealth;
            }
            parent = parent.parent;
        }
        
        // 3. Check root object
        Transform root = currentObject.transform.root;
        if (root != currentObject.transform)
        {
            playerHealth = root.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"Found PlayerHealth on root object: {root.name}");
                return playerHealth;
            }
        }
        
        // 4. Last resort: Find any PlayerHealth in the scene that has "Player" tag
        PlayerHealth[] allPlayerHealths = Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (PlayerHealth ph in allPlayerHealths)
        {
            if (ph.gameObject.CompareTag("Player"))
            {
                Debug.Log($"Found PlayerHealth via scene search on: {ph.gameObject.name}");
                return ph;
            }
        }
        
        return null; // Not found anywhere
    }

    // Helper method to get full GameObject path
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }

    // Debug method to show GameObject hierarchy and components
    private void DebugGameObjectHierarchy(GameObject obj)
    {
        Debug.Log($"=== DEBUGGING PLAYER OBJECT HIERARCHY ===");
        
        // Show current object
        Debug.Log($"Current Object: {obj.name}");
        Component[] components = obj.GetComponents<Component>();
        Debug.Log($"Components on {obj.name}:");
        foreach (Component comp in components)
        {
            Debug.Log($"  - {comp.GetType().Name}");
        }
        
        // Show parent hierarchy
        Transform parent = obj.transform.parent;
        int level = 1;
        while (parent != null && level <= 3) // Limit to 3 levels to avoid spam
        {
            Debug.Log($"Parent Level {level}: {parent.name}");
            Component[] parentComponents = parent.GetComponents<Component>();
            Debug.Log($"Components on {parent.name}:");
            foreach (Component comp in parentComponents)
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }
            parent = parent.parent;
            level++;
        }
        
        // Show children (immediate only)
        Debug.Log($"Children of {obj.name}:");
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            Transform child = obj.transform.GetChild(i);
            Debug.Log($"  Child: {child.name}");
            Component[] childComponents = child.GetComponents<Component>();
            foreach (Component comp in childComponents)
            {
                Debug.Log($"    - {comp.GetType().Name}");
            }
        }
        
        Debug.Log($"=== END HIERARCHY DEBUG ===");
    }

    void OnDisable()
    {
        Debug.Log("Enemy attack collider disabled");
    }

    private void ShowDamageEffect(Vector3 position)
    {
        // You can add particle effects or damage numbers here
        Debug.Log($"Enemy damage effect at position: {position}");
    }
}