using UnityEngine;

/// <summary>
/// Place this on GameObjects with trigger colliders to automatically complete quest objectives
/// when the player enters the trigger zone.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class QuestObjectiveTrigger : MonoBehaviour
{
    [Header("Quest Objective Settings")]
    [Tooltip("The exact title of the objective to complete (must match the objective title in QuestData)")]
    [SerializeField] private string objectiveTitle;
    
    [Tooltip("Optional: Specific quest ID. Leave empty to use current active quest.")]
    [SerializeField] private string questId = "";
    
    [Header("Trigger Behavior")]
    [Tooltip("If true, this trigger can only fire once and will disable itself after triggering")]
    [SerializeField] private bool oneTimeUse = true;
    
    [Tooltip("If true, show debug messages when this trigger activates")]
    [SerializeField] private bool debugMode = true;
    
    [Header("Visual Feedback (Optional)")]
    [Tooltip("GameObject to activate when objective is completed (e.g., particle effect)")]
    [SerializeField] private GameObject completionVFX;
    
    [Tooltip("Audio clip to play when objective is completed")]
    [SerializeField] private AudioClip completionSound;
    
    private bool hasTriggered = false;
    private AudioSource audioSource;

    void Awake()
    {
        // Ensure collider is set to trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"QuestObjectiveTrigger on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }
        
        // Setup audio source if completion sound is assigned
        if (completionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = completionSound;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if already triggered (for one-time use triggers)
        if (oneTimeUse && hasTriggered)
        {
            return;
        }
        
        // Check if it's the player
        if (!other.CompareTag("Player"))
        {
            return;
        }
        
        // Verify QuestManager exists
        if (QuestManager.Instance == null)
        {
            Debug.LogError($"QuestObjectiveTrigger on {gameObject.name}: QuestManager not found in scene!");
            return;
        }
        
        // Validate objective title
        if (string.IsNullOrEmpty(objectiveTitle))
        {
            Debug.LogError($"QuestObjectiveTrigger on {gameObject.name}: Objective title is not set!");
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"QuestObjectiveTrigger: Player entered trigger zone for objective '{objectiveTitle}'");
        }
        
        // Complete the objective
        CompleteObjective();
    }

    private void CompleteObjective()
    {
        // Mark as triggered
        hasTriggered = true;
        
        // Complete the objective through QuestManager
        if (string.IsNullOrEmpty(questId))
        {
            // Use current active quest
            QuestManager.Instance.CompleteObjectiveByTitle(objectiveTitle);
            
            if (debugMode)
            {
                Debug.Log($"QuestObjectiveTrigger: Completed objective '{objectiveTitle}' in current quest");
            }
        }
        else
        {
            // Use specific quest ID
            QuestManager.Instance.CompleteObjective(questId, objectiveTitle);
            
            if (debugMode)
            {
                Debug.Log($"QuestObjectiveTrigger: Completed objective '{objectiveTitle}' in quest '{questId}'");
            }
        }
        
        // Play visual/audio feedback
        PlayCompletionFeedback();
        
        // Disable this trigger if one-time use
        if (oneTimeUse)
        {
            GetComponent<Collider2D>().enabled = false;
            
            if (debugMode)
            {
                Debug.Log($"QuestObjectiveTrigger: Disabled trigger for '{objectiveTitle}' (one-time use)");
            }
        }
    }

    private void PlayCompletionFeedback()
    {
        // Spawn VFX if assigned
        if (completionVFX != null)
        {
            GameObject vfxInstance = Instantiate(completionVFX, transform.position, Quaternion.identity);
            Destroy(vfxInstance, 3f); // Auto-destroy after 3 seconds
            
            if (debugMode)
            {
                Debug.Log($"QuestObjectiveTrigger: Spawned completion VFX at {transform.position}");
            }
        }
        
        // Play sound if assigned
        if (audioSource != null && completionSound != null)
        {
            audioSource.Play();
            
            if (debugMode)
            {
                Debug.Log($"QuestObjectiveTrigger: Playing completion sound");
            }
        }
    }

    // Public method to manually trigger (can be called from other scripts)
    public void ManualTrigger()
    {
        if (debugMode)
        {
            Debug.Log($"QuestObjectiveTrigger: Manually triggered for objective '{objectiveTitle}'");
        }
        
        CompleteObjective();
    }

    // Reset trigger (useful for testing)
    public void ResetTrigger()
    {
        hasTriggered = false;
        GetComponent<Collider2D>().enabled = true;
        
        if (debugMode)
        {
            Debug.Log($"QuestObjectiveTrigger: Reset trigger for objective '{objectiveTitle}'");
        }
    }

    void OnDrawGizmos()
    {
        // Draw trigger zone in editor
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = hasTriggered ? Color.gray : Color.cyan;
            
            if (col is BoxCollider2D boxCol)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            }
            else if (col is CircleCollider2D circleCol)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
            }
        }
        
        // Draw label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
            $"Quest Trigger\n{objectiveTitle}", 
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan } });
        #endif
    }

    void OnValidate()
    {
        // Auto-setup collider as trigger when component is added
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }
}