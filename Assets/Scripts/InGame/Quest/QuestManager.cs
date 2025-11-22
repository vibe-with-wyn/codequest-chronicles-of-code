using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    [Header("Quest Configuration")]
    [SerializeField] private QuestDatabase questDatabase;
    [SerializeField] private QuestData[] allQuests;
    
    [Header("Development Settings")]
    [SerializeField] private bool resetQuestsInEditor = true;
    
    private QuestData currentQuest;
    private int currentQuestIndex = 0;
    
    public System.Action<QuestData> OnQuestUpdated;
    public System.Action<QuestData> OnNewQuestStarted;
    public System.Action<QuestData> OnQuestCompleted;
    public System.Action<QuestObjective, QuestData> OnObjectiveCompleted;
    
    public static QuestManager Instance { get; private set; }
    
    private const string FinalQuestId = "Q5_SyntaxTrial";
    
    void Awake()
    {
        #if UNITY_EDITOR
        if (resetQuestsInEditor)
        {
            if (Instance != null && Instance != this)
            {
                Debug.Log("QuestManager: Destroying previous instance for editor reset");
                Destroy(Instance.gameObject);
                Instance = null;
            }
        }
        #endif
        
        if (Instance == null)
        {
            Instance = this;
            
            #if UNITY_EDITOR
            if (!resetQuestsInEditor)
            {
                DontDestroyOnLoad(gameObject);
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
        EnsureQuestDatabase();

        #if UNITY_EDITOR
        if (resetQuestsInEditor && questDatabase != null)
        {
            Debug.Log("QuestManager: Resetting all quest data for editor session");
            
            var databaseQuests = questDatabase.GetAllQuests();
            if (databaseQuests.Count > 0)
            {
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
        
        if (questDatabase != null)
        {
            var databaseQuests = questDatabase.GetAllQuests();
            if (databaseQuests.Count > 0)
            {
                allQuests = databaseQuests.ToArray();
                Debug.Log($"Loaded {allQuests.Length} quests from QuestDatabase");
                
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

    private void EnsureQuestDatabase()
    {
        if (questDatabase == null)
        {
            questDatabase = Resources.Load<QuestDatabase>("OakWoods/QuestDatabase_OakWoods");

            if (questDatabase == null)
            {
                Debug.LogError("QuestDatabase not found in Resources/OakWoods/. Run Tools > OakWoods > Generate Oak Woods Content to create it.");
                CreateEmptyQuestArray();
            }
            else
            {
                Debug.Log("QuestDatabase auto-loaded from Resources/OakWoods/QuestDatabase_OakWoods");
            }
        }
    }
    
    private QuestData CreateFreshQuestCopy(QuestData original)
    {
        QuestData copy = new QuestData(original.questId, original.questTitle, original.questDescription);
        copy.isActive = false;
        copy.isCompleted = false;
        copy.questImage = original.questImage;
        copy.nextQuestId = original.nextQuestId;
        
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
            currentQuest.StartQuest();
            currentQuestIndex = 0;
            
            Debug.Log($"First quest started: {currentQuest.questTitle}");
            Debug.Log($"Quest Description: {currentQuest.questDescription}");
            Debug.Log($"Number of objectives: {currentQuest.objectives.Count}");
            Debug.Log($"First objective active: {currentQuest.GetCurrentObjective()?.objectiveTitle}");
            
            OnNewQuestStarted?.Invoke(currentQuest);
        }
        else
        {
            Debug.LogError("No quests available to start! Please check your QuestDatabase.");
        }
    }
    
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
                    else
                    {
                        OnQuestUpdated?.Invoke(quest);
                    }
                }
                else
                {
                    OnQuestUpdated?.Invoke(quest);
                }
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
        if (currentQuest == null) return;

        // CRITICAL FIX: Store reference to current quest BEFORE modifications
        QuestData questToDisplay = currentQuest;
        string questIdToCheck = currentQuest.questId;
        int questIndexToCheck = currentQuestIndex;

        currentQuest.CompleteQuest();
        Debug.Log($"Quest completed: {currentQuest.questTitle}");
        OnQuestCompleted?.Invoke(currentQuest);

        // CRITICAL FIX: Use stored reference and explicit checks instead of relying on modified state
        bool isFinalQuest =
            questIdToCheck == FinalQuestId &&
            string.IsNullOrEmpty(questToDisplay.nextQuestId) &&
            questIndexToCheck == allQuests.Length - 1;

        if (isFinalQuest)
        {
            // Show final quest completion once and DO NOT start a next quest (prevents overwriting with Quest 2)
            if (QuestUIController.Instance != null)
            {
                Debug.Log("[QuestManager] Final quest completed – displaying final completion UI for Quest 5.");
                Debug.Log($"[QuestManager] Displaying quest: {questToDisplay.questTitle} (ID: {questToDisplay.questId})");
                QuestUIController.Instance.ShowQuestDisplay(questToDisplay, autoHide: true);
            }
            else
            {
                Debug.LogWarning("[QuestManager] QuestUIController.Instance not found - cannot display final quest completion UI.");
            }

            // End the quest chain cleanly
            currentQuest = null;
            currentQuestIndex = allQuests.Length; // mark as finished
            Debug.Log("All quests completed!");
            return;
        }

        // Normal flow: proceed to next quest
        StartNextQuest();
    }
    
    private void StartNextQuest()
    {
        Debug.Log("StartNextQuest called");
        Debug.Log($"Current quest nextQuestId: {currentQuest?.nextQuestId}");
        
        if (!string.IsNullOrEmpty(currentQuest?.nextQuestId))
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
