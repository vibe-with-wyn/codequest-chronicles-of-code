using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    [Header("Quest Configuration")]
    [SerializeField] private QuestDatabase questDatabase;
    [SerializeField] private QuestData[] allQuests;
    
    // NEW: Development setting
    [Header("Development Settings")]
    [Tooltip("If true, quest progress will reset every time you enter Play Mode in the Editor")]
    [SerializeField] private bool resetQuestsInEditor = true;
    
    // Current active quest
    private QuestData currentQuest;
    private int currentQuestIndex = 0;
    
    // Events for UI updates
    public System.Action<QuestData> OnQuestUpdated;
    public System.Action<QuestData> OnNewQuestStarted;
    public System.Action<QuestData> OnQuestCompleted;
    public System.Action<QuestObjective, QuestData> OnObjectiveCompleted;
    
    // Singleton pattern for easy access
    public static QuestManager Instance { get; private set; }
    
    void Awake()
    {
        // NEW: Handle editor reset
        #if UNITY_EDITOR
        if (resetQuestsInEditor)
        {
            // Destroy any existing QuestManager from previous play sessions
            if (Instance != null && Instance != this)
            {
                Debug.Log("QuestManager: Destroying previous instance for editor reset");
                Destroy(Instance.gameObject);
                Instance = null;
            }
        }
        #endif
        
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            
            // NEW: Only use DontDestroyOnLoad in builds, not in editor during development
            #if UNITY_EDITOR
            if (!resetQuestsInEditor)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.Log("QuestManager: Editor reset mode enabled - progress will NOT persist between play sessions");
            }
            #else
            DontDestroyOnLoad(gameObject);
            #endif
            
            InitializeQuests();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeQuests()
    {
        // NEW: Force fresh quest data in editor
        #if UNITY_EDITOR
        if (resetQuestsInEditor && questDatabase != null)
        {
            Debug.Log("QuestManager: Resetting all quest data for editor session");
            
            // Get fresh copy of quests from database
            var databaseQuests = questDatabase.GetAllQuests();
            if (databaseQuests.Count > 0)
            {
                // Create NEW instances to avoid modifying the ScriptableObject
                allQuests = new QuestData[databaseQuests.Count];
                for (int i = 0; i < databaseQuests.Count; i++)
                {
                    allQuests[i] = CreateFreshQuestCopy(databaseQuests[i]);
                }
                
                Debug.Log($"Loaded {allQuests.Length} fresh quests from QuestDatabase (Editor Mode)");
                return;
            }
        }
        #endif
        
        // Normal initialization (for builds or when reset is disabled)
        if (questDatabase != null)
        {
            var databaseQuests = questDatabase.GetAllQuests();
            if (databaseQuests.Count > 0)
            {
                allQuests = databaseQuests.ToArray();
                Debug.Log($"Loaded {allQuests.Length} quests from QuestDatabase");
                
                // Debug: Print all loaded quests
                for (int i = 0; i < allQuests.Length; i++)
                {
                    var quest = allQuests[i];
                    Debug.Log($"Quest {i + 1}: {quest.questTitle}");
                    Debug.Log($"  Description: {quest.questDescription}");
                    Debug.Log($"  Objectives: {quest.objectives.Count}");
                    
                    for (int j = 0; j < quest.objectives.Count; j++)
                    {
                        Debug.Log($"    Objective {j + 1}: {quest.objectives[j].objectiveTitle}");
                    }
                }
                
                return;
            }
            else
            {
                Debug.LogWarning("QuestDatabase is assigned but contains no quests!");
            }
        }
        else
        {
            Debug.LogWarning("No QuestDatabase assigned to QuestManager!");
        }
        
        CreateEmptyQuestArray();
        Debug.Log($"QuestManager initialized with {allQuests.Length} quests");
    }
    
    // NEW: Create a fresh copy of quest data (not referencing the ScriptableObject)
    private QuestData CreateFreshQuestCopy(QuestData original)
    {
        QuestData copy = new QuestData(original.questId, original.questTitle, original.questDescription);
        copy.isActive = false;
        copy.isCompleted = false;
        copy.questImage = original.questImage;
        copy.nextQuestId = original.nextQuestId;
        
        // Copy objectives
        foreach (var originalObjective in original.objectives)
        {
            QuestObjective objCopy = new QuestObjective(
                originalObjective.objectiveTitle,
                originalObjective.objectiveDescription,
                originalObjective.isOptional,
                originalObjective.targetCount
            );
            objCopy.isActive = false;
            objCopy.isCompleted = false;
            objCopy.currentCount = 0;
            
            copy.objectives.Add(objCopy);
        }
        
        return copy;
    }
    
    // NEW: Create empty array instead of default quests
    private void CreateEmptyQuestArray()
    {
        allQuests = new QuestData[0];
        Debug.LogWarning("No quests available! Please assign a QuestDatabase with quests.");
    }
    
    public void StartFirstQuest()
    {
        if (allQuests.Length > 0)
        {
            currentQuest = allQuests[0];
            currentQuest.StartQuest(); // This will activate first objective
            currentQuestIndex = 0;
            
            Debug.Log($"First quest started: {currentQuest.questTitle}");
            Debug.Log($"Quest Description: {currentQuest.questDescription}");
            Debug.Log($"Number of objectives: {currentQuest.objectives.Count}");
            Debug.Log($"First objective active: {currentQuest.GetCurrentObjective()?.objectiveTitle}");
            
            // Notify listeners that a new quest started
            OnNewQuestStarted?.Invoke(currentQuest);
        }
        else
        {
            Debug.LogError("No quests available to start! Please check your QuestDatabase.");
        }
    }
    
    // Complete specific objectives
    public void CompleteObjective(string questId, string objectiveTitle)
    {
        var quest = allQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            var objective = quest.objectives.Find(o => o.objectiveTitle == objectiveTitle);
            if (objective != null && !objective.isCompleted)
            {
                objective.CompleteObjective();
                Debug.Log($"Completed objective: {objectiveTitle}");
                
                OnObjectiveCompleted?.Invoke(objective, quest);
                
                // Progress to next objective
                quest.ProgressToNextObjective();
                
                if (quest.isCompleted)
                {
                    CompleteCurrentQuest();
                }
                else
                {
                    OnQuestUpdated?.Invoke(quest);
                }
            }
        }
    }
    
    // Update objective progress (for count-based objectives)
    public void UpdateObjectiveProgress(string questId, string objectiveTitle, int amount = 1)
    {
        var quest = allQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            var objective = quest.objectives.Find(o => o.objectiveTitle == objectiveTitle);
            if (objective != null && !objective.isCompleted)
            {
                objective.UpdateProgress(amount);
                Debug.Log($"Updated objective: {objectiveTitle} ({objective.currentCount}/{objective.targetCount})");
                
                if (objective.isCompleted)
                {
                    OnObjectiveCompleted?.Invoke(objective, quest);
                    quest.ProgressToNextObjective();
                    
                    if (quest.isCompleted)
                    {
                        CompleteCurrentQuest();
                    }
                }
                
                OnQuestUpdated?.Invoke(quest);
            }
        }
    }
    
    public QuestData GetCurrentQuest()
    {
        return currentQuest;
    }
    
    public bool HasActiveQuest()
    {
        return currentQuest != null && currentQuest.isActive && !currentQuest.isCompleted;
    }
    
    public void CompleteCurrentQuest()
    {
        if (currentQuest != null)
        {
            currentQuest.CompleteQuest();
            
            Debug.Log($"Quest completed: {currentQuest.questTitle}");
            OnQuestCompleted?.Invoke(currentQuest);
            
            // Start next quest if available
            StartNextQuest();
        }
    }
    
    private void StartNextQuest()
    {
        Debug.Log("StartNextQuest called");
        Debug.Log($"Current quest nextQuestId: {currentQuest?.nextQuestId}");
        
        // Find next quest by ID
        if (!string.IsNullOrEmpty(currentQuest.nextQuestId))
        {
            var nextQuest = allQuests.FirstOrDefault(q => q.questId == currentQuest.nextQuestId);
            if (nextQuest != null)
            {
                currentQuest = nextQuest;
                currentQuest.StartQuest();
                
                Debug.Log($"Next quest started: {currentQuest.questTitle}");
                Debug.Log($"Next quest description: {currentQuest.questDescription}");
                OnNewQuestStarted?.Invoke(currentQuest);
                return;
            }
            else
            {
                Debug.LogError($"Could not find next quest with ID: {currentQuest.nextQuestId}");
            }
        }
        
        // Fallback: try next quest in array
        currentQuestIndex++;
        if (currentQuestIndex < allQuests.Length)
        {
            currentQuest = allQuests[currentQuestIndex];
            currentQuest.StartQuest();
            
            Debug.Log($"Next quest started (fallback): {currentQuest.questTitle}");
            Debug.Log($"Next quest description (fallback): {currentQuest.questDescription}");
            OnNewQuestStarted?.Invoke(currentQuest);
        }
        else
        {
            currentQuest = null;
            Debug.Log("All quests completed!");
        }
    }
    
    // NEW: Method to manually check current quest info
    public void DebugCurrentQuestInfo()
    {
        if (currentQuest != null)
        {
            Debug.Log("=== CURRENT QUEST DEBUG INFO ===");
            Debug.Log($"Quest ID: {currentQuest.questId}");
            Debug.Log($"Quest Title: {currentQuest.questTitle}");
            Debug.Log($"Quest Description: {currentQuest.questDescription}");
            Debug.Log($"Quest Active: {currentQuest.isActive}");
            Debug.Log($"Quest Completed: {currentQuest.isCompleted}");
            Debug.Log($"Next Quest ID: {currentQuest.nextQuestId}");
            Debug.Log($"Objectives Count: {currentQuest.objectives.Count}");
            
            for (int i = 0; i < currentQuest.objectives.Count; i++)
            {
                var obj = currentQuest.objectives[i];
                Debug.Log($"  Objective {i+1}: {obj.objectiveTitle} - Active: {obj.isActive}, Completed: {obj.isCompleted}");
            }
            Debug.Log("================================");
        }
        else
        {
            Debug.Log("No current quest active");
        }
    }
    
    // Dynamic convenience methods - these will work with any quest from the database
    public void CompleteObjectiveByTitle(string objectiveTitle)
    {
        if (currentQuest != null)
        {
            CompleteObjective(currentQuest.questId, objectiveTitle);
        }
        else
        {
            Debug.LogWarning("No active quest to complete objective for!");
        }
    }
    
    public void UpdateCurrentQuestObjectiveProgress(string objectiveTitle, int amount = 1)
    {
        if (currentQuest != null)
        {
            UpdateObjectiveProgress(currentQuest.questId, objectiveTitle, amount);
        }
        else
        {
            Debug.LogWarning("No active quest to update objective progress for!");
        }
    }
}
