using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// QUEST 4 OBJECTIVE 3: String Exercise Chest
/// Handles chest quiz interaction, chest opening animation, and key spawning
/// Similar to HelloWorldAltarTrigger and RuneCollectionTrigger pattern
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StringExerciseChestTrigger : MonoBehaviour
{
    [Header("Quest Settings")]
    [SerializeField] private string requiredQuestId = "Q4_StringsPrinting";
    [SerializeField] private string requiredObjectiveTitle = "Complete the String Exercises";

    [Header("Mini Quest Data")]
    [Tooltip("Quiz data with 7 String & Printing questions")]
    [SerializeField] private MiniQuestData miniQuestData;

    [Header("Interaction Button Prefab")]
    [Tooltip("Prefab with world-space Canvas containing the 'Open Chest' button")]
    [SerializeField] private GameObject interactionButtonPrefab;

    [Header("Button Position")]
    [SerializeField] private Vector3 buttonOffset = new Vector3(0, 2f, 0);

    [Header("Button Settings")]
    [SerializeField] private float buttonFadeSpeed = 5f;

    [Header("Chest Components")]
    [Tooltip("Animator for the chest (auto-found if not assigned)")]
    [SerializeField] private Animator chestAnimator;
    
    [Tooltip("Trigger parameter name for chest opening animation")]
    [SerializeField] private string openTriggerName = "Open";
    
    [Tooltip("Duration of chest opening animation (seconds)")]
    [SerializeField] private float openAnimationDuration = 2f;

    [Header("Key Spawning")]
    [Tooltip("Key GameObject to spawn (must have KeyCollectionTrigger script)")]
    [SerializeField] private GameObject keyPrefab;
    
    [Tooltip("Position where key spawns (inside chest)")]
    [SerializeField] private Vector3 keySpawnOffset = new Vector3(0, 0.5f, 0);
    
    [Tooltip("Position where key lands (left side of chest)")]
    [SerializeField] private Vector3 keyLandingOffset = new Vector3(-2f, 0, 0);
    
    [Tooltip("Arc height for key jump animation")]
    [SerializeField] private float keyArcHeight = 3f;
    
    [Tooltip("Duration of key jump animation")]
    [SerializeField] private float keyJumpDuration = 1.5f;

    [Header("Timing")]
    [Tooltip("Delay after quiz completion before chest opens")]
    [SerializeField] private float postQuizDelay = 0.5f;
    
    [Tooltip("Delay after chest opens before key spawns")]
    [SerializeField] private float keySpawnDelay = 0.5f;
    
    [Tooltip("Delay after key lands before restoring UI")]
    [SerializeField] private float uiRestoreDelay = 0.5f;

    [Header("Game UI Reference")]
    [SerializeField] private CanvasGroup gameUICanvasGroup;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // State
    private bool isPlayerInRange = false;
    private bool isQuizActive = false;
    private bool hasOpenedChest = false;
    private bool isOpening = false;
    private Transform playerTransform;
    private MiniQuestUIController miniQuestUI;

    // Button
    private GameObject buttonInstance;
    private Button interactionButton;
    private TextMeshProUGUI buttonText;
    private CanvasGroup buttonCanvasGroup;

    // Key reference
    private GameObject spawnedKey;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"StringExerciseChestTrigger on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }

        // Auto-find chest animator
        if (chestAnimator == null)
        {
            chestAnimator = GetComponentInChildren<Animator>();
            if (chestAnimator != null && debugMode)
                Debug.Log($"[StringChest] Auto-found Chest Animator: {chestAnimator.gameObject.name}");
        }

        // Auto-find game UI
        if (gameUICanvasGroup == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.name != "Quiz UI")
            {
                gameUICanvasGroup = canvas.GetComponent<CanvasGroup>();
                if (debugMode)
                    Debug.Log("[StringChest] Auto-found game UI CanvasGroup");
            }
        }
    }

    void Start()
    {
        CacheMiniQuestUI();
        ValidateMiniQuestData();
        InitializeButton();
        ValidateChestSetup();
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
                Debug.LogError("[StringChest] MiniQuestUIController not found in scene!");
            else if (debugMode)
                Debug.Log("[StringChest] MiniQuestUIController found");
        }
    }

    private void ValidateMiniQuestData()
    {
        if (miniQuestData == null)
        {
            Debug.LogError("[StringChest] MiniQuestData not assigned!");
            return;
        }

        if (miniQuestData.questions.Count == 0)
            Debug.LogError($"[StringChest] MiniQuestData '{miniQuestData.name}' has no questions!");

        if (debugMode)
        {
            Debug.Log($"[StringChest] Mini Quest Data loaded: {miniQuestData.miniQuestTitle}");
            Debug.Log($"[StringChest] Total questions: {miniQuestData.GetTotalQuestions()}");
        }
    }

    private void InitializeButton()
    {
        if (interactionButtonPrefab == null)
        {
            Debug.LogError("[StringChest] Interaction Button Prefab not assigned!");
            return;
        }

        Vector3 buttonPosition = transform.position + buttonOffset;
        buttonInstance = Instantiate(interactionButtonPrefab, buttonPosition, Quaternion.identity);
        buttonInstance.transform.SetParent(transform, true);

        interactionButton = buttonInstance.GetComponentInChildren<Button>();
        buttonCanvasGroup = buttonInstance.GetComponent<CanvasGroup>();
        buttonText = buttonInstance.GetComponentInChildren<TextMeshProUGUI>();

        if (interactionButton == null)
        {
            Debug.LogError("[StringChest] Button component not found in prefab!");
            return;
        }

        if (buttonCanvasGroup == null)
            buttonCanvasGroup = buttonInstance.AddComponent<CanvasGroup>();

        if (buttonText != null)
            buttonText.text = "Open Chest";

        interactionButton.onClick.AddListener(OnButtonClicked);

        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;
        buttonInstance.SetActive(false);

        if (debugMode)
            Debug.Log($"[StringChest] Button instantiated at: {buttonPosition}");
    }

    private void ValidateChestSetup()
    {
        if (chestAnimator == null)
        {
            Debug.LogWarning("[StringChest] No Animator assigned - chest won't animate");
            return;
        }

        if (chestAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("[StringChest] Animator has no controller!");
            return;
        }

        // Validate trigger parameter
        bool hasTrigger = false;
        foreach (var param in chestAnimator.parameters)
        {
            if (param.name == openTriggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasTrigger = true;
                if (debugMode)
                    Debug.Log($"[StringChest] ? Found trigger '{openTriggerName}' in Animator");
                break;
            }
        }

        if (!hasTrigger)
            Debug.LogError($"[StringChest] Trigger '{openTriggerName}' NOT FOUND in Animator!");
    }

    #endregion

    #region Button Visibility

    private void UpdateButtonVisibility()
    {
        if (buttonCanvasGroup == null || buttonInstance == null) return;

        bool shouldShow = isPlayerInRange && !isQuizActive && !hasOpenedChest && !isOpening && IsObjectiveActive();

        if (shouldShow)
        {
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
            Debug.Log("[StringChest] Open Chest button clicked!");

        if (miniQuestData == null || miniQuestUI == null)
        {
            Debug.LogError("[StringChest] Cannot start quiz - missing data or controller!");
            return;
        }

        isQuizActive = true;
        miniQuestUI.StartMiniQuest(miniQuestData, OnQuizCompleted);

        if (debugMode)
            Debug.Log("[StringChest] Quiz started!");
    }

    private void OnQuizCompleted(bool success)
    {
        isQuizActive = false;

        if (success)
        {
            hasOpenedChest = true;

            if (debugMode)
            {
                Debug.Log("[StringChest] Quiz completed successfully!");
                Debug.Log("[StringChest] Starting chest opening sequence...");
            }

            StartCoroutine(ChestOpeningSequence());
        }
        else if (debugMode)
        {
            Debug.Log("[StringChest] Quiz was cancelled or failed");
        }
    }

    #endregion

    #region Chest Opening & Key Spawning Sequence

    private IEnumerator ChestOpeningSequence()
    {
        isOpening = true;

        // ============= PHASE 1: POST-QUIZ DELAY (UI REMAINS HIDDEN) =============
        if (debugMode)
            Debug.Log($"[StringChest] PHASE 1: Waiting {postQuizDelay}s after quiz...");

        yield return new WaitForSeconds(postQuizDelay);

        // ============= PHASE 2: TRIGGER CHEST OPENING ANIMATION =============
        if (debugMode)
            Debug.Log("[StringChest] PHASE 2: Opening chest...");

        TriggerChestOpen();

        // ============= PHASE 3: WAIT FOR CHEST ANIMATION =============
        if (debugMode)
            Debug.Log($"[StringChest] PHASE 3: Waiting {openAnimationDuration}s for chest to open...");

        yield return new WaitForSeconds(openAnimationDuration);

        // ============= PHASE 4: SPAWN KEY WITH JUMP ANIMATION =============
        if (debugMode)
        {
            Debug.Log($"[StringChest] PHASE 4: Waiting {keySpawnDelay}s before spawning key...");
        }

        yield return new WaitForSeconds(keySpawnDelay);

        if (debugMode)
            Debug.Log("[StringChest] PHASE 4 (continued): Spawning key with jump animation...");

        yield return StartCoroutine(SpawnAndAnimateKey());

        // ============= PHASE 5: RESTORE GAME UI =============
        if (debugMode)
            Debug.Log($"[StringChest] PHASE 5: Waiting {uiRestoreDelay}s before UI restoration...");

        yield return new WaitForSeconds(uiRestoreDelay);

        if (debugMode)
            Debug.Log("[StringChest] PHASE 5 (continued): Restoring game UI...");

        yield return StartCoroutine(RestoreGameUI());

        // ============= PHASE 6: OBJECTIVE COMPLETED (DON'T AUTO-COMPLETE YET) =============
        // NOTE: Objective completes when player picks up the key, not when chest opens
        if (debugMode)
            Debug.Log("[StringChest] PHASE 6: Chest opened! Key is now collectible.");

        isOpening = false;

        if (debugMode)
            Debug.Log("[StringChest] ??? Chest opening sequence completed!");
    }

    private void TriggerChestOpen()
    {
        if (chestAnimator == null)
        {
            Debug.LogWarning("[StringChest] No animator - skipping animation");
            return;
        }

        chestAnimator.SetTrigger(openTriggerName);

        if (debugMode)
            Debug.Log($"[StringChest] ? Triggered chest opening: '{openTriggerName}'");

        chestAnimator.Update(0f);
    }

    private IEnumerator SpawnAndAnimateKey()
    {
        if (keyPrefab == null)
        {
            Debug.LogError("[StringChest] Key prefab not assigned! Cannot spawn key.");
            yield break;
        }

        // Calculate positions
        Vector3 spawnPos = transform.position + keySpawnOffset;
        Vector3 landingPos = transform.position + keyLandingOffset;

        // Spawn key at chest position
        spawnedKey = Instantiate(keyPrefab, spawnPos, Quaternion.identity);

        if (debugMode)
            Debug.Log($"[StringChest] Key spawned at: {spawnPos}");

        // Disable key's collider and interaction during animation
        Collider2D keyCollider = spawnedKey.GetComponent<Collider2D>();
        if (keyCollider != null)
            keyCollider.enabled = false;

        KeyCollectionTrigger keyScript = spawnedKey.GetComponent<KeyCollectionTrigger>();
        if (keyScript != null)
            keyScript.enabled = false;

        // Animate key jump (parabolic arc)
        float elapsed = 0f;

        while (elapsed < keyJumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / keyJumpDuration;

            // Horizontal movement (linear)
            Vector3 currentPos = Vector3.Lerp(spawnPos, landingPos, t);

            // Vertical movement (parabolic arc)
            float arcProgress = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0
            currentPos.y += arcProgress * keyArcHeight;

            spawnedKey.transform.position = currentPos;

            yield return null;
        }

        // Snap to final position
        spawnedKey.transform.position = landingPos;

        // Re-enable collider and interaction
        if (keyCollider != null)
            keyCollider.enabled = true;

        if (keyScript != null)
            keyScript.enabled = true;

        if (debugMode)
            Debug.Log($"[StringChest] ? Key landed at: {landingPos}");
    }

    #endregion

    #region UI Restoration

    private IEnumerator RestoreGameUI()
    {
        if (gameUICanvasGroup == null)
        {
            Debug.LogWarning("[StringChest] No game UI CanvasGroup - skipping restoration");
            yield break;
        }

        if (debugMode)
            Debug.Log("[StringChest] Starting UI restoration...");

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

        // Reinitialize UIController
        UIController uiController = Object.FindFirstObjectByType<UIController>();
        if (uiController != null)
        {
            uiController.ReinitializeButtons();
            if (debugMode)
                Debug.Log("[StringChest] UIController reinitialized");
        }

        if (debugMode)
            Debug.Log("[StringChest] ? Game UI fully restored!");
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
                Debug.Log("[StringChest] Player entered chest range");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerTransform = null;

            if (debugMode)
                Debug.Log("[StringChest] Player left chest range");
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
        // Draw detection zone
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = hasOpenedChest ? Color.green : Color.yellow;

            if (col is BoxCollider2D boxCol)
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            else if (col is CircleCollider2D circleCol)
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
        }

        // Draw button position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + buttonOffset, 0.3f);

        // Draw key spawn and landing positions
        Gizmos.color = Color.magenta;
        Vector3 spawnPos = transform.position + keySpawnOffset;
        Vector3 landPos = transform.position + keyLandingOffset;
        
        Gizmos.DrawWireSphere(spawnPos, 0.2f);
        Gizmos.DrawWireSphere(landPos, 0.2f);
        Gizmos.DrawLine(spawnPos, landPos);

        // Draw arc preview
        Gizmos.color = Color.yellow;
        Vector3 prevPos = spawnPos;
        for (int i = 1; i <= 10; i++)
        {
            float t = i / 10f;
            Vector3 pos = Vector3.Lerp(spawnPos, landPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * keyArcHeight;
            Gizmos.DrawLine(prevPos, pos);
            prevPos = pos;
        }

#if UNITY_EDITOR
        string status = hasOpenedChest ? "OPENED" : (isOpening ? "OPENING..." : "CLOSED");
        float totalTime = postQuizDelay + openAnimationDuration + keySpawnDelay + keyJumpDuration + uiRestoreDelay;

        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
            $"String Exercise Chest [{status}]\n{requiredObjectiveTitle}\nChest Animator: {(chestAnimator != null ? "?" : "?")}\nKey Prefab: {(keyPrefab != null ? "?" : "?")}\n\nSEQUENCE:\n1. Player clicks 'Open Chest'\n2. Quiz (7 questions)\n3. Delay {postQuizDelay}s\n4. Chest opens {openAnimationDuration}s\n5. Key spawns & jumps {keyJumpDuration}s\n6. Restore UI {uiRestoreDelay}s\n7. Player collects key\n\nTotal: {totalTime:F1}s",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan } });
#endif
    }

    #endregion
}