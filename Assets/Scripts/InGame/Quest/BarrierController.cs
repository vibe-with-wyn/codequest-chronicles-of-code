using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// QUEST 5 FINAL EVALUATION: Barrier Controller
/// Requires the Eternal Key from Quest 4 to proceed
/// Player must complete 10-question comprehensive quiz covering all topics
/// After quiz success, key insertion animation plays and barrier is destroyed
/// If player bypasses without key, they die and respawn
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BarrierController : MonoBehaviour
{
    [Header("Quest Settings")]
    [SerializeField] private string requiredQuestId = "Q5_SyntaxTrial";
    [SerializeField] private string requiredObjectiveTitle = "Complete the Syntax Trial";

    [Header("Key Requirement")]
    [Tooltip("Quest 4 must be completed to have the key")]
    [SerializeField] private string keyQuestId = "Q4_StringsPrinting";
    [Tooltip("Quest 4 objective that proves player has the key")]
    [SerializeField] private string keyObjectiveTitle = "Obtain the Path Barrier Key";

    [Header("Final Evaluation Quiz")]
    [Tooltip("10-question comprehensive quiz covering all topics")]
    [SerializeField] private MiniQuestData finalEvaluationQuiz;

    [Header("Interaction Button Prefab")]
    [Tooltip("Prefab with world-space Canvas containing the interaction button")]
    [SerializeField] private GameObject interactionButtonPrefab;

    [Header("Button Position")]
    [SerializeField] private Vector3 buttonOffset = new(0, 3f, 0);

    [Header("Button Settings")]
    [SerializeField] private float buttonFadeSpeed = 5f;

    [Header("Key Insertion")]
    [Tooltip("Key prefab to spawn and animate during insertion")]
    [SerializeField] private GameObject keyPrefab;

    [Tooltip("Position where key starts insertion animation")]
    [SerializeField] private Vector3 keyInsertionStartOffset = new(-1f, 0f, 0);

    [Tooltip("Position where key ends (inside keyhole)")]
    [SerializeField] private Vector3 keyInsertionEndOffset = new(0f, 0f, 0);

    [Tooltip("Duration of key insertion animation")]
    [SerializeField] private float keyInsertionDuration = 1.5f;

    [Tooltip("Rotation applied during key twist (degrees)")]
    [SerializeField] private float keyTwistRotation = 90f;

    [Tooltip("Duration of key twist animation")]
    [SerializeField] private float keyTwistDuration = 0.8f;

    [Header("Path Barrier (Parent)")]
    [Tooltip("Path Barrier parent GameObject - will be destroyed after key twist")]
    [SerializeField] private GameObject pathBarrier;

    [Header("Lightning Barrier")]
    [Tooltip("Lightning barrier GameObject to disable after key insertion")]
    [SerializeField] private GameObject lightningBarrier;

    [Header("Timing")]
    [Tooltip("Delay after quiz completion before key insertion")]
    [SerializeField] private float postQuizDelay = 0.5f;

    [Tooltip("Delay after key twist before barrier is destroyed")]
    [SerializeField] private float preBarrierDestructionDelay = 0.5f;

    [Tooltip("Delay after barrier destruction before restoring UI")]
    [SerializeField] private float uiRestoreDelay = 0.5f;

    [Header("Game UI Reference")]
    [SerializeField] private CanvasGroup gameUICanvasGroup;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // State
    private bool isPlayerInRange = false;
    private bool isQuizActive = false;
    private bool hasCompletedQuiz = false;
    private bool isProcessing = false;
    private Transform playerTransform;
    private MiniQuestUIController miniQuestUI;

    // Button
    private GameObject buttonInstance;
    private Button interactionButton;
    private TextMeshProUGUI buttonText;
    private CanvasGroup buttonCanvasGroup;

    // Key reference
    private GameObject spawnedKey;

    // Cached components to avoid GetComponent allocations
    private Collider2D cachedCollider;
    private Collider2D lightningBarrierCollider;
    private UIController cachedUIController;

    void Awake()
    {
        // Cache collider reference
        cachedCollider = GetComponent<Collider2D>();
        if (cachedCollider != null && !cachedCollider.isTrigger)
        {
            cachedCollider.isTrigger = true;
            Debug.LogWarning($"BarrierController on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }

        // Auto-find Path Barrier
        if (pathBarrier == null)
        {
            Transform barrier = transform.Find("Path Barrier");
            if (barrier != null)
            {
                pathBarrier = barrier.gameObject;
                if (debugMode)
                    Debug.Log($"[Barrier] Auto-found Path Barrier: {pathBarrier.name}");
            }
        }

        // Auto-find lightning barrier
        if (lightningBarrier == null)
        {
            Transform barrier = transform.Find("Lightning Barrier");
            if (barrier != null)
            {
                lightningBarrier = barrier.gameObject;
                if (debugMode)
                    Debug.Log($"[Barrier] Auto-found Lightning Barrier: {lightningBarrier.name}");
            }
        }

        // Auto-find game UI
        if (gameUICanvasGroup == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.name != "Quiz UI")
            {
                gameUICanvasGroup = canvas.TryGetComponent<CanvasGroup>(out var canvasGroup) ? canvasGroup : null;
                if (debugMode)
                    Debug.Log("[Barrier] Auto-found game UI CanvasGroup");
            }
        }
    }

    void Start()
    {
        CacheMiniQuestUI();
        ValidateQuizData();
        InitializeButton();
        ValidateBarrierSetup();
    }

    void Update()
    {
        UpdateButtonVisibility();
    }

    #region Initialization

    private void CacheMiniQuestUI()
    {
        if (miniQuestUI == null)
        {
            miniQuestUI = Object.FindFirstObjectByType<MiniQuestUIController>();
            if (miniQuestUI == null)
                Debug.LogError("[Barrier] MiniQuestUIController not found in scene!");
            else if (debugMode)
                Debug.Log("[Barrier] MiniQuestUIController found");
        }
    }

    private void ValidateQuizData()
    {
        if (finalEvaluationQuiz == null)
        {
            Debug.LogError("[Barrier] Final Evaluation Quiz Data not assigned!");
            return;
        }

        if (finalEvaluationQuiz.questions.Count == 0)
            Debug.LogError($"[Barrier] Quiz '{finalEvaluationQuiz.name}' has no questions!");

        if (debugMode)
        {
            Debug.Log($"[Barrier] Final Evaluation Quiz loaded: {finalEvaluationQuiz.miniQuestTitle}");
            Debug.Log($"[Barrier] Total questions: {finalEvaluationQuiz.GetTotalQuestions()}");
        }
    }

    private void InitializeButton()
    {
        if (interactionButtonPrefab == null)
        {
            Debug.LogError("[Barrier] Interaction Button Prefab not assigned!");
            return;
        }

        Vector3 buttonPosition = transform.position + buttonOffset;
        buttonInstance = Instantiate(interactionButtonPrefab, buttonPosition, Quaternion.identity);
        buttonInstance.transform.SetParent(transform, true);

        interactionButton = buttonInstance.GetComponentInChildren<Button>();
        buttonCanvasGroup = buttonInstance.TryGetComponent<CanvasGroup>(out var canvasGroup) 
            ? canvasGroup 
            : buttonInstance.AddComponent<CanvasGroup>();
        buttonText = buttonInstance.GetComponentInChildren<TextMeshProUGUI>();

        if (interactionButton == null)
        {
            Debug.LogError("[Barrier] Button component not found in prefab!");
            return;
        }

        // Button text changes based on key status
        UpdateButtonText();

        interactionButton.onClick.AddListener(OnButtonClicked);

        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;
        buttonInstance.SetActive(false);

        if (debugMode)
            Debug.Log($"[Barrier] Button instantiated at: {buttonPosition}");
    }

    private void ValidateBarrierSetup()
    {
        if (pathBarrier == null)
        {
            Debug.LogWarning("[Barrier] No Path Barrier assigned - barrier won't be destroyed");
        }
        else
        {
            // Count how many child Animators the Path Barrier has
            Animator[] childAnimators = pathBarrier.GetComponentsInChildren<Animator>();
            if (debugMode)
                Debug.Log($"[Barrier] Path Barrier has {childAnimators.Length} child Animator(s)");
        }

        if (lightningBarrier == null)
        {
            Debug.LogWarning("[Barrier] No Lightning Barrier assigned - barrier won't deactivate");
            return;
        }

        // Check if barrier has collider for player death (cached to avoid allocation)
        lightningBarrierCollider = lightningBarrier.TryGetComponent<Collider2D>(out var collider) ? collider : null;
        
        if (lightningBarrierCollider == null)
        {
            Debug.LogWarning("[Barrier] Lightning Barrier has no collider - player won't die on contact");
        }
        else if (!lightningBarrierCollider.isTrigger)
        {
            Debug.LogWarning("[Barrier] Lightning Barrier collider is not a trigger - player won't die on contact");
        }

        if (debugMode)
            Debug.Log("[Barrier] Barrier setup validated");
    }

    #endregion

    #region Key Validation

    /// <summary>
    /// Check if player has the Eternal Key from Quest 4
    /// FIXED: Only runs during Play Mode to avoid Edit Mode errors
    /// </summary>
    private bool PlayerHasKey()
    {
        // CRITICAL FIX: Don't run in Edit Mode (causes QuestManager not found warnings)
        if (!Application.isPlaying)
            return false;

        if (QuestManager.Instance == null)
        {
            if (debugMode)
                Debug.LogWarning("[Barrier] QuestManager not found - cannot validate key");
            return false;
        }

        // Find Quest 4 in all quests
        QuestData quest4 = FindQuestById(keyQuestId);

        if (quest4 == null)
        {
            if (debugMode)
                Debug.LogWarning($"[Barrier] Quest 4 ({keyQuestId}) not found!");
            return false;
        }

        // Check if the key collection objective was completed
        QuestObjective keyObjective = quest4.objectives.Find(o => o.objectiveTitle == keyObjectiveTitle);

        if (keyObjective == null)
        {
            if (debugMode)
                Debug.LogWarning($"[Barrier] Key objective '{keyObjectiveTitle}' not found in Quest 4!");
            return false;
        }

        bool playerHasEternalKey = keyObjective.isCompleted;

        if (debugMode)
        {
            Debug.Log($"[Barrier] Key validation: {(playerHasEternalKey ? "✓ PLAYER HAS KEY" : "✗ PLAYER DOES NOT HAVE KEY")}");
            Debug.Log($"[Barrier]   Quest 4 Status: {(quest4.isCompleted ? "Completed" : "In Progress")}");
            Debug.Log($"[Barrier]   Key Objective: {keyObjective.objectiveTitle} - {(keyObjective.isCompleted ? "COMPLETED" : "NOT COMPLETED")}");
        }

        return playerHasEternalKey;
    }

    /// <summary>
    /// Helper method to find a quest by ID from QuestManager
    /// </summary>
    private QuestData FindQuestById(string questId)
    {
        var questManagerType = typeof(QuestManager);
        var allQuestsField = questManagerType.GetField("allQuests",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (allQuestsField != null)
        {
            QuestData[] allQuests = (QuestData[])allQuestsField.GetValue(QuestManager.Instance);
            if (allQuests != null)
            {
                foreach (var quest in allQuests)
                {
                    if (quest.questId == questId)
                        return quest;
                }
            }
        }

        return null;
    }

    #endregion

    #region Button Visibility & Interaction

    private void UpdateButtonVisibility()
    {
        if (buttonCanvasGroup == null || buttonInstance == null) return;

        bool shouldShow = isPlayerInRange && !isQuizActive && !hasCompletedQuiz && !isProcessing && IsObjectiveActive();

        if (shouldShow)
        {
            // Update button text dynamically
            UpdateButtonText();

            if (!buttonInstance.activeSelf)
                buttonInstance.SetActive(true);

            buttonCanvasGroup.alpha = Mathf.Lerp(buttonCanvasGroup.alpha, 1f, Time.deltaTime * buttonFadeSpeed);
            buttonCanvasGroup.interactable = buttonCanvasGroup.alpha > 0.9f;
            buttonCanvasGroup.blocksRaycasts = buttonCanvasGroup.interactable;
        }
        else
        {
            buttonCanvasGroup.alpha = Mathf.Lerp(buttonCanvasGroup.alpha, 0f, Time.deltaTime * buttonFadeSpeed);
            buttonCanvasGroup.interactable = false;
            buttonCanvasGroup.blocksRaycasts = false;

            if (buttonCanvasGroup.alpha < 0.01f && buttonInstance.activeSelf)
                buttonInstance.SetActive(false);
        }
    }

    private void UpdateButtonText()
    {
        if (buttonText == null) return;

        bool playerHasEternalKey = PlayerHasKey();

        if (playerHasEternalKey)
        {
            buttonText.text = "Start Final Evaluation";
            buttonText.color = Color.white;
        }
        else
        {
            buttonText.text = "🔒 Requires Eternal Key";
            buttonText.color = new Color(1f, 0.3f, 0.3f, 1f); // Red tint
        }
    }

    private bool IsObjectiveActive()
    {
        if (QuestManager.Instance == null) return false;

        QuestData currentQuest = QuestManager.Instance.GetCurrentQuest();
        if (currentQuest == null || currentQuest.questId != requiredQuestId) return false;

        QuestObjective objective = currentQuest.objectives.Find(o => o.objectiveTitle == requiredObjectiveTitle);
        return objective != null && objective.isActive && !objective.isCompleted;
    }

    #endregion

    #region Quiz Interaction

    private void OnButtonClicked()
    {
        if (debugMode)
            Debug.Log("[Barrier] Barrier Controller button clicked!");

        // CRITICAL: Validate player has the key BEFORE starting quiz
        if (!PlayerHasKey())
        {
            if (debugMode)
                Debug.LogWarning("[Barrier] ✗ ACCESS DENIED - Player does not have the Eternal Key from Quest 4!");

            ShowKeyRequiredMessage();
            return;
        }

        if (debugMode)
            Debug.Log("[Barrier] ✓ KEY VALIDATED - Starting Final Evaluation Quiz...");

        if (finalEvaluationQuiz == null || miniQuestUI == null)
        {
            Debug.LogError("[Barrier] Cannot start quiz - missing data or controller!");
            return;
        }

        isQuizActive = true;
        miniQuestUI.StartMiniQuest(finalEvaluationQuiz, OnQuizCompleted);

        if (debugMode)
            Debug.Log("[Barrier] Final Evaluation Quiz started!");
    }

    private void ShowKeyRequiredMessage()
    {
        Debug.Log("[Barrier] 🔒 The barrier is sealed. You need the Eternal Key from Quest 4 to proceed.");
    }

    private void OnQuizCompleted(bool success)
    {
        isQuizActive = false;

        if (success)
        {
            hasCompletedQuiz = true;

            if (debugMode)
            {
                Debug.Log("[Barrier] ✓✓✓ FINAL EVALUATION COMPLETED SUCCESSFULLY!");
                Debug.Log("[Barrier] Starting key insertion and barrier destruction sequence...");
            }

            StartCoroutine(BarrierDeactivationSequence());
        }
        else if (debugMode)
        {
            Debug.Log("[Barrier] Final Evaluation was cancelled or failed");
        }
    }

    #endregion

    #region Barrier Deactivation Sequence

    private IEnumerator BarrierDeactivationSequence()
    {
        isProcessing = true;

        // ============= PHASE 1: POST-QUIZ DELAY =============
        if (debugMode)
            Debug.Log($"[Barrier] PHASE 1: Waiting {postQuizDelay}s after quiz...");

        yield return new WaitForSeconds(postQuizDelay);

        // ============= PHASE 2: SPAWN KEY FOR INSERTION =============
        if (debugMode)
            Debug.Log("[Barrier] PHASE 2: Spawning key for insertion...");

        yield return StartCoroutine(SpawnKeyForInsertion());

        // ============= PHASE 3: KEY INSERTION ANIMATION =============
        if (debugMode)
            Debug.Log("[Barrier] PHASE 3: Animating key insertion...");

        yield return StartCoroutine(AnimateKeyInsertion());

        // ============= PHASE 4: KEY TWIST ANIMATION =============
        if (debugMode)
            Debug.Log("[Barrier] PHASE 4: Animating key twist...");

        yield return StartCoroutine(AnimateKeyTwist());

        // ============= PHASE 5: DELAY BEFORE BARRIER DESTRUCTION =============
        if (debugMode)
            Debug.Log($"[Barrier] PHASE 5: Waiting {preBarrierDestructionDelay}s before destroying barriers...");

        yield return new WaitForSeconds(preBarrierDestructionDelay);

        // ============= PHASE 6: DESTROY BARRIERS (PATH + LIGHTNING) =============
        if (debugMode)
            Debug.Log("[Barrier] PHASE 6: Destroying barriers...");

        DestroyAllBarriers();

        // ============= PHASE 7: COMPLETE OBJECTIVE =============
        if (debugMode)
            Debug.Log("[Barrier] PHASE 7: Completing Syntax Trial objective...");

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteObjectiveByTitle(requiredObjectiveTitle);

            if (debugMode)
                Debug.Log("[Barrier] ✓ Objective completed!");
        }

        // ============= PHASE 8: RESTORE GAME UI =============
        if (debugMode)
            Debug.Log($"[Barrier] PHASE 8: Waiting {uiRestoreDelay}s before UI restoration...");

        yield return new WaitForSeconds(uiRestoreDelay);

        if (debugMode)
            Debug.Log("[Barrier] PHASE 8 (continued): Restoring game UI...");

        yield return StartCoroutine(RestoreGameUI());

        isProcessing = false;

        if (debugMode)
            Debug.Log("[Barrier] ✓✓✓ Barrier destruction sequence completed! Player can now pass!");
    }

    private IEnumerator SpawnKeyForInsertion()
    {
        if (keyPrefab == null)
        {
            Debug.LogError("[Barrier] Key prefab not assigned! Cannot spawn key.");
            yield break;
        }

        Vector3 startPos = transform.position + keyInsertionStartOffset;

        // UPDATED: Spawn key with specific rotation for proper keyhole alignment
        Quaternion keyRotation = Quaternion.Euler(1.182f, 86.621f, 45.495f);
        spawnedKey = Instantiate(keyPrefab, startPos, keyRotation);

        if (debugMode)
            Debug.Log($"[Barrier] Key spawned at: {startPos} with rotation: {keyRotation.eulerAngles}");

        yield return null;
    }

    private IEnumerator AnimateKeyInsertion()
    {
        if (spawnedKey == null)
        {
            Debug.LogError("[Barrier] Key not spawned!");
            yield break;
        }

        Vector3 startPos = spawnedKey.transform.position;
        Vector3 endPos = transform.position + keyInsertionEndOffset;

        float elapsed = 0f;

        while (elapsed < keyInsertionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / keyInsertionDuration;

            spawnedKey.transform.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        spawnedKey.transform.position = endPos;

        if (debugMode)
            Debug.Log($"[Barrier] ✓ Key inserted at: {endPos}");
    }

    private IEnumerator AnimateKeyTwist()
    {
        if (spawnedKey == null)
        {
            Debug.LogError("[Barrier] Key not found for twist animation!");
            yield break;
        }

        Quaternion startRotation = spawnedKey.transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 0, keyTwistRotation);

        float elapsed = 0f;

        while (elapsed < keyTwistDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / keyTwistDuration;

            spawnedKey.transform.rotation = Quaternion.Lerp(startRotation, endRotation, t);

            yield return null;
        }

        spawnedKey.transform.rotation = endRotation;

        if (debugMode)
            Debug.Log($"[Barrier] ✓ Key twisted by {keyTwistRotation}°");
    }

    private void DestroyAllBarriers()
    {
        // SIMPLIFIED: Just destroy the Path Barrier GameObject (includes all 3 child Animators)
        if (pathBarrier != null)
        {
            int childAnimatorCount = pathBarrier.GetComponentsInChildren<Animator>().Length;
            
            Destroy(pathBarrier);
            
            if (debugMode)
                Debug.Log($"[Barrier] ✓ Path Barrier destroyed (removed {childAnimatorCount} child Animators from game)");
        }
        else
        {
            Debug.LogWarning("[Barrier] Path Barrier is null - cannot destroy");
        }

        // Destroy Lightning Barrier (audio will automatically stop via OnDestroy)
        if (lightningBarrier != null)
        {
            // Stop audio before destroying (optional - OnDestroy will handle it)
            LightningBarrierAudioController barrierAudio = lightningBarrier.GetComponent<LightningBarrierAudioController>();
            if (barrierAudio != null)
            {
                barrierAudio.StopBarrierAmbient();
                if (debugMode)
                    Debug.Log("[Barrier] ✓ Lightning Barrier audio stopped");
            }

            Destroy(lightningBarrier);
            
            if (debugMode)
                Debug.Log("[Barrier] ✓ Lightning barrier destroyed");
        }
        else
        {
            Debug.LogWarning("[Barrier] Lightning Barrier is null - cannot destroy");
        }
    }

    #endregion

    #region UI Restoration

    private IEnumerator RestoreGameUI()
    {
        if (gameUICanvasGroup == null)
        {
            Debug.LogWarning("[Barrier] No game UI CanvasGroup - skipping restoration");
            yield break;
        }

        if (debugMode)
            Debug.Log("[Barrier] Starting UI restoration...");

        // Restore CanvasGroup
        gameUICanvasGroup.alpha = 1f;
        gameUICanvasGroup.interactable = true;
        gameUICanvasGroup.blocksRaycasts = true;

        yield return null;

        // Restore Images
        Image[] images = gameUICanvasGroup.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            Color c = img.color;
            c.a = 1f;
            img.color = c;
        }

        // Restore Texts
        TextMeshProUGUI[] texts = gameUICanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI txt in texts)
        {
            Color c = txt.color;
            c.a = 1f;
            txt.color = c;
        }

        // Restore Sprites
        SpriteRenderer[] sprites = gameUICanvasGroup.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spr in sprites)
        {
            Color c = spr.color;
            c.a = 1f;
            spr.color = c;
        }

        yield return null;

        // Restore Buttons
        Button[] buttons = gameUICanvasGroup.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
            btn.interactable = true;

        // Restore Sliders
        Slider[] sliders = gameUICanvasGroup.GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
            slider.interactable = true;

        yield return null;

        Canvas.ForceUpdateCanvases();

        yield return null;

        // Reinitialize UIController (cached to avoid allocation)
        if (cachedUIController == null)
            cachedUIController = Object.FindFirstObjectByType<UIController>();

        if (cachedUIController != null)
        {
            cachedUIController.ReinitializeButtons();
            if (debugMode)
                Debug.Log("[Barrier] UIController reinitialized");
        }

        if (debugMode)
            Debug.Log("[Barrier] ✓ Game UI fully restored!");
    }

    #endregion

    #region Trigger Detection

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;

            if (debugMode)
                Debug.Log("[Barrier] Player entered barrier controller range");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerTransform = null;

            if (debugMode)
                Debug.Log("[Barrier] Player left barrier controller range");
        }
    }

    #endregion

    #region Cleanup

    void OnDestroy()
    {
        if (buttonInstance != null)
            Destroy(buttonInstance);
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        // Draw detection zone (use cached collider)
        if (cachedCollider == null)
            cachedCollider = GetComponent<Collider2D>();

        if (cachedCollider != null)
        {
            // FIXED: Only check PlayerHasKey during Play Mode
            bool hasKeyForGizmos = Application.isPlaying && PlayerHasKey();
            Gizmos.color = hasCompletedQuiz ? Color.green : (hasKeyForGizmos ? Color.cyan : Color.red);

            if (cachedCollider is BoxCollider2D boxCol)
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            else if (cachedCollider is CircleCollider2D circleCol)
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
        }

        // Draw button position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + buttonOffset, 0.3f);

        // Draw key insertion path
        Gizmos.color = Color.magenta;
        Vector3 insertStart = transform.position + keyInsertionStartOffset;
        Vector3 insertEnd = transform.position + keyInsertionEndOffset;

        Gizmos.DrawWireSphere(insertStart, 0.2f);
        Gizmos.DrawWireSphere(insertEnd, 0.2f);
        Gizmos.DrawLine(insertStart, insertEnd);

#if UNITY_EDITOR
        // FIXED: Only check key status during Play Mode
        bool hasKeyForLabel = Application.isPlaying && PlayerHasKey();
        string status = hasCompletedQuiz ? "DESTROYED" : (isProcessing ? "PROCESSING..." : "ACTIVE");
        string keyStatus = Application.isPlaying ? (hasKeyForLabel ? "✓ HAS KEY" : "✗ NO KEY") : "KEY STATUS: NOT PLAYING";

        int pathAnimatorCount = 0;
        if (pathBarrier != null)
        {
            Animator[] anims = pathBarrier.GetComponentsInChildren<Animator>();
            pathAnimatorCount = anims != null ? anims.Length : 0;
        }

        float totalTime = postQuizDelay + keyInsertionDuration + keyTwistDuration + preBarrierDestructionDelay + uiRestoreDelay;

        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"FINAL EVALUATION BARRIER [{status}]\n{requiredObjectiveTitle}\n\nKEY REQUIREMENT:\n{keyStatus}\nQuest: {keyQuestId}\nObjective: {keyObjectiveTitle}\n\nPath Barrier: {(pathBarrier != null ? $"✓ ({pathAnimatorCount} Child Animators)" : "✗ DESTROYED")}\nLightning Barrier: {(lightningBarrier != null ? "✓" : "✗ DESTROYED")}\nQuiz Data: {(finalEvaluationQuiz != null ? "✓" : "✗")}\nKey Prefab: {(keyPrefab != null ? "✓" : "✗")}\n\nSEQUENCE:\n1. Player approaches → KEY CHECK\n2. Start Final Quiz (10 questions)\n3. Complete quiz\n4. Spawn key for insertion\n5. Key insertion {keyInsertionDuration}s\n6. Key twist {keyTwistDuration}s\n7. DESTROY Path Barrier ({pathAnimatorCount} Animators removed)\n8. DESTROY Lightning Barrier\n9. Complete objective\n10. Restore UI\n\nTotal: {totalTime:F1}s\n\n⚠ WARNING: Player dies if touching active barrier!",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan } });
#endif
    }

    #endregion
}