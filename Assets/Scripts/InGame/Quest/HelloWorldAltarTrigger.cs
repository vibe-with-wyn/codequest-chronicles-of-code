using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Handles player detection at the Hello World Altar and manages the "Start Quiz" button visibility
/// CORRECTED: Phase 1 starts AFTER Quiz UI closes, Phase 4 restores UI after 1 second delay
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HelloWorldAltarTrigger : MonoBehaviour
{
    [Header("Quest Settings")]
    [SerializeField] private string requiredQuestId = "Q2_HelloJava";
    [SerializeField] private string requiredObjectiveTitle = "Complete the Hello World Altar Quiz";

    [Header("Mini Quest Data")]
    [SerializeField] private MiniQuestData miniQuestData;

    [Header("Start Quiz Button Prefab")]
    [Tooltip("Prefab with world-space Canvas containing the Start Quiz Button")]
    [SerializeField] private GameObject startQuizButtonPrefab;

    [Header("Button Position")]
    [Tooltip("Offset from the altar where the button should appear (world space)")]
    [SerializeField] private Vector3 buttonOffset = new Vector3(0, 2f, 0);

    [Header("Button Settings")]
    [SerializeField] private float buttonFadeSpeed = 5f;

    [Header("Altar Activation Settings")]
    [Tooltip("Delay AFTER Quiz UI closes before triggering altar activation (seconds)")]
    [SerializeField] private float altarActivationDelay = 1f;
    
    [Tooltip("Animator component for the altar (auto-found if not assigned)")]
    [SerializeField] private Animator altarAnimator;
    
    [Tooltip("Trigger parameter name for altar activation animation")]
    [SerializeField] private string activationTriggerName = "Activate";
    
    [Tooltip("Duration of the altar activation animation (seconds) - MUST MATCH YOUR ANIMATION LENGTH")]
    [SerializeField] private float activationAnimationDuration = 2f;
    
    [Tooltip("Delay before UI restoration and Quest 3 display (seconds)")]
    [SerializeField] private float quest3DisplayDelay = 1f;

    [Header("Altar Effect Settings")]
    [Tooltip("Altar Effect GameObject to disable after quiz completion (auto-found if not assigned)")]
    [SerializeField] private GameObject altarEffectObject;

    [Header("Game UI Reference")]
    [Tooltip("Main game Canvas to restore before Quest 3 displays")]
    [SerializeField] private CanvasGroup gameUICanvasGroup;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool isPlayerInRange = false;
    private bool isQuizActive = false;
    private bool hasCompletedQuiz = false;
    private bool isActivating = false;
    private Transform playerTransform;
    private MiniQuestUIController miniQuestUI;

    private GameObject startQuizButtonInstance;
    private Button startQuizButton;
    private CanvasGroup buttonCanvasGroup;

    void Awake()
    {
        // Ensure collider is set to trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"HelloWorldAltarTrigger on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }
        
        // Auto-find altar animator if not assigned
        if (altarAnimator == null)
        {
            altarAnimator = GetComponent<Animator>();
            if (altarAnimator == null)
            {
                altarAnimator = GetComponentInChildren<Animator>();
            }
            
            if (altarAnimator != null && debugMode)
            {
                Debug.Log($"[HelloWorldAltar] Auto-found Animator component: {altarAnimator.gameObject.name}");
            }
        }

        // Auto-find Altar Effect if not assigned
        if (altarEffectObject == null)
        {
            Transform altarEffect = transform.Find("Altar Effect");
            if (altarEffect != null)
            {
                altarEffectObject = altarEffect.gameObject;
                if (debugMode)
                {
                    Debug.Log($"[HelloWorldAltar] Auto-found Altar Effect: {altarEffectObject.name}");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning("[HelloWorldAltar] Altar Effect GameObject not found - animation will not be disabled");
            }
        }

        // Auto-find game UI if not assigned
        if (gameUICanvasGroup == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.name != "Quiz UI")
            {
                gameUICanvasGroup = canvas.GetComponent<CanvasGroup>();
                if (debugMode)
                {
                    Debug.Log("[HelloWorldAltar] Auto-found game UI CanvasGroup");
                }
            }
        }
    }

    void Start()
    {
        CacheMiniQuestUI();
        ValidateMiniQuestData();
        InitializeStartQuizButton();
        ValidateAnimatorSetup();
    }

    void Update()
    {
        UpdateButtonVisibility();
    }

    private void InitializeStartQuizButton()
    {
        if (startQuizButtonPrefab == null)
        {
            Debug.LogError("[HelloWorldAltar] Start Quiz Button Prefab not assigned!");
            return;
        }

        Vector3 buttonPosition = transform.position + buttonOffset;
        startQuizButtonInstance = Instantiate(startQuizButtonPrefab, buttonPosition, Quaternion.identity);
        startQuizButtonInstance.transform.SetParent(transform, true);

        startQuizButton = startQuizButtonInstance.GetComponentInChildren<Button>();
        buttonCanvasGroup = startQuizButtonInstance.GetComponent<CanvasGroup>();

        if (startQuizButton == null)
        {
            Debug.LogError("[HelloWorldAltar] Button component not found in Start Quiz Button Prefab!");
            return;
        }

        if (buttonCanvasGroup == null)
        {
            buttonCanvasGroup = startQuizButtonInstance.AddComponent<CanvasGroup>();
        }

        startQuizButton.onClick.AddListener(OnStartQuizButtonClicked);

        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;
        startQuizButtonInstance.SetActive(false);

        if (debugMode)
        {
            Debug.Log($"[HelloWorldAltar] Start Quiz Button instantiated at position: {buttonPosition}");
        }
    }

    private void CacheMiniQuestUI()
    {
        if (miniQuestUI == null)
        {
            miniQuestUI = Object.FindFirstObjectByType<MiniQuestUIController>();

            if (miniQuestUI == null)
            {
                Debug.LogError("[HelloWorldAltar] MiniQuestUIController not found in scene!");
            }
            else if (debugMode)
            {
                Debug.Log("[HelloWorldAltar] MiniQuestUIController found successfully");
            }
        }
    }

    private void ValidateMiniQuestData()
    {
        if (miniQuestData == null)
        {
            Debug.LogError("[HelloWorldAltar] MiniQuestData not assigned!");
            return;
        }

        if (miniQuestData.questions.Count == 0)
        {
            Debug.LogError($"[HelloWorldAltar] MiniQuestData '{miniQuestData.name}' has no questions!");
        }

        if (debugMode)
        {
            Debug.Log($"[HelloWorldAltar] Mini Quest Data loaded: {miniQuestData.miniQuestTitle}");
            Debug.Log($"[HelloWorldAltar] Total questions: {miniQuestData.GetTotalQuestions()}");
        }
    }

    private void ValidateAnimatorSetup()
    {
        if (altarAnimator == null)
        {
            Debug.LogWarning("[HelloWorldAltar] No Animator assigned - altar activation will be skipped");
            return;
        }

        if (altarAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("[HelloWorldAltar] Animator has no controller assigned!");
            return;
        }

        bool hasTrigger = false;
        foreach (var param in altarAnimator.parameters)
        {
            if (param.name == activationTriggerName)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasTrigger = true;
                    if (debugMode)
                    {
                        Debug.Log($"[HelloWorldAltar] ✓ Found trigger parameter '{activationTriggerName}' in Animator");
                    }
                }
                else
                {
                    Debug.LogError($"[HelloWorldAltar] Parameter '{activationTriggerName}' exists but is NOT a Trigger!");
                }
                break;
            }
        }

        if (!hasTrigger)
        {
            Debug.LogError($"[HelloWorldAltar] Trigger parameter '{activationTriggerName}' NOT FOUND in Animator!");
        }
    }

    private void UpdateButtonVisibility()
    {
        if (buttonCanvasGroup == null || startQuizButtonInstance == null) return;

        bool shouldShowButton = isPlayerInRange && !isQuizActive && !hasCompletedQuiz && !isActivating && IsObjectiveActive();

        if (shouldShowButton)
        {
            if (!startQuizButtonInstance.activeSelf)
            {
                startQuizButtonInstance.SetActive(true);
            }

            buttonCanvasGroup.alpha = Mathf.Lerp(buttonCanvasGroup.alpha, 1f, Time.deltaTime * buttonFadeSpeed);
            buttonCanvasGroup.interactable = buttonCanvasGroup.alpha > 0.9f;
            buttonCanvasGroup.blocksRaycasts = buttonCanvasGroup.interactable;
        }
        else
        {
            buttonCanvasGroup.alpha = Mathf.Lerp(buttonCanvasGroup.alpha, 0f, Time.deltaTime * buttonFadeSpeed);
            buttonCanvasGroup.interactable = false;
            buttonCanvasGroup.blocksRaycasts = false;

            if (buttonCanvasGroup.alpha < 0.01f && startQuizButtonInstance.activeSelf)
            {
                startQuizButtonInstance.SetActive(false);
            }
        }
    }

    private bool IsObjectiveActive()
    {
        if (QuestManager.Instance == null) return false;

        QuestData currentQuest = QuestManager.Instance.GetCurrentQuest();
        if (currentQuest == null || currentQuest.questId != requiredQuestId) return false;

        QuestObjective objective = currentQuest.objectives.Find(o => o.objectiveTitle == requiredObjectiveTitle);
        if (objective != null && objective.isActive && !objective.isCompleted)
        {
            return true;
        }

        return false;
    }

    private void OnStartQuizButtonClicked()
    {
        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] Start Quiz button clicked!");
        }

        if (miniQuestData == null || miniQuestUI == null)
        {
            Debug.LogError("[HelloWorldAltar] Cannot start quiz - missing data or controller!");
            return;
        }

        isQuizActive = true;
        miniQuestUI.StartMiniQuest(miniQuestData, OnQuizCompleted);

        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] Quiz started!");
        }
    }

    private void OnQuizCompleted(bool success)
    {
        isQuizActive = false;

        if (success)
        {
            hasCompletedQuiz = true;

            if (debugMode)
            {
                Debug.Log("[HelloWorldAltar] Quiz completed successfully!");
                Debug.Log("[HelloWorldAltar] Quiz UI will now close (handled by MiniQuestUI)...");
                Debug.Log("[HelloWorldAltar] Starting delayed activation sequence AFTER Quiz UI closes...");
            }

            StartCoroutine(DelayedActivationSequence());
        }
        else if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] Quiz was cancelled or failed");
        }
    }

    // CORRECTED: Phase 1 starts AFTER Quiz UI closes, Phase 4 includes 1s delay + UI restoration
    private IEnumerator DelayedActivationSequence()
    {
        isActivating = true;

        // ============= PHASE 1: DISABLE ALTAR EFFECT & WAIT 1 SECOND AFTER QUIZ UI CLOSES =============
        if (debugMode)
        {
            Debug.Log($"[HelloWorldAltar] PHASE 1: Quiz UI has closed. Disabling Altar Effect animation...");
        }

        // Disable Altar Effect animation
        if (altarEffectObject != null)
        {
            altarEffectObject.SetActive(false);
            if (debugMode)
            {
                Debug.Log("[HelloWorldAltar] ✓ Altar Effect disabled");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("[HelloWorldAltar] Altar Effect GameObject not assigned - skipping disable");
        }

        if (debugMode)
        {
            Debug.Log($"[HelloWorldAltar] PHASE 1 (continued): Waiting {altarActivationDelay}s before altar activation...");
        }
        
        yield return new WaitForSeconds(altarActivationDelay);

        // ============= PHASE 2: TRIGGER ALTAR ACTIVATION ANIMATION =============
        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] PHASE 2: Triggering altar activation animation...");
        }

        TriggerAltarActivation();

        // ============= PHASE 3: WAIT FOR ACTIVATION ANIMATION (2 seconds) =============
        if (debugMode)
        {
            Debug.Log($"[HelloWorldAltar] PHASE 3: Waiting {activationAnimationDuration}s for altar activation animation to complete...");
            Debug.Log("[HelloWorldAltar] UI remains HIDDEN during altar activation");
        }
        
        yield return new WaitForSeconds(activationAnimationDuration);

        // ============= PHASE 4: WAIT 1 SECOND, THEN RESTORE GAME UI =============
        if (debugMode)
        {
            Debug.Log($"[HelloWorldAltar] PHASE 4: Altar activation complete! Waiting {quest3DisplayDelay}s before UI restoration...");
        }
        
        yield return new WaitForSeconds(quest3DisplayDelay);

        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] PHASE 4 (continued): Now restoring game UI...");
        }

        yield return StartCoroutine(RestoreGameUI());

        // ============= PHASE 5: COMPLETE QUEST OBJECTIVE (TRIGGERS QUEST 3 DISPLAY) =============
        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] PHASE 5: UI restored! Completing quest objective - Quest 3 will display now!");
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteObjectiveByTitle(requiredObjectiveTitle);
            if (debugMode)
            {
                Debug.Log($"[HelloWorldAltar] ✓ Quest objective '{requiredObjectiveTitle}' completed!");
                Debug.Log("[HelloWorldAltar] Quest 3 should now be displaying");
            }
        }

        isActivating = false;

        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] ✓✓✓ Delayed activation sequence completed!");
        }
    }

    // Restore game UI method
    private IEnumerator RestoreGameUI()
    {
        if (gameUICanvasGroup == null)
        {
            Debug.LogWarning("[HelloWorldAltar] No game UI CanvasGroup assigned - skipping UI restoration");
            yield break;
        }

        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] Starting game UI restoration...");
        }

        // Restore CanvasGroup properties
        gameUICanvasGroup.alpha = 1f;
        gameUICanvasGroup.interactable = true;
        gameUICanvasGroup.blocksRaycasts = true;

        yield return null;

        // Restore all Image components
        Image[] allImages = gameUICanvasGroup.GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            Color color = img.color;
            color.a = 1f;
            img.color = color;
        }

        // Restore all TextMeshProUGUI components
        TextMeshProUGUI[] allTexts = gameUICanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in allTexts)
        {
            Color color = text.color;
            color.a = 1f;
            text.color = color;
        }

        // Restore all SpriteRenderer components (UI sprite icons)
        SpriteRenderer[] allSprites = gameUICanvasGroup.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sprite in allSprites)
        {
            Color color = sprite.color;
            color.a = 1f;
            sprite.color = color;
        }

        yield return null;

        // Restore button interactivity
        Button[] buttons = gameUICanvasGroup.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            button.interactable = true;
        }

        // Restore slider interactivity
        Slider[] sliders = gameUICanvasGroup.GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            slider.interactable = true;
        }

        yield return null;

        // Force canvas update
        Canvas.ForceUpdateCanvases();

        yield return null;

        // Reinitialize UIController buttons
        UIController uiController = Object.FindFirstObjectByType<UIController>();
        if (uiController != null)
        {
            uiController.ReinitializeButtons();
            if (debugMode)
            {
                Debug.Log("[HelloWorldAltar] UIController buttons reinitialized");
            }
        }

        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] ✓ Game UI fully restored!");
            Debug.Log($"[HelloWorldAltar] Restored: {allImages.Length} images, {allTexts.Length} texts, {allSprites.Length} sprites, {buttons.Length} buttons, {sliders.Length} sliders");
        }
    }

    private void TriggerAltarActivation()
    {
        if (altarAnimator == null)
        {
            if (debugMode)
            {
                Debug.LogWarning("[HelloWorldAltar] No Animator assigned - skipping activation animation");
            }
            return;
        }

        if (altarAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("[HelloWorldAltar] Animator has no controller - cannot trigger animation!");
            return;
        }

        bool hasTrigger = false;
        AnimatorControllerParameterType foundType = AnimatorControllerParameterType.Float;
        
        foreach (var param in altarAnimator.parameters)
        {
            if (param.name == activationTriggerName)
            {
                hasTrigger = true;
                foundType = param.type;
                break;
            }
        }

        if (!hasTrigger)
        {
            Debug.LogError($"[HelloWorldAltar] Trigger parameter '{activationTriggerName}' NOT FOUND!");
            return;
        }

        if (foundType != AnimatorControllerParameterType.Trigger)
        {
            Debug.LogError($"[HelloWorldAltar] Parameter '{activationTriggerName}' is NOT a Trigger!");
            return;
        }

        altarAnimator.SetTrigger(activationTriggerName);
        
        if (debugMode)
        {
            Debug.Log($"[HelloWorldAltar] ✓ Triggered altar activation: '{activationTriggerName}'");
        }

        altarAnimator.Update(0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;

            if (debugMode)
            {
                Debug.Log("[HelloWorldAltar] Player entered altar range");
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerTransform = null;

            if (debugMode)
            {
                Debug.Log("[HelloWorldAltar] Player left altar range");
            }
        }
    }

    void OnDestroy()
    {
        if (startQuizButtonInstance != null)
        {
            Destroy(startQuizButtonInstance);
        }
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = hasCompletedQuiz ? Color.green : Color.yellow;

            if (col is BoxCollider2D boxCol)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            }
            else if (col is CircleCollider2D circleCol)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
            }
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + buttonOffset, 0.3f);

#if UNITY_EDITOR
        string statusText = hasCompletedQuiz ? "COMPLETED" : (isActivating ? "ACTIVATING..." : "READY");
        string animatorStatus = altarAnimator != null ? "✓" : "✗ MISSING";
        string uiStatus = gameUICanvasGroup != null ? "✓" : "✗ MISSING";
        string altarEffectStatus = altarEffectObject != null ? "✓" : "✗ MISSING";
        float totalDelay = altarActivationDelay + activationAnimationDuration + quest3DisplayDelay;
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
            $"Hello World Altar [{statusText}]\n{requiredObjectiveTitle}\nAnimator: {animatorStatus} | Game UI: {uiStatus} | Altar Effect: {altarEffectStatus}\nTrigger: '{activationTriggerName}'\n\nSEQUENCE:\n1. Quiz UI Closes (MiniQuestUI)\n2. Disable Altar Effect + Wait {altarActivationDelay}s\n3. Altar Animation {activationAnimationDuration}s\n4. Wait {quest3DisplayDelay}s + Restore UI\n5. Quest 3 Displays\n\nTotal Time: {totalDelay}s",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan } });
#endif
    }
}