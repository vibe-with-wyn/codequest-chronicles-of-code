using UnityEngine;

/// <summary>
/// Attach to boss enemies to trigger quest objective completion on death
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class BossDeathQuestTrigger : MonoBehaviour
{
    [Header("Quest Objective Settings")]
    [Tooltip("The objective title to complete when this boss dies")]
    [SerializeField] private string objectiveTitle = "Defeat the Bringer of Death";
    
    [Tooltip("Optional: Specific quest ID. Leave empty to use current active quest.")]
    [SerializeField] private string questId = "";
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private EnemyHealth enemyHealth;
    private bool objectiveCompleted = false;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        
        if (enemyHealth == null)
        {
            Debug.LogError($"BossDeathQuestTrigger on {gameObject.name}: EnemyHealth component not found!");
            enabled = false;
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"BossDeathQuestTrigger initialized on {gameObject.name} for objective '{objectiveTitle}'");
        }
    }

    void Update()
    {
        // Check if boss is dead and objective hasn't been completed yet
        if (!objectiveCompleted && enemyHealth != null && !enemyHealth.IsAlive())
        {
            CompleteObjective();
        }
    }

    private void CompleteObjective()
    {
        if (objectiveCompleted) return;
        
        objectiveCompleted = true;
        
        // Verify QuestManager exists
        if (QuestManager.Instance == null)
        {
            Debug.LogError($"BossDeathQuestTrigger on {gameObject.name}: QuestManager not found!");
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"BossDeathQuestTrigger: Boss {gameObject.name} died, completing objective '{objectiveTitle}'");
        }
        
        // Complete the objective
        if (string.IsNullOrEmpty(questId))
        {
            QuestManager.Instance.CompleteObjectiveByTitle(objectiveTitle);
        }
        else
        {
            QuestManager.Instance.CompleteObjective(questId, objectiveTitle);
        }
    }

    // Public method to manually trigger (useful for testing)
    public void ManualTriggerObjective()
    {
        if (debugMode)
        {
            Debug.Log($"BossDeathQuestTrigger: Manually completing objective '{objectiveTitle}'");
        }
        
        CompleteObjective();
    }
}