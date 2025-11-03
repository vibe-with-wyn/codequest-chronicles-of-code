using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Handles player detection at Rune objects and manages collection through quiz
/// Each rune displays a collection button, shows quiz UI, and fades out upon completion
/// UI is restored ONLY after rune is completely disabled
/// Designed for Quest 3, Objective 3: Collect all four elemental runes (Fire, Air, Ice, Water)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RuneCollectionTrigger : MonoBehaviour
{
    [Header("Rune Identification")]
    [SerializeField] private string runeName = "Fire"; // Fire, Air, Ice, Water
    [Tooltip("Unique identifier for this rune (e.g., Q3_Obj3_RuneFire)")]
    [SerializeField] private string runeId = "Q3_Obj3_RuneFire";
    
    [Header("Quest Settings")]
    [SerializeField] private string requiredQuestId = "Q3_ElementalRunes";
    [SerializeField] private string requiredObjectiveTitle = "Collect all four elemental runes";

    [Header("Mini Quest Data")]
    [Tooltip("Quiz data for this specific rune (contains 1 question)")]
    [SerializeField] private MiniQuestData miniQuestData;

    [Header("Collection Button Prefab")]
    [Tooltip("Prefab with world-space Canvas containing the Collection Button")]
    [SerializeField] private GameObject collectionButtonPrefab;

    [Header("Button Position")]
    [Tooltip("Offset from the rune where the button should appear (world space)")]
    [SerializeField] private Vector3 buttonOffset = new Vector3(0, 1.5f, 0);

    [Header("Button Settings")]
    [SerializeField] private float buttonFadeSpeed = 5f;

    [Header("Rune Components")]
    [Tooltip("Animator for the rune sprite (auto-found in first child if not assigned)")]
    [SerializeField] private Animator runeAnimator;
    
    [Tooltip("Rune Effect GameObject (auto-found by name 'Rune Effect' if not assigned)")]
    [SerializeField] private GameObject runeEffectObject;
    
    [Tooltip("Animator for the rune effect (auto-found if not assigned)")]
    [SerializeField] private Animator runeEffectAnimator;

    [Header("Collection Settings")]
    [Tooltip("Duration of fade-out animation after quiz completion (seconds)")]
    [SerializeField] private float fadeOutDuration = 1.5f;
    
    [Tooltip("Delay after quiz completion before starting fade-out (seconds)")]
    [SerializeField] private float postQuizDelay = 0.5f;

    [Header("Game UI Reference")]
    [Tooltip("Main game Canvas (auto-found if not assigned)")]
    [SerializeField] private CanvasGroup gameUICanvasGroup;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // State Management
    private bool isPlayerInRange = false;
    private bool isQuizActive = false;
    private bool hasBeenCollected = false;
    private bool isCollecting = false;
    private Transform playerTransform;
    private MiniQuestUIController miniQuestUI;

    // Button Management
    private GameObject collectionButtonInstance;
    private Button collectionButton;
    private TextMeshProUGUI collectionButtonText;
    private CanvasGroup buttonCanvasGroup;

    // Fade Management
    private SpriteRenderer runeSpriteRenderer;
    private SpriteRenderer[] runeEffectSpriteRenderers;
    private Color originalRuneColor;
    private Color[] originalEffectColors;

    void Awake()
    {
        // Ensure collider is set to trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"RuneCollectionTrigger on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }
        
        // Auto-find rune animator (first child)
        if (runeAnimator == null)
        {
            if (transform.childCount > 0)
            {
                runeAnimator = transform.GetChild(0).GetComponent<Animator>();
            }
            
            if (runeAnimator != null && debugMode)
            {
                Debug.Log($"[Rune {runeName}] Auto-found Rune Animator: {runeAnimator.gameObject.name}");
            }
        }

        // Get rune sprite renderer for fade-out
        if (runeAnimator != null)
        {
            runeSpriteRenderer = runeAnimator.GetComponent<SpriteRenderer>();
            if (runeSpriteRenderer != null)
            {
                originalRuneColor = runeSpriteRenderer.color;
                if (debugMode)
                {
                    Debug.Log($"[Rune {runeName}] Found Rune SpriteRenderer");
                }
            }
        }

        // Auto-find Rune Effect if not assigned
        if (runeEffectObject == null)
        {
            Transform runeEffect = transform.Find("Rune Effect");
            if (runeEffect != null)
            {
                runeEffectObject = runeEffect.gameObject;
                if (debugMode)
                {
                    Debug.Log($"[Rune {runeName}] Auto-found Rune Effect: {runeEffectObject.name}");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning($"[Rune {runeName}] Rune Effect GameObject not found - fade will not affect effects");
            }
        }

        // Auto-find effect animator and sprite renderers
        if (runeEffectObject != null)
        {
            if (runeEffectAnimator == null)
            {
                if (runeEffectObject.transform.childCount > 0)
                {
                    runeEffectAnimator = runeEffectObject.transform.GetChild(0).GetComponent<Animator>();
                    if (debugMode && runeEffectAnimator != null)
                    {
                        Debug.Log($"[Rune {runeName}] Auto-found Effect Animator: {runeEffectAnimator.gameObject.name}");
                    }
                }
            }

            // Get all sprite renderers in effect for fade-out
            runeEffectSpriteRenderers = runeEffectObject.GetComponentsInChildren<SpriteRenderer>(true);
            if (runeEffectSpriteRenderers.Length > 0)
            {
                originalEffectColors = new Color[runeEffectSpriteRenderers.Length];
                for (int i = 0; i < runeEffectSpriteRenderers.Length; i++)
                {
                    originalEffectColors[i] = runeEffectSpriteRenderers[i].color;
                }
                if (debugMode)
                {
                    Debug.Log($"[Rune {runeName}] Found {runeEffectSpriteRenderers.Length} Effect SpriteRenderers");
                }
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
                    Debug.Log($"[Rune {runeName}] Auto-found game UI CanvasGroup");
                }
            }
        }
    }

    void Start()
    {
        CacheMiniQuestUI();
        ValidateMiniQuestData();
        InitializeCollectionButton();
        
        // Check if already collected (from saved game state)
        CheckIfAlreadyCollected();
    }

    void Update()
    {
        UpdateButtonVisibility();
    }

    private void InitializeCollectionButton()
    {
        if (collectionButtonPrefab == null)
        {
            Debug.LogError($"[Rune {runeName}] Collection Button Prefab not assigned!");
            return;
        }

        Vector3 buttonPosition = transform.position + buttonOffset;
        collectionButtonInstance = Instantiate(collectionButtonPrefab, buttonPosition, Quaternion.identity);
        collectionButtonInstance.transform.SetParent(transform, true);

        collectionButton = collectionButtonInstance.GetComponentInChildren<Button>();
        buttonCanvasGroup = collectionButtonInstance.GetComponent<CanvasGroup>();
        collectionButtonText = collectionButtonInstance.GetComponentInChildren<TextMeshProUGUI>();

        if (collectionButton == null)
        {
            Debug.LogError($"[Rune {runeName}] Button component not found in Collection Button Prefab!");
            return;
        }

        if (buttonCanvasGroup == null)
        {
            buttonCanvasGroup = collectionButtonInstance.AddComponent<CanvasGroup>();
        }

        // Set button text
        if (collectionButtonText != null)
        {
            collectionButtonText.text = $"Collect Rune of {runeName}";
        }

        collectionButton.onClick.AddListener(OnCollectionButtonClicked);

        // Initially hide button
        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;
        collectionButtonInstance.SetActive(false);

        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] Collection Button instantiated at position: {buttonPosition}");
        }
    }

    private void CacheMiniQuestUI()
    {
        if (miniQuestUI == null)
        {
            miniQuestUI = Object.FindFirstObjectByType<MiniQuestUIController>();

            if (miniQuestUI == null)
            {
                Debug.LogError($"[Rune {runeName}] MiniQuestUIController not found in scene!");
            }
            else if (debugMode)
            {
                Debug.Log($"[Rune {runeName}] MiniQuestUIController found successfully");
            }
        }
    }

    private void ValidateMiniQuestData()
    {
        if (miniQuestData == null)
        {
            Debug.LogError($"[Rune {runeName}] MiniQuestData not assigned!");
            return;
        }

        if (miniQuestData.questions.Count == 0)
        {
            Debug.LogError($"[Rune {runeName}] MiniQuestData '{miniQuestData.name}' has no questions!");
        }
        else if (miniQuestData.questions.Count > 1)
        {
            Debug.LogWarning($"[Rune {runeName}] MiniQuestData has {miniQuestData.questions.Count} questions. Expected 1 question per rune.");
        }

        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] Mini Quest Data loaded: {miniQuestData.miniQuestTitle}");
            Debug.Log($"[Rune {runeName}] Total questions: {miniQuestData.GetTotalQuestions()}");
        }
    }

    private void CheckIfAlreadyCollected()
    {
        // TODO: Check save data to see if this rune was already collected
        // For now, we'll check if the objective shows this rune as collected
        // This can be expanded with a save system later
        
        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] Checking collection status...");
        }
    }

    private void UpdateButtonVisibility()
    {
        if (buttonCanvasGroup == null || collectionButtonInstance == null) return;

        bool shouldShowButton = isPlayerInRange && !isQuizActive && !hasBeenCollected && !isCollecting && IsObjectiveActive();

        if (shouldShowButton)
        {
            if (!collectionButtonInstance.activeSelf)
            {
                collectionButtonInstance.SetActive(true);
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

            if (buttonCanvasGroup.alpha < 0.01f && collectionButtonInstance.activeSelf)
            {
                collectionButtonInstance.SetActive(false);
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

    private void OnCollectionButtonClicked()
    {
        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] Collection button clicked!");
        }

        if (miniQuestData == null || miniQuestUI == null)
        {
            Debug.LogError($"[Rune {runeName}] Cannot start quiz - missing data or controller!");
            return;
        }

        isQuizActive = true;
        miniQuestUI.StartMiniQuest(miniQuestData, OnQuizCompleted);

        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] Quiz started!");
        }
    }

    private void OnQuizCompleted(bool success)
    {
        isQuizActive = false;

        if (success)
        {
            hasBeenCollected = true;

            if (debugMode)
            {
                Debug.Log($"[Rune {runeName}] Quiz completed successfully!");
                Debug.Log($"[Rune {runeName}] Quiz UI will now close (UI restoration handled by MiniQuestUI is SKIPPED)");
                Debug.Log($"[Rune {runeName}] Starting collection sequence...");
            }

            StartCoroutine(RuneCollectionSequence());
        }
        else if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] Quiz was cancelled or failed");
        }
    }

    // CORRECTED: Collection sequence with UI restoration ONLY after rune is completely disabled
    private IEnumerator RuneCollectionSequence()
    {
        isCollecting = true;

        // ============= PHASE 1: POST-QUIZ DELAY (UI REMAINS HIDDEN) =============
        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] PHASE 1: Quiz UI has closed. Waiting {postQuizDelay}s before fade-out...");
            Debug.Log($"[Rune {runeName}] UI remains HIDDEN during this phase");
        }
        
        yield return new WaitForSeconds(postQuizDelay);

        // ============= PHASE 2: FADE OUT RUNE AND EFFECTS (UI REMAINS HIDDEN) =============
        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] PHASE 2: Fading out rune and effects over {fadeOutDuration}s...");
            Debug.Log($"[Rune {runeName}] UI remains HIDDEN during fade-out");
        }

        yield return StartCoroutine(FadeOutRune());

        // ============= PHASE 3: DISABLE RUNE OBJECTS =============
        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] PHASE 3: Rune faded out! Disabling rune objects...");
        }

        // Disable rune animator
        if (runeAnimator != null)
        {
            runeAnimator.gameObject.SetActive(false);
            if (debugMode)
            {
                Debug.Log($"[Rune {runeName}] ✓ Rune Animator disabled");
            }
        }

        // Disable rune effect
        if (runeEffectObject != null)
        {
            runeEffectObject.SetActive(false);
            if (debugMode)
            {
                Debug.Log($"[Rune {runeName}] ✓ Rune Effect disabled");
            }
        }

        // ============= PHASE 4: RESTORE GAME UI =============
        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] PHASE 4: Rune completely disabled! Now restoring game UI...");
        }

        yield return StartCoroutine(RestoreGameUI());

        // ============= PHASE 5: UPDATE QUEST OBJECTIVE =============
        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] PHASE 5: UI restored! Updating quest objective progress...");
        }

        if (QuestManager.Instance != null)
        {
            // Update objective progress (1 rune collected out of 4)
            QuestManager.Instance.UpdateCurrentQuestObjectiveProgress(requiredObjectiveTitle, 1);
            
            if (debugMode)
            {
                Debug.Log($"[Rune {runeName}] ✓ Quest objective updated! (+1 rune collected)");
            }
        }

        isCollecting = false;

        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] ✓✓✓ Collection sequence completed!");
        }

        // Optionally destroy the entire GameObject after collection
        // Destroy(gameObject);
    }

    // Fade out rune sprite and all effect sprites
    private IEnumerator FadeOutRune()
    {
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);

            // Fade rune sprite
            if (runeSpriteRenderer != null)
            {
                Color color = originalRuneColor;
                color.a = alpha;
                runeSpriteRenderer.color = color;
            }

            // Fade effect sprites
            if (runeEffectSpriteRenderers != null && originalEffectColors != null)
            {
                for (int i = 0; i < runeEffectSpriteRenderers.Length && i < originalEffectColors.Length; i++)
                {
                    if (runeEffectSpriteRenderers[i] != null)
                    {
                        Color color = originalEffectColors[i];
                        color.a = alpha;
                        runeEffectSpriteRenderers[i].color = color;
                    }
                }
            }

            yield return null;
        }

        // Ensure final alpha is 0
        if (runeSpriteRenderer != null)
        {
            Color finalColor = originalRuneColor;
            finalColor.a = 0f;
            runeSpriteRenderer.color = finalColor;
        }

        if (runeEffectSpriteRenderers != null && originalEffectColors != null)
        {
            for (int i = 0; i < runeEffectSpriteRenderers.Length && i < originalEffectColors.Length; i++)
            {
                if (runeEffectSpriteRenderers[i] != null)
                {
                    Color finalColor = originalEffectColors[i];
                    finalColor.a = 0f;
                    runeEffectSpriteRenderers[i].color = finalColor;
                }
            }
        }

        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] ✓ Fade-out completed!");
        }
    }

    // Restore game UI method (copied from HelloWorldAltarTrigger)
    private IEnumerator RestoreGameUI()
    {
        if (gameUICanvasGroup == null)
        {
            Debug.LogWarning($"[Rune {runeName}] No game UI CanvasGroup assigned - skipping UI restoration");
            yield break;
        }

        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] Starting game UI restoration...");
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
                Debug.Log($"[Rune {runeName}] UIController buttons reinitialized");
            }
        }

        if (debugMode)
        {
            Debug.Log($"[Rune {runeName}] ✓ Game UI fully restored!");
            Debug.Log($"[Rune {runeName}] Restored: {allImages.Length} images, {allTexts.Length} texts, {allSprites.Length} sprites, {buttons.Length} buttons, {sliders.Length} sliders");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;

            if (debugMode)
            {
                Debug.Log($"[Rune {runeName}] Player entered detection range");
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
                Debug.Log($"[Rune {runeName}] Player left detection range");
            }
        }
    }

    void OnDestroy()
    {
        if (collectionButtonInstance != null)
        {
            Destroy(collectionButtonInstance);
        }
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = hasBeenCollected ? Color.green : Color.yellow;

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
        string statusText = hasBeenCollected ? "COLLECTED" : (isCollecting ? "COLLECTING..." : "READY");
        string runeAnimatorStatus = runeAnimator != null ? "✓" : "✗ MISSING";
        string effectStatus = runeEffectObject != null ? "✓" : "✗ MISSING";
        string uiStatus = gameUICanvasGroup != null ? "✓" : "✗ MISSING";
        float totalTime = postQuizDelay + fadeOutDuration;
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"Rune of {runeName} [{statusText}]\nID: {runeId}\nAnimator: {runeAnimatorStatus} | Effect: {effectStatus} | UI: {uiStatus}\n\nSEQUENCE:\n1. Player enters → Button appears\n2. Button clicked → Quiz UI (1 question)\n3. Quiz complete → Delay {postQuizDelay}s (UI HIDDEN)\n4. Fade out {fadeOutDuration}s (UI HIDDEN)\n5. Disable rune objects\n6. Restore UI\n7. Update quest (+1/4 runes)\n\nTotal Time: {totalTime}s",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan } });
#endif
    }
}